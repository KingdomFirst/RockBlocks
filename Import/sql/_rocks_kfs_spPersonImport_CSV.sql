/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spPersonImport_CSV]    Script Date: 1/4/2019 5:09:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[_rocks_kfs_spPersonImport_CSV]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 1

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
- Required-column check now verifies Email, First Name and Last Name
  all exist (either "FirstName"/"First Name", "LastName"/"Last Name").
  ConnectionStatusId and GroupId are now optional. - GM 7/22/2026 (Assisted by Claude Code)

Reporting:
- RAISERROR ... WITH NOWAIT (severity 0-10) is used throughout so the
  Spreadsheet import block's LogTrigger can stream progress back to the
  client. The one-second WAITFOR DELAYs give the async reader time to
  flush each message. Both are intentional and preserved.

**************************************************************/

    SET NOCOUNT ON
    SET XACT_ABORT ON
    BEGIN TRANSACTION

    DECLARE @cmd NVARCHAR(MAX);
    DECLARE @message NVARCHAR(400);
    DECLARE @qImportTable NVARCHAR(300) = QUOTENAME(@ImportTable);
    DECLARE @now DATETIME = GETDATE();

    -- Rock lookup values (resolved once, then used as static values below)
    DECLARE @PersonRecordTypeId INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Person' AND DefinedTypeId = 1);
    DECLARE @ActiveRecordStatusId INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Active' AND DefinedTypeId = 2);
    DECLARE @PersonEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.Person');
    DECLARE @GroupEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.Group');
    DECLARE @GroupMemberEntityTypeId INT = (SELECT TOP 1 [Id] FROM EntityType WHERE [Name] = 'Rock.Model.GroupMember');

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
        - Gives each uploaded row a stable key so newly created people and
            person aliases can all be joined back to
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
        - Capture the Ids we actually insert
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
        - Capture inserted GroupMember rows
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