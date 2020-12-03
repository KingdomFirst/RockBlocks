/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spPersonImport_CSV]    Script Date: 1/4/2019 5:09:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[_rocks_kfs_spPersonImportWithAttendance_CSV]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 1

AS

/**************************************************************
- Copyright: Kingdom First Solutions 
- Module: Person Import CSV, with Attendance
- Author: Trey Hendon (1/4/19), Nate Hoffman (11/23/2020)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- People are imported on the destination server
- Rock block is used to upload people file

Installation:
- CREATE PROCEDURE [dbo].[[_rocks_kfs_spPersonImportWithAttendance_CSV]] AS ;

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
    RAISERROR(''FirstName,LastName,BirthDate,Email,ConnectionStatusId,GroupID,AttendanceTimestamp,Gender'', 0, 10) WITH NOWAIT;
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
    BirthDate date,
	Email NVARCHAR(75),
	Gender INT,
	ConnectionStatusId INT DEFAULT 66, -- Visitor connection status by default.
	GroupID INT,
    AttendanceTimestamp datetime2,
	ForeignGuid UNIQUEIDENTIFIER,
	PersonId INT
);

DECLARE @StatusGroupID INT
DECLARE @StatusAttendanceTimestamp INT

-- Populate Temp Table
SELECT @cmd = '
SELECT * FROM INFORMATION_SCHEMA.COLUMNS
WHERE [TABLE_NAME] = ''' + @ImportTable + '''
   AND COLUMN_NAME = ''GroupID''
';
EXEC(@cmd)
SET @StatusGroupID = @@ROWCOUNT

SELECT @cmd = '
SELECT * FROM INFORMATION_SCHEMA.COLUMNS
WHERE [TABLE_NAME] = ''' + @ImportTable + '''
   AND COLUMN_NAME = ''AttendanceTimestamp''
';
EXEC(@cmd)
SET @StatusAttendanceTimestamp = @@ROWCOUNT

-- Populate Temp Table
SELECT @cmd = '
;WITH personData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
')
INSERT _rocks_kfs_peopleCsvTemp (FirstName, LastName, BirthDate, Email, Gender, ForeignGuid, GroupID, AttendanceTimestamp)
SELECT 
    CASE WHEN NULLIF(LTRIM([FirstName]), '''') IS NULL THEN RIGHT(Email, LEN(Email) - CHARINDEX(''@'', email)) ELSE [FirstName] END,
	CASE WHEN NULLIF(LTRIM([LastName]), '''') IS NULL THEN LEFT(Email, LEN(Email) - CHARINDEX(''@'', email)) ELSE [LastName] END,
    CONVERT(DATE, BirthDate),
	Email,
    CASE
        WHEN Gender = ''Female'' THEN 2
        WHEN Gender = ''Male'' THEN 1
        ELSE 0
    END AS Gender,
	NEWID()'+
    CASE
        WHEN @StatusGroupID = 1 AND @StatusAttendanceTimestamp = 1 THEN ', CONVERT(INT, GroupID), AttendanceTimestamp'
        WHEN @StatusGroupID = 1 AND @StatusAttendanceTimestamp = 0 THEN ', CONVERT(INT, GroupID), NULL'
        WHEN @StatusGroupID = 0 AND @StatusAttendanceTimestamp = 1 THEN ', NULL, AttendanceTimestamp'
        ELSE ', NULL, NULL'
    END
    +'
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
        ON p.Email = fd.Email OR p.BirthDate = fd.BirthDate
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
        fd.BirthDate,
        DATEPART(day, fd.BirthDate) BirthDay,
        DATEPART(month, fd.BirthDate) BirthMonth,
        DATEPART(year, fd.BirthDate) BirthYear,
		fd.Email,
        fd.Gender,
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
INSERT Person (FirstName, NickName, LastName, BirthDate, BirthDay, BirthMonth, BirthYear, Email, Gender, ForeignGuid, CreatedDateTime, ModifiedDateTime, IsSystem, RecordTypeValueId, RecordStatusValueId, ConnectionStatusValueId, IsDeceased, IsEmailActive, Guid, EmailPreference, CommunicationPreference)
SELECT FirstName, FirstName, LastName, BirthDate, BirthDay, BirthMonth, BirthYear, Email, Gender, ForeignGuid, GETDATE(), GETDATE(), 0, ' + CONVERT(VARCHAR(20), @PersonRecordTypeId) + ', ' + CONVERT(VARCHAR(20), @ActiveRecordStatusId) + ', ConnectionStatusId, 0, 1, NEWID(), 0, 1
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
	OR ((p.Email = t.Email OR p.BirthDate = t.BirthDate)
	AND p.LastName = RTRIM(LTRIM(t.[LastName]))
	AND (p.FirstName = RTRIM(LTRIM(t.[FirstName]))
		OR p.NickName = RTRIM(LTRIM(t.[FirstName])))) 
';

EXEC(@cmd)

RAISERROR('Adding Group Members...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- insert to group
SELECT @cmd = '
;WITH NewGroupMembers AS (
SELECT t.GroupID,
    t.PersonId,
    gt.[DefaultGroupRoleId]
FROM _rocks_kfs_peopleCsvTemp t
LEFT OUTER JOIN [GroupMember] gm ON t.[PersonId] = gm.[PersonId] and gm.[GroupID] = t.GroupID
JOIN [Group] g ON t.[GroupID] = g.[Id]
JOIN [GroupType] gt ON g.[GroupTypeId] = gt.[Id]
WHERE gm.[Id] IS NULL
)
INSERT GroupMember( IsSystem, GroupID, PersonId, GroupRoleId, GroupMemberStatus, [Guid], CreatedDateTime, ModifiedDateTime, DateTimeAdded, IsNotified, IsArchived )
SELECT DISTINCT 0, GroupID, PersonId, DefaultGroupRoleId, 1, NEWID(), GETDATE(), GETDATE(), GETDATE(), 0, 0
FROM NewGroupMembers
';

EXEC(@cmd)

RAISERROR('Adding/Updating attendance...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- insert to attendanceOccurrence
SELECT @cmd = '
;WITH NewAttendanceOccurrences AS (
SELECT t.GroupID,
	CAST(t.AttendanceTimestamp AS DATE) as OccurrenceDate
FROM _rocks_kfs_peopleCsvTemp t
JOIN [Group] g ON t.[GroupID] = g.[Id]
LEFT OUTER JOIN AttendanceOccurrence ao ON ao.GroupId = t.GroupID AND CAST(ao.OccurrenceDate AS DATE) = CAST(t.AttendanceTimestamp AS DATE)
WHERE ao.[Id] IS NULL AND t.AttendanceTimestamp IS NOT NULL
)
INSERT AttendanceOccurrence( GroupID, OccurrenceDate, [Guid], CreatedDateTime, ModifiedDateTime )
SELECT GroupID, OccurrenceDate, NEWID(), GETDATE(), GETDATE()
FROM NewAttendanceOccurrences GROUP BY GroupID, OccurrenceDate
';

EXEC(@cmd)

-- insert to attendance
SELECT @cmd = '
;WITH NewAttendance AS (
SELECT ao.Id as OccurrenceId,
	t.AttendanceTimestamp,
	pa.Id as PersonAliasId
FROM _rocks_kfs_peopleCsvTemp t
JOIN [PersonAlias] pa ON t.PersonId = pa.PersonId
JOIN AttendanceOccurrence ao ON ao.GroupId = t.GroupID AND CAST(ao.OccurrenceDate AS DATE) = CAST(t.AttendanceTimestamp AS DATE)
LEFT OUTER JOIN [Attendance] a ON a.PersonAliasId = pa.Id AND ao.Id = a.OccurrenceId
WHERE a.[Id] IS NULL
)
INSERT Attendance( OccurrenceId, StartDateTime, PersonAliasId, DidAttend, [Guid], CreatedDateTime, ModifiedDateTime )
SELECT OccurrenceId, AttendanceTimestamp, PersonAliasId, 1, NEWID(), GETDATE(), GETDATE()
FROM NewAttendance GROUP BY OccurrenceId, AttendanceTimestamp, PersonAliasId
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
