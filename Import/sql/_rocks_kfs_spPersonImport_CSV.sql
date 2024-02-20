/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spPersonImport_CSV]    Script Date: 1/4/2019 5:09:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[_rocks_kfs_spPersonImport_CSV]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 1

AS

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

**************************************************************/

SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRANSACTION

DECLARE @cmd NVARCHAR(MAX)

/* =================================
Start Logging Operations
==================================== */
DECLARE @message nvarchar(250) = 'Started people import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Variables
DECLARE @PersonRecordTypeId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Person' AND DefinedTypeId = 1);
DECLARE @ActiveRecordStatusId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Active' AND DefinedTypeId = 2);

/* =================================
Initial cleanup tasks
==================================== */
RAISERROR('Checking table for consistency...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- First verify schema matches
SELECT @cmd = '
DECLARE @Status INT
SELECT @Status = COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS
WHERE [TABLE_NAME] = ''' + @ImportTable + '''
   AND COLUMN_NAME = ''Email''

IF @Status < 1 
BEGIN 
    RAISERROR(''Table does not contain the correct column definitions:'', 0, 10) WITH NOWAIT;
    RAISERROR(''FirstName,LastName,Email,ConnectionStatusId,GroupId'', 0, 10) WITH NOWAIT;
    WAITFOR DELAY ''00:00:01'';
END
';

EXEC(@cmd)


-- Import to processing table so foreign guid can be created
IF (EXISTS (SELECT * 
                 FROM INFORMATION_SCHEMA.TABLES 
                 WHERE TABLE_SCHEMA = 'dbo' 
                 AND  TABLE_NAME = '_rocks_kfs_peopleCsvTemp'))
BEGIN
    DROP TABLE [_rocks_kfs_peopleCsvTemp];
END

CREATE TABLE _rocks_kfs_peopleCsvTemp (
	FirstName NVARCHAR(50),
	LastName NVARCHAR(50),
	Email NVARCHAR(75),
	ConnectionStatusId INT,
	GroupId INT,
	ForeignGuid UNIQUEIDENTIFIER,
	PersonId INT
);


-- Populate Temp Table
SELECT @cmd = '
;WITH personData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
')
INSERT _rocks_kfs_peopleCsvTemp (FirstName, LastName, Email, ConnectionStatusId, GroupId, ForeignGuid)
SELECT 
    CASE WHEN NULLIF(LTRIM([FirstName]), '''') IS NULL THEN RIGHT(Email, LEN(Email) - CHARINDEX(''@'', email)) ELSE [FirstName] END,
	CASE WHEN NULLIF(LTRIM([LastName]), '''') IS NULL THEN LEFT(Email, LEN(Email) - CHARINDEX(''@'', email)) ELSE [LastName] END,
	Email,
	CONVERT(INT, ConnectionStatusId),
	CONVERT(INT, GroupId),
	NEWID()
FROM personData fd
';

EXEC(@cmd)


-- Output unmatched people
SELECT @cmd = '
DECLARE @Status NVARCHAR(1000)
;WITH personData AS (
    SELECT * FROM _rocks_kfs_peopleCsvTemp
), RockMatch AS (
    SELECT missingPeople = STUFF( (SELECT '','' + fd.[FirstName] + '' '' + fd.[LastName] FROM personData fd
    LEFT JOIN Person p
        ON p.Email = fd.Email
        AND p.LastName = RTRIM(LTRIM(fd.[LastName]))
        AND (p.FirstName = RTRIM(LTRIM(fd.[FirstName]))
            OR p.NickName = RTRIM(LTRIM(fd.[FirstName])))
            WHERE p.[Id] IS NULL FOR XML PATH ('''')), 1, 1, '''' )
)
SELECT @Status = ISNULL(''Missing Person records will be created: '' + NULLIF(missingPeople, ''''), ''No unmatched person records...'')
FROM RockMatch;

RAISERROR(@Status, 0, 10) WITH NOWAIT;
';

EXEC(@cmd)


-- Create unmatched people
SELECT @cmd = '
;WITH personData AS (
    SELECT * FROM _rocks_kfs_peopleCsvTemp
), NewPeople AS (
    SELECT
		LTRIM(RTRIM(fd.[FirstName])) FirstName,
		LTRIM(RTRIM(fd.[LastName])) LastName,
		fd.Email, 
		fd.ConnectionStatusId,
		fd.ForeignGuid
    FROM personData fd
    LEFT JOIN Person p
		ON p.Email = fd.Email
		AND p.LastName = RTRIM(LTRIM(fd.[LastName]))
		AND (p.FirstName = RTRIM(LTRIM(fd.[FirstName]))
			OR p.NickName = RTRIM(LTRIM(fd.[FirstName])))
    WHERE p.[Id] IS NULL
)
INSERT Person (FirstName, NickName, LastName, Email, ForeignGuid, CreatedDateTime, ModifiedDateTime, IsSystem, RecordTypeValueId, RecordStatusValueId, ConnectionStatusValueId, IsDeceased, Gender, IsEmailActive, Guid, EmailPreference, CommunicationPreference)
SELECT FirstName, FirstName, LastName, Email, ForeignGuid, GETDATE(), GETDATE(), 0, ' + CONVERT(VARCHAR(20), @PersonRecordTypeId) + ', ' + CONVERT(VARCHAR(20), @ActiveRecordStatusId) + ', ConnectionStatusId, 0, 0, 1, NEWID(), 0, 1
FROM NewPeople
';

EXEC(@cmd)


-- Assign PersonAliasId
SELECT @cmd = '
;WITH personData AS (
    SELECT * FROM _rocks_kfs_peopleCsvTemp
), PersonRecords AS (
    SELECT DISTINCT
		fd.[ForeignGuid], 
		p.Id PersonId,
		p.Guid PersonGuid
    FROM personData fd
	JOIN Person p
		ON p.ForeignGuid LIKE fd.[ForeignGuid]
    LEFT JOIN PersonAlias pa
        ON fd.[ForeignGuid] LIKE pa.ForeignGuid
    WHERE pa.[PersonId] IS NULL
)
INSERT PersonAlias( PersonId, AliasPersonId, AliasPersonGuid, Guid, ForeignGuid)
SELECT DISTINCT PersonId, PersonId, PersonGuid, PersonGuid, ForeignGuid
FROM PersonRecords
';

EXEC(@cmd)


-- set person id on temp table
SELECT @cmd = '
UPDATE t
SET t.PersonId = p.Id
FROM _rocks_kfs_peopleCsvTemp t
JOIN Person p
	ON p.ForeignGuid = t.ForeignGuid
	OR (p.Email = t.Email
	AND p.LastName = RTRIM(LTRIM(t.[LastName]))
	AND (p.FirstName = RTRIM(LTRIM(t.[FirstName]))
		OR p.NickName = RTRIM(LTRIM(t.[FirstName])))) 
';

EXEC(@cmd)


-- insert to group
SELECT @cmd = '
;WITH NewGroupMembers AS (
SELECT t.GroupId,
    t.PersonId,
    g.[GroupTypeId],
    gt.[DefaultGroupRoleId]
FROM _rocks_kfs_peopleCsvTemp t
LEFT OUTER JOIN [GroupMember] gm ON t.[PersonId] = gm.[PersonId] and gm.[GroupId] = t.GroupId
JOIN [Group] g ON t.[GroupId] = g.[Id]
JOIN [GroupType] gt ON g.[GroupTypeId] = gt.[Id]
WHERE gm.[Id] IS NULL
)
INSERT GroupMember( IsSystem, GroupId, GroupTypeId, PersonId, GroupRoleId, GroupMemberStatus, [Guid], CreatedDateTime, ModifiedDateTime, DateTimeAdded, IsNotified, IsArchived )
SELECT DISTINCT 0, GroupId, GroupTypeId, PersonId, DefaultGroupRoleId, 1, NEWID(), GETDATE(), GETDATE(), GETDATE(), 0, 0
FROM NewGroupMembers
';

EXEC(@cmd)


/* =================================
Cleanup table post processing
==================================== */

-- Remove temp table
DROP TABLE [_rocks_kfs_peopleCsvTemp];

IF @CleanupTable = 1
BEGIN
    RAISERROR('Removing imported table...', 0, 10) WITH NOWAIT;
    WAITFOR DELAY '00:00:01';

    SELECT @cmd = 'DROP TABLE ' + QUOTENAME(@ImportTable);
    EXEC(@cmd)
END


/* =================================
Report Operations Complete
==================================== */
SELECT @message = 'People import completed at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

COMMIT TRANSACTION

