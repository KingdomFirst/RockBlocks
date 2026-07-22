/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spPersonImport_CSV] ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[_rocks_kfs_spPersonImport_CSV]
    @ImportTable NVARCHAR(250),
    @CleanupTable BIT = 1,
    @CreateHistory BIT = 1,
    @CreateDefinedValues BIT = 0
AS
BEGIN

/**************************************************************
- Copyright: Kingdom First Solutions
- Module: Person Import CSV
- Author: Trey Hendon (1/4/19)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- People are imported on the destination server
- Rock block is used to upload people file

Installation:
- CREATE PROCEDURE [dbo].[_rocks_kfs_spPersonImport_CSV] AS ;

Updates:
- Added GroupTypeId to the GroupMember insert process to support
  new Rock model requirement - GM 2/1/2024
- Refactored: only the read from the uploaded table now uses dynamic
  SQL; the rest of the pipeline runs as static SQL against a session
  temp table (#peopleCsvTemp) for readability, better plans and
  concurrency safety. Schema validation now aborts instead of
  continuing. - GM 7/22/2026 (Assisted by Claude Code)
- Added Person AttributeValue import: any column in the uploaded
  table whose name matches a Person Attribute [Key] is imported as
  that person's attribute value (upsert). - GM 7/22/2026 (Assisted by Claude Code)
- Added Rock History generation (@CreateHistory) that mirrors what
  Rock's save hooks write to the History timeline when a Person or a
  GroupMember is created through the UI. - GM 7/22/2026 (Assisted by Claude Code)
- Defined Value attribute handling: when a matched attribute's field
  type is "Defined Value", the raw column text is resolved to the
  DefinedValue.Guid Rock stores. Integers match DefinedValue.Id (no
  match => skip + report); strings match DefinedValue.Value, creating a
  new DefinedValue in the Defined Type when none exists. Multi-value and
  mis-configured Defined Value attributes are skipped + reported.
  @CreateDefinedValues (default 0/false) controls whether missing string
  values are created; when false, non-existent values are reported and
  the attribute value is skipped rather than created.
  Note: DefinedValue has no save hook, so new values create no History
  (consistent with Rock's model layer); a Rock cache clear may be needed
  before new values appear in pick lists. - GM 7/22/2026 (Assisted by Claude Code)
- Required-column check now verifies Email, First Name and Last Name
  all exist (either "FirstName"/"First Name", "LastName"/"Last Name").
  ConnectionStatusId and GroupId are now optional. - GM 7/22/2026 (Assisted by Claude Code)

Reporting:
- RAISERROR ... WITH NOWAIT (severity 0-10) is used throughout so the
  Spreadsheet import block's LogTrigger can stream progress back to the
  client. The one-second WAITFOR DELAYs give the async reader time to
  flush each message. Both are intentional and preserved.

History notes:
- Person (new) -> "Person Demographic Changes" timeline: an ADD/Record
  summary row plus a MODIFY/Property row for each non-blank field, exactly
  as Person.SaveHook builds via History.EvaluateChange (only non-blank new
  values produce a row).
- GroupMember (new) -> two timelines, matching GroupMember.SaveHook:
  "Person Group Membership" (on the Person) and "Group Member Changes"
  (on the GroupMember).
- PersonAlias creates no History. AttributeValue history is intentionally
  NOT written here: Rock stores it in the AttributeValueHistorical SCD
  table (only when Attribute.EnableHistory = 1) with field-type-specific
  value formatting that cannot be faithfully reproduced in T-SQL.
- Enum text matches Rock's ConvertToString() (SplitCase), e.g.
  "Email Allowed", "Recipient Preference", "Push Notification".
**************************************************************/

    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @cmd NVARCHAR(MAX);
    DECLARE @message NVARCHAR(400);
    DECLARE @qImportTable NVARCHAR(300) = QUOTENAME(@ImportTable);
    DECLARE @now DATETIME = GETDATE();
    DECLARE @SourceOfChange NVARCHAR(200) = 'Person Import CSV';

    -- Rock lookup values (resolved once, then used as static values below)
    DECLARE @PersonRecordTypeId INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Person' AND DefinedTypeId = 1);
    DECLARE @ActiveRecordStatusId INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Active' AND DefinedTypeId = 2);
    DECLARE @PersonEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.Person');
    DECLARE @GroupEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.Group');
    DECLARE @GroupMemberEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.GroupMember');

    -- History categories (Rock system GUIDs)
    DECLARE @CatPersonDemographic INT = (SELECT TOP 1 [Id] FROM Category WHERE [Guid] = '51D3EC5A-D079-45ED-909E-B0AB2FD06835');
    DECLARE @CatPersonGroupMembership INT = (SELECT TOP 1 [Id] FROM Category WHERE [Guid] = '325278A4-FACA-4F38-A405-9C090B3BAA34');
    DECLARE @CatGroupChanges INT = (SELECT TOP 1 [Id] FROM Category WHERE [Guid] = '089EB47D-D0EF-493E-B867-DC51BCDEF319');

    /* =================================
    Start Logging Operations
    ==================================== */
    SET @message = 'Started people import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
    RAISERROR(@message, 0, 10) WITH NOWAIT;
    WAITFOR DELAY '00:00:01';

    BEGIN TRY
        BEGIN TRANSACTION;

        /* =================================
        1. Validate schema (aborts on failure)
        - Required: Email, First Name (FirstName | First Name), Last Name
          (LastName | Last Name). Values may be blank per row; the columns
          must exist. ConnectionStatusId and GroupId are optional.
        ==================================== */
        RAISERROR('Checking table for consistency...', 0, 10) WITH NOWAIT;
        WAITFOR DELAY '00:00:01';

        DECLARE @EmailCol     SYSNAME = ( SELECT TOP 1 [COLUMN_NAME] FROM INFORMATION_SCHEMA.COLUMNS
                                          WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] = 'Email' );
        DECLARE @FirstNameCol SYSNAME = ( SELECT TOP 1 [COLUMN_NAME] FROM INFORMATION_SCHEMA.COLUMNS
                                          WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] IN ('FirstName', 'First Name')
                                          ORDER BY CASE [COLUMN_NAME] WHEN 'FirstName' THEN 0 ELSE 1 END );
        DECLARE @LastNameCol  SYSNAME = ( SELECT TOP 1 [COLUMN_NAME] FROM INFORMATION_SCHEMA.COLUMNS
                                          WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] IN ('LastName', 'Last Name')
                                          ORDER BY CASE [COLUMN_NAME] WHEN 'LastName' THEN 0 ELSE 1 END );

        IF @EmailCol IS NULL OR @FirstNameCol IS NULL OR @LastNameCol IS NULL
        BEGIN
            RAISERROR('Import table is missing one or more required columns. Required: Email, FirstName (or "First Name"), LastName (or "Last Name").', 16, 1);
        END

        DECLARE @HasConnectionStatus BIT = CASE WHEN EXISTS ( SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] = 'ConnectionStatusId' ) THEN 1 ELSE 0 END;
        DECLARE @HasGroupId BIT = CASE WHEN EXISTS ( SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] = 'GroupId' ) THEN 1 ELSE 0 END;

        /* =================================
        2. Tag every source row with a ForeignGuid
        - Gives each uploaded row a stable key so newly created people,
          person aliases and attribute values can all be joined back to
          the exact source row (no fragile name/email re-matching).
        ==================================== */
        IF NOT EXISTS (
            SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
            WHERE [TABLE_NAME] = @ImportTable AND [COLUMN_NAME] = 'ForeignGuid'
        )
        BEGIN
            SET @cmd = 'ALTER TABLE ' + @qImportTable + ' ADD ForeignGuid UNIQUEIDENTIFIER;';
            EXEC (@cmd);
        END

        SET @cmd = 'UPDATE ' + @qImportTable + ' SET ForeignGuid = NEWID() WHERE ForeignGuid IS NULL;';
        EXEC (@cmd);

        /* =================================
        3. Load fixed-shape working set
        - Only this step needs dynamic SQL (the source table/column names
          vary). Everything after this runs as plain, tunable static SQL.
        ==================================== */
        CREATE TABLE #peopleCsvTemp (
            FirstName          NVARCHAR(50),
            LastName           NVARCHAR(50),
            Email              NVARCHAR(75),
            ConnectionStatusId INT,
            GroupId            INT,
            ForeignGuid        UNIQUEIDENTIFIER,
            PersonId           INT
        );

        SET @cmd = '
        INSERT #peopleCsvTemp (FirstName, LastName, Email, ConnectionStatusId, GroupId, ForeignGuid)
        SELECT
            CASE WHEN NULLIF(LTRIM(' + QUOTENAME(@FirstNameCol) + '), '''') IS NULL THEN RIGHT(' + QUOTENAME(@EmailCol) + ', LEN(' + QUOTENAME(@EmailCol) + ') - CHARINDEX(''@'', ' + QUOTENAME(@EmailCol) + ')) ELSE ' + QUOTENAME(@FirstNameCol) + ' END,
            CASE WHEN NULLIF(LTRIM(' + QUOTENAME(@LastNameCol) + '), '''')  IS NULL THEN LEFT(' + QUOTENAME(@EmailCol) + ',  LEN(' + QUOTENAME(@EmailCol) + ') - CHARINDEX(''@'', ' + QUOTENAME(@EmailCol) + ')) ELSE ' + QUOTENAME(@LastNameCol) + ' END,
            ' + QUOTENAME(@EmailCol) + ',
            ' + CASE WHEN @HasConnectionStatus = 1 THEN 'CONVERT(INT, ConnectionStatusId)' ELSE 'NULL' END + ',
            ' + CASE WHEN @HasGroupId = 1 THEN 'CONVERT(INT, GroupId)' ELSE 'NULL' END + ',
            ForeignGuid
        FROM ' + @qImportTable + ';';
        EXEC (@cmd);

        /* =================================
        4. Output unmatched people
        ==================================== */
        DECLARE @Status NVARCHAR(1000);

        ;WITH RockMatch AS (
            SELECT missingPeople = STUFF((
                SELECT ',' + fd.[FirstName] + ' ' + fd.[LastName]
                FROM #peopleCsvTemp fd
                LEFT JOIN Person p
                    ON p.Email = fd.Email
                    AND p.LastName = RTRIM(LTRIM(fd.[LastName]))
                    AND (p.FirstName = RTRIM(LTRIM(fd.[FirstName])) OR p.NickName = RTRIM(LTRIM(fd.[FirstName])))
                WHERE p.[Id] IS NULL
                FOR XML PATH('')), 1, 1, '')
        )
        SELECT @Status = ISNULL('Missing Person records will be created: ' + NULLIF(missingPeople, ''), 'No unmatched person records...')
        FROM RockMatch;

        RAISERROR(@Status, 0, 10) WITH NOWAIT;

        /* =================================
        5. Create unmatched people
        - Capture the Ids we actually insert so History (step 9) targets
          only the newly created people.
        ==================================== */
        CREATE TABLE #newPersonIds (PersonId INT);

        ;WITH NewPeople AS (
            SELECT
                LTRIM(RTRIM(fd.[FirstName])) FirstName,
                LTRIM(RTRIM(fd.[LastName]))  LastName,
                fd.Email,
                fd.ConnectionStatusId,
                fd.ForeignGuid
            FROM #peopleCsvTemp fd
            LEFT JOIN Person p
                ON p.Email = fd.Email
                AND p.LastName = RTRIM(LTRIM(fd.[LastName]))
                AND (p.FirstName = RTRIM(LTRIM(fd.[FirstName])) OR p.NickName = RTRIM(LTRIM(fd.[FirstName])))
            WHERE p.[Id] IS NULL
        )
        INSERT Person (FirstName, NickName, LastName, Email, ForeignGuid, CreatedDateTime, ModifiedDateTime, IsSystem, RecordTypeValueId, RecordStatusValueId, ConnectionStatusValueId, IsDeceased, Gender, IsEmailActive, Guid, EmailPreference, CommunicationPreference)
        OUTPUT INSERTED.Id INTO #newPersonIds (PersonId)
        SELECT FirstName, FirstName, LastName, Email, ForeignGuid, @now, @now, 0, @PersonRecordTypeId, @ActiveRecordStatusId, ConnectionStatusId, 0, 0, 1, NEWID(), 0, 1
        FROM NewPeople;

        SELECT @message = CONCAT(@@ROWCOUNT, ' new person record(s) created.');
        RAISERROR(@message, 0, 10) WITH NOWAIT;

        /* =================================
        6. Assign PersonAliasId for the newly created people
        ==================================== */
        ;WITH PersonRecords AS (
            SELECT DISTINCT
                fd.[ForeignGuid],
                p.Id   PersonId,
                p.Guid PersonGuid
            FROM #peopleCsvTemp fd
            JOIN Person p
                ON p.ForeignGuid LIKE fd.[ForeignGuid]
            LEFT JOIN PersonAlias pa
                ON fd.[ForeignGuid] LIKE pa.ForeignGuid
            WHERE pa.[PersonId] IS NULL
        )
        INSERT PersonAlias (PersonId, AliasPersonId, AliasPersonGuid, Guid, ForeignGuid)
        SELECT DISTINCT PersonId, PersonId, PersonGuid, PersonGuid, ForeignGuid
        FROM PersonRecords;

        /* =================================
        7. Resolve PersonId on the working set (existing + new people)
        ==================================== */
        UPDATE t
        SET t.PersonId = p.Id
        FROM #peopleCsvTemp t
        JOIN Person p
            ON p.ForeignGuid = t.ForeignGuid
            OR (p.Email = t.Email
                AND p.LastName = RTRIM(LTRIM(t.[LastName]))
                AND (p.FirstName = RTRIM(LTRIM(t.[FirstName])) OR p.NickName = RTRIM(LTRIM(t.[FirstName]))));

        /* =================================
        8. Insert to group
        - Capture inserted GroupMember rows so History (step 9) targets
          only the memberships we actually created.
        ==================================== */
        CREATE TABLE #newGroupMembers (
            GroupMemberId INT,
            PersonId INT,
            GroupId INT,
            GroupTypeId INT,
            GroupRoleId INT,
            GroupMemberStatus INT,
            CommunicationPreference INT
        );

        ;WITH NewGroupMembers AS (
            SELECT t.GroupId,
                t.PersonId,
                g.[GroupTypeId],
                gt.[DefaultGroupRoleId]
            FROM #peopleCsvTemp t
            LEFT OUTER JOIN [GroupMember] gm ON t.[PersonId] = gm.[PersonId] AND gm.[GroupId] = t.GroupId
            JOIN [Group] g ON t.[GroupId] = g.[Id]
            JOIN [GroupType] gt ON g.[GroupTypeId] = gt.[Id]
            WHERE gm.[Id] IS NULL
        )
        INSERT GroupMember (IsSystem, GroupId, GroupTypeId, PersonId, GroupRoleId, GroupMemberStatus, [Guid], CreatedDateTime, ModifiedDateTime, DateTimeAdded, IsNotified, IsArchived)
        OUTPUT INSERTED.Id, INSERTED.PersonId, INSERTED.GroupId, INSERTED.GroupTypeId, INSERTED.GroupRoleId, INSERTED.GroupMemberStatus, INSERTED.CommunicationPreference
            INTO #newGroupMembers (GroupMemberId, PersonId, GroupId, GroupTypeId, GroupRoleId, GroupMemberStatus, CommunicationPreference)
        SELECT DISTINCT 0, GroupId, GroupTypeId, PersonId, DefaultGroupRoleId, 1, NEWID(), @now, @now, @now, 0, 0
        FROM NewGroupMembers;

        SELECT @message = CONCAT(@@ROWCOUNT, ' group member(s) added.');
        RAISERROR(@message, 0, 10) WITH NOWAIT;

        /* =================================
        9. Rock History (mirrors the UI save hooks)
        ==================================== */
        IF @CreateHistory = 1
        BEGIN
            RAISERROR('Writing history...', 0, 10) WITH NOWAIT;

            /* --- 9a. Person "Demographic Changes" for newly created people --- */
            IF @CatPersonDemographic IS NOT NULL AND @PersonEntityTypeId IS NOT NULL
            BEGIN
                ;WITH np AS (
                    SELECT p.Id,
                        p.RecordTypeValueId, p.RecordStatusValueId, p.ConnectionStatusValueId,
                        p.FirstName, p.NickName, p.LastName, p.Email, p.Gender, p.IsDeceased,
                        p.IsEmailActive, p.EmailPreference, p.CommunicationPreference,
                        LTRIM(RTRIM(ISNULL(p.NickName, ''))) + ' ' + LTRIM(RTRIM(ISNULL(p.LastName, ''))) AS FullName
                    FROM #newPersonIds n
                    JOIN Person p ON p.Id = n.PersonId
                )
                INSERT History (IsSystem, CategoryId, EntityTypeId, EntityId, Verb, Caption, ChangeType, ValueName, NewValue, OldValue, IsSensitive, SourceOfChange, [Guid], CreatedDateTime)
                SELECT 0, @CatPersonDemographic, @PersonEntityTypeId, np.Id,
                    x.Verb, LEFT(np.FullName, 200), x.ChangeType, LEFT(x.ValueName, 250), x.NewValue, NULL, 0, @SourceOfChange, NEWID(), @now
                FROM np
                LEFT JOIN DefinedValue dvRT ON dvRT.Id = np.RecordTypeValueId
                LEFT JOIN DefinedValue dvRS ON dvRS.Id = np.RecordStatusValueId
                LEFT JOIN DefinedValue dvCS ON dvCS.Id = np.ConnectionStatusValueId
                CROSS APPLY ( VALUES
                    ('ADD',    'Record',   'Person',                   np.FullName),
                    ('MODIFY', 'Property', 'Record Type',              dvRT.[Value]),
                    ('MODIFY', 'Property', 'Record Status',            dvRS.[Value]),
                    ('MODIFY', 'Property', 'Connection Status',        dvCS.[Value]),
                    ('MODIFY', 'Property', 'Deceased',                 CASE WHEN np.IsDeceased = 1 THEN 'True' ELSE 'False' END),
                    ('MODIFY', 'Property', 'First Name',               np.FirstName),
                    ('MODIFY', 'Property', 'Nick Name',                np.NickName),
                    ('MODIFY', 'Property', 'Last Name',                np.LastName),
                    ('MODIFY', 'Property', 'Gender',                   CASE np.Gender WHEN 1 THEN 'Male' WHEN 2 THEN 'Female' ELSE 'Unknown' END),
                    ('MODIFY', 'Property', 'Email',                    np.Email),
                    ('MODIFY', 'Property', 'Email Active',             CASE WHEN np.IsEmailActive = 1 THEN 'True' ELSE 'False' END),
                    ('MODIFY', 'Property', 'Email Preference',         CASE np.EmailPreference WHEN 0 THEN 'Email Allowed' WHEN 1 THEN 'No Mass Emails' WHEN 2 THEN 'Do Not Email' END),
                    ('MODIFY', 'Property', 'Communication Preference', CASE np.CommunicationPreference WHEN 0 THEN 'Recipient Preference' WHEN 1 THEN 'Email' WHEN 2 THEN 'SMS' WHEN 3 THEN 'Push Notification' END)
                ) x (Verb, ChangeType, ValueName, NewValue)
                -- Record rows are always written; Property rows only when the new value is non-blank (matches History.EvaluateChange)
                WHERE x.ChangeType = 'Record' OR NULLIF(LTRIM(RTRIM(x.NewValue)), '') IS NOT NULL;
            END

            /* --- 9b. GroupMember: "Person Group Membership" (on the Person) --- */
            IF @CatPersonGroupMembership IS NOT NULL AND @PersonEntityTypeId IS NOT NULL
            BEGIN
                ;WITH gm AS (
                    SELECT n.PersonId, n.GroupId, n.GroupRoleId, n.GroupMemberStatus, n.CommunicationPreference,
                        g.[Name] AS GroupName, gtr.[Name] AS RoleName
                    FROM #newGroupMembers n
                    JOIN [Group] g ON g.Id = n.GroupId
                    LEFT JOIN GroupTypeRole gtr ON gtr.Id = n.GroupRoleId
                )
                INSERT History (IsSystem, CategoryId, EntityTypeId, EntityId, Verb, Caption, ChangeType, ValueName, NewValue, OldValue, IsSensitive, SourceOfChange, RelatedEntityTypeId, RelatedEntityId, [Guid], CreatedDateTime)
                SELECT 0, @CatPersonGroupMembership, @PersonEntityTypeId, gm.PersonId,
                    x.Verb, LEFT(gm.GroupName, 200), x.ChangeType, LEFT(x.ValueName, 250), x.NewValue, NULL, 0, @SourceOfChange,
                    @GroupEntityTypeId, gm.GroupId, NEWID(), @now
                FROM gm
                CROSS APPLY ( VALUES
                    ('ADDEDTOGROUP', 'Record',   '''' + gm.GroupName + ''' Group',              CAST(NULL AS NVARCHAR(MAX))),
                    ('MODIFY',       'Property', gm.GroupName + ' Role',                         gm.RoleName),
                    ('MODIFY',       'Property', gm.GroupName + ' Status',                       CASE gm.GroupMemberStatus WHEN 0 THEN 'Inactive' WHEN 1 THEN 'Active' WHEN 2 THEN 'Pending' END),
                    ('MODIFY',       'Property', gm.GroupName + ' Communication Preference',     CASE gm.CommunicationPreference WHEN 0 THEN 'Recipient Preference' WHEN 1 THEN 'Email' WHEN 2 THEN 'SMS' WHEN 3 THEN 'Push Notification' END)
                ) x (Verb, ChangeType, ValueName, NewValue)
                WHERE x.ChangeType = 'Record' OR NULLIF(LTRIM(RTRIM(x.NewValue)), '') IS NOT NULL;
            END

            /* --- 9c. GroupMember: "Group Member Changes" (on the GroupMember) --- */
            IF @CatGroupChanges IS NOT NULL AND @GroupMemberEntityTypeId IS NOT NULL
            BEGIN
                ;WITH gm AS (
                    SELECT n.GroupMemberId, n.GroupId, n.GroupRoleId, n.GroupMemberStatus, n.CommunicationPreference,
                        g.[Name] AS GroupName, gtr.[Name] AS RoleName,
                        LTRIM(RTRIM(ISNULL(p.NickName, ''))) + ' ' + LTRIM(RTRIM(ISNULL(p.LastName, ''))) AS PersonName
                    FROM #newGroupMembers n
                    JOIN [Group] g ON g.Id = n.GroupId
                    JOIN Person p ON p.Id = n.PersonId
                    LEFT JOIN GroupTypeRole gtr ON gtr.Id = n.GroupRoleId
                )
                INSERT History (IsSystem, CategoryId, EntityTypeId, EntityId, Verb, Caption, ChangeType, ValueName, NewValue, OldValue, IsSensitive, SourceOfChange, RelatedEntityTypeId, RelatedEntityId, [Guid], CreatedDateTime)
                SELECT 0, @CatGroupChanges, @GroupMemberEntityTypeId, gm.GroupMemberId,
                    x.Verb, LEFT(x.Caption, 200), x.ChangeType, LEFT(x.ValueName, 250), x.NewValue, NULL, 0, @SourceOfChange,
                    @GroupEntityTypeId, gm.GroupId, NEWID(), @now
                FROM gm
                CROSS APPLY ( VALUES
                    -- summary row: caption is the person's name (SetCaption in the hook)
                    ('ADDEDTOGROUP', 'Record',   gm.PersonName,                  CAST(NULL AS NVARCHAR(MAX)), gm.PersonName),
                    ('MODIFY',       'Property', 'Role',                         gm.RoleName,                 gm.GroupName),
                    ('MODIFY',       'Property', 'Status',                       CASE gm.GroupMemberStatus WHEN 0 THEN 'Inactive' WHEN 1 THEN 'Active' WHEN 2 THEN 'Pending' END, gm.GroupName),
                    ('MODIFY',       'Property', 'Communication Preference',     CASE gm.CommunicationPreference WHEN 0 THEN 'Recipient Preference' WHEN 1 THEN 'Email' WHEN 2 THEN 'SMS' WHEN 3 THEN 'Push Notification' END, gm.GroupName)
                ) x (Verb, ChangeType, ValueName, NewValue, Caption)
                WHERE x.ChangeType = 'Record' OR NULLIF(LTRIM(RTRIM(x.NewValue)), '') IS NOT NULL;
            END
        END

        /* =================================
        10. Import person attribute values
        - Any uploaded column whose name matches a global Person
          Attribute [Key] is imported as that person's attribute value.
        - Reserved import columns are excluded so a person field cannot be
          mistaken for an attribute.
        - Upsert (update existing / insert new) keyed on AttributeId +
          EntityId (= PersonId). IsPersistedValueDirty = 1 tells Rock to
          recompute the persisted/indexed value columns on next save,
          matching how Rock's own migrations write AttributeValue rows.
        ==================================== */
        RAISERROR('Importing person attribute values...', 0, 10) WITH NOWAIT;
        WAITFOR DELAY '00:00:01';

        DECLARE @attrSelect NVARCHAR(MAX) = '';

        SELECT @attrSelect = @attrSelect
            + CASE WHEN @attrSelect = '' THEN '' ELSE ' UNION ALL ' END
            + 'SELECT t.PersonId, ' + CONVERT(VARCHAR(20), m.AttributeId) + ' AS AttributeId, '
            + 'CONVERT(NVARCHAR(MAX), it.' + QUOTENAME(m.ColumnName) + ') AS [Value] '
            + 'FROM ' + @qImportTable + ' it '
            + 'JOIN #peopleCsvTemp t ON t.ForeignGuid = it.ForeignGuid'
        FROM (
            SELECT c.[COLUMN_NAME] AS ColumnName, MIN(a.[Id]) AS AttributeId
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN Attribute a
                ON a.[Key] = c.[COLUMN_NAME] COLLATE DATABASE_DEFAULT
                AND a.[EntityTypeId] = @PersonEntityTypeId
                AND ISNULL(a.[EntityTypeQualifierColumn], '') = ''
                AND ISNULL(a.[EntityTypeQualifierValue], '') = ''
            WHERE c.[TABLE_NAME] = @ImportTable
              AND c.[COLUMN_NAME] NOT IN ('FirstName', 'First Name', 'LastName', 'Last Name', 'Email', 'ConnectionStatusId', 'GroupId', 'ForeignGuid')
            GROUP BY c.[COLUMN_NAME]
        ) m;

        IF NULLIF(@attrSelect, '') IS NOT NULL
        BEGIN
            CREATE TABLE #attrImport (PersonId INT, AttributeId INT, [Value] NVARCHAR(MAX));

            SET @cmd = 'INSERT #attrImport (PersonId, AttributeId, [Value]) ' + @attrSelect + ';';
            EXEC (@cmd);

            -- Drop unmatched people and blank values
            DELETE FROM #attrImport
            WHERE PersonId IS NULL
               OR NULLIF(LTRIM(RTRIM([Value])), '') IS NULL;

            -- Keep a single value per (PersonId, AttributeId) in case the
            -- upload had duplicate rows resolving to the same person.
            -- After this, (PersonId, AttributeId) is a unique key for #attrImport.
            ;WITH ranked AS (
                SELECT ROW_NUMBER() OVER (PARTITION BY PersonId, AttributeId ORDER BY (SELECT 1)) rn
                FROM #attrImport
            )
            DELETE FROM ranked WHERE rn > 1;

            /* ---------------------------------------------------------------
            Defined Value resolution
            - For attributes whose field type is "Defined Value", the raw
              column text must be converted to the DefinedValue's Guid (that
              is how Rock stores the value).
                * Integer  -> match DefinedValue.Id within the attribute's
                              Defined Type. No match => skip + report.
                * String   -> match DefinedValue.Value within the Defined
                              Type. No match => create a new DefinedValue,
                              then use it.
            - A "pure digits" value is treated as an integer (Id) lookup even
              if it overflows INT (which then simply fails to match => skip).
            - Multi-value ("allowmultiple") and mis-configured (no Defined
              Type) Defined Value attributes are skipped + reported rather
              than risk creating bogus values.
            --------------------------------------------------------------- */
            CREATE TABLE #attrSkipped (AttributeId INT, [Value] NVARCHAR(MAX), Reason NVARCHAR(200));

            -- Defined Value attributes present in this import
            CREATE TABLE #dvAttr (AttributeId INT PRIMARY KEY, DefinedTypeId INT, AllowMultiple BIT);

            INSERT #dvAttr (AttributeId, DefinedTypeId, AllowMultiple)
            SELECT a.[Id],
                   TRY_CONVERT(INT, aqDt.[Value]),
                   CASE WHEN LOWER(ISNULL(aqAm.[Value], '')) IN ('1', 'true', 'yes') THEN 1 ELSE 0 END
            FROM Attribute a
            JOIN FieldType ft ON ft.[Id] = a.[FieldTypeId] AND ft.[Guid] = '59D5A94C-94A0-4630-B80A-BB25697D74C7'
            LEFT JOIN AttributeQualifier aqDt ON aqDt.[AttributeId] = a.[Id] AND aqDt.[Key] = 'definedtype'
            LEFT JOIN AttributeQualifier aqAm ON aqAm.[AttributeId] = a.[Id] AND aqAm.[Key] = 'allowmultiple'
            WHERE a.[Id] IN (SELECT DISTINCT AttributeId FROM #attrImport);

            IF EXISTS (SELECT 1 FROM #dvAttr)
            BEGIN
                -- (a) Skip unsupported Defined Value attributes (multi-value or no Defined Type)
                INSERT #attrSkipped (AttributeId, [Value], Reason)
                SELECT ai.AttributeId, ai.[Value],
                    CASE WHEN d.DefinedTypeId IS NULL
                         THEN 'Defined Value attribute has no Defined Type configured'
                         ELSE 'Multi-value Defined Value attributes are not supported' END
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId
                WHERE d.DefinedTypeId IS NULL OR d.AllowMultiple = 1;

                DELETE ai
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId
                WHERE d.DefinedTypeId IS NULL OR d.AllowMultiple = 1;

                -- (b) Integer values that do not match a DefinedValue.Id in the Defined Type
                INSERT #attrSkipped (AttributeId, [Value], Reason)
                SELECT ai.AttributeId, ai.[Value], 'Integer does not match a DefinedValue.Id in the Defined Type'
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                WHERE LTRIM(RTRIM(ai.[Value])) NOT LIKE '%[^0-9]%'   -- pure digits => integer intent
                  AND NOT EXISTS (
                      SELECT 1 FROM DefinedValue dv
                      WHERE dv.DefinedTypeId = d.DefinedTypeId
                        AND dv.[Id] = TRY_CONVERT(INT, LTRIM(RTRIM(ai.[Value])))
                  );

                DELETE ai
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                WHERE LTRIM(RTRIM(ai.[Value])) NOT LIKE '%[^0-9]%'
                  AND NOT EXISTS (
                      SELECT 1 FROM DefinedValue dv
                      WHERE dv.DefinedTypeId = d.DefinedTypeId
                        AND dv.[Id] = TRY_CONVERT(INT, LTRIM(RTRIM(ai.[Value])))
                  );

                -- (c) String values with no existing DefinedValue.Value in the Defined Type.
                --     When @CreateDefinedValues = 1 these are created; otherwise they are
                --     reported and skipped.
                CREATE TABLE #newDefinedValues (DefinedTypeId INT, ValueText NVARCHAR(MAX));

                INSERT #newDefinedValues (DefinedTypeId, ValueText)
                SELECT DISTINCT d.DefinedTypeId, LTRIM(RTRIM(ai.[Value]))
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                WHERE LTRIM(RTRIM(ai.[Value])) LIKE '%[^0-9]%'       -- contains a non-digit => string intent
                  AND NOT EXISTS (
                      SELECT 1 FROM DefinedValue dv
                      WHERE dv.DefinedTypeId = d.DefinedTypeId
                        AND LTRIM(RTRIM(dv.[Value])) = LTRIM(RTRIM(ai.[Value])) COLLATE DATABASE_DEFAULT
                  );

                IF @CreateDefinedValues = 1
                BEGIN
                    IF EXISTS (SELECT 1 FROM #newDefinedValues)
                    BEGIN
                        ;WITH maxOrder AS (
                            SELECT DefinedTypeId, MAX([Order]) AS MaxOrder
                            FROM DefinedValue
                            GROUP BY DefinedTypeId
                        )
                        INSERT DefinedValue (IsSystem, DefinedTypeId, [Order], [Value], [Description], IsActive, [Guid], CreatedDateTime, ModifiedDateTime)
                        SELECT 0, n.DefinedTypeId,
                            ISNULL(mo.MaxOrder, -1) + ROW_NUMBER() OVER (PARTITION BY n.DefinedTypeId ORDER BY n.ValueText),
                            n.ValueText, '', 1, NEWID(), @now, @now
                        FROM #newDefinedValues n
                        LEFT JOIN maxOrder mo ON mo.DefinedTypeId = n.DefinedTypeId;

                        SELECT @message = CONCAT(@@ROWCOUNT, ' new Defined Value(s) created.');
                        RAISERROR(@message, 0, 10) WITH NOWAIT;
                    END
                END
                ELSE
                BEGIN
                    -- Creation disabled: report + skip string values that do not already exist
                    INSERT #attrSkipped (AttributeId, [Value], Reason)
                    SELECT ai.AttributeId, ai.[Value], 'DefinedValue does not exist and creation is disabled'
                    FROM #attrImport ai
                    JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                    WHERE LTRIM(RTRIM(ai.[Value])) LIKE '%[^0-9]%'
                      AND NOT EXISTS (
                          SELECT 1 FROM DefinedValue dv
                          WHERE dv.DefinedTypeId = d.DefinedTypeId
                            AND LTRIM(RTRIM(dv.[Value])) = LTRIM(RTRIM(ai.[Value])) COLLATE DATABASE_DEFAULT
                      );

                    DELETE ai
                    FROM #attrImport ai
                    JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                    WHERE LTRIM(RTRIM(ai.[Value])) LIKE '%[^0-9]%'
                      AND NOT EXISTS (
                          SELECT 1 FROM DefinedValue dv
                          WHERE dv.DefinedTypeId = d.DefinedTypeId
                            AND LTRIM(RTRIM(dv.[Value])) = LTRIM(RTRIM(ai.[Value])) COLLATE DATABASE_DEFAULT
                      );
                END

                -- (d) Resolve every remaining Defined Value row to a DefinedValue.Guid
                --     using the ORIGINAL value (keyed by PersonId+AttributeId, now unique),
                --     so the update below can't re-interpret a resolved Guid.
                CREATE TABLE #dvResolved (PersonId INT, AttributeId INT, ResolvedGuid NVARCHAR(36));

                -- integer intent
                INSERT #dvResolved (PersonId, AttributeId, ResolvedGuid)
                SELECT ai.PersonId, ai.AttributeId, LOWER(CONVERT(NVARCHAR(36), dv.[Guid]))
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                JOIN DefinedValue dv ON dv.DefinedTypeId = d.DefinedTypeId AND dv.[Id] = TRY_CONVERT(INT, LTRIM(RTRIM(ai.[Value])))
                WHERE LTRIM(RTRIM(ai.[Value])) NOT LIKE '%[^0-9]%';

                -- string intent (existing or just-created; lowest Id wins on duplicate Values)
                INSERT #dvResolved (PersonId, AttributeId, ResolvedGuid)
                SELECT ai.PersonId, ai.AttributeId, LOWER(CONVERT(NVARCHAR(36), dv.[Guid]))
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId AND d.DefinedTypeId IS NOT NULL AND d.AllowMultiple = 0
                CROSS APPLY (
                    SELECT TOP 1 dv0.[Guid]
                    FROM DefinedValue dv0
                    WHERE dv0.DefinedTypeId = d.DefinedTypeId
                      AND LTRIM(RTRIM(dv0.[Value])) = LTRIM(RTRIM(ai.[Value])) COLLATE DATABASE_DEFAULT
                    ORDER BY dv0.[Id]
                ) dv
                WHERE LTRIM(RTRIM(ai.[Value])) LIKE '%[^0-9]%';

                UPDATE ai
                SET ai.[Value] = r.ResolvedGuid
                FROM #attrImport ai
                JOIN #dvResolved r ON r.PersonId = ai.PersonId AND r.AttributeId = ai.AttributeId;

                -- Safety net: any Defined Value row still unresolved is skipped + reported
                INSERT #attrSkipped (AttributeId, [Value], Reason)
                SELECT ai.AttributeId, ai.[Value], 'Could not resolve Defined Value'
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId
                LEFT JOIN #dvResolved r ON r.PersonId = ai.PersonId AND r.AttributeId = ai.AttributeId
                WHERE r.PersonId IS NULL;

                DELETE ai
                FROM #attrImport ai
                JOIN #dvAttr d ON d.AttributeId = ai.AttributeId
                LEFT JOIN #dvResolved r ON r.PersonId = ai.PersonId AND r.AttributeId = ai.AttributeId
                WHERE r.PersonId IS NULL;
            END

            -- Report any skipped Defined Value entries
            IF EXISTS (SELECT 1 FROM #attrSkipped)
            BEGIN
                DECLARE @skipCount INT = (SELECT COUNT(*) FROM #attrSkipped);
                DECLARE @skipMsg NVARCHAR(MAX);
                SELECT @skipMsg = STRING_AGG(CONVERT(NVARCHAR(MAX), CONCAT(a.[Key], '=', LEFT(s.[Value], 50), ' (', s.Reason, ')')), '; ')
                FROM #attrSkipped s
                JOIN Attribute a ON a.[Id] = s.AttributeId;

                SET @message = LEFT(CONCAT(@skipCount, ' attribute value(s) skipped: ', @skipMsg), 400);
                RAISERROR(@message, 0, 10) WITH NOWAIT;
                WAITFOR DELAY '00:00:01';
            END

            -- Update existing attribute values
            UPDATE av
            SET av.[Value] = ai.[Value],
                av.[ModifiedDateTime] = @now,
                av.[IsPersistedValueDirty] = 1
            FROM AttributeValue av
            JOIN #attrImport ai
                ON ai.AttributeId = av.AttributeId
                AND ai.PersonId = av.EntityId;

            DECLARE @attrUpdated INT = @@ROWCOUNT;

            -- Insert new attribute values
            INSERT AttributeValue (IsSystem, AttributeId, EntityId, [Value], [Guid], CreatedDateTime, ModifiedDateTime, IsPersistedValueDirty)
            SELECT 0, ai.AttributeId, ai.PersonId, ai.[Value], NEWID(), @now, @now, 1
            FROM #attrImport ai
            WHERE NOT EXISTS (
                SELECT 1 FROM AttributeValue av
                WHERE av.AttributeId = ai.AttributeId
                  AND av.EntityId = ai.PersonId
            );

            SELECT @message = CONCAT(@@ROWCOUNT + @attrUpdated, ' person attribute value(s) imported.');
            RAISERROR(@message, 0, 10) WITH NOWAIT;
            WAITFOR DELAY '00:00:01';
        END
        ELSE
        BEGIN
            RAISERROR('No uploaded columns matched a Person attribute key.', 0, 10) WITH NOWAIT;
        END

        /* =================================
        Cleanup table post processing
        ==================================== */
        IF @CleanupTable = 1
        BEGIN
            RAISERROR('Removing imported table...', 0, 10) WITH NOWAIT;
            WAITFOR DELAY '00:00:01';

            SET @cmd = 'DROP TABLE ' + @qImportTable + ';';
            EXEC (@cmd);
        END

        COMMIT TRANSACTION;

        /* =================================
        Report Operations Complete
        ==================================== */
        SELECT @message = 'People import completed at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
        RAISERROR(@message, 0, 10) WITH NOWAIT;
        WAITFOR DELAY '00:00:01';
    END TRY
    BEGIN CATCH
        IF XACT_STATE() <> 0
            ROLLBACK TRANSACTION;

        SELECT @message = 'People import failed: ' + ERROR_MESSAGE();
        RAISERROR(@message, 16, 1) WITH NOWAIT;
    END CATCH
END
GO