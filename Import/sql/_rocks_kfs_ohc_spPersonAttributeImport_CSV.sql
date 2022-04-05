/****** Object:  StoredProcedure [dbo].[_rocks_kfs_ohc_spPersonAttributeImport_CSV]    Script Date: 1/4/2019 5:09:20 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[_rocks_kfs_ohc_spPersonAttributeImport_CSV]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 1

AS

/**************************************************************
- Copyright: Kingdom First Solutions 
- Module: Attribute Value Import CSV
- Author: Matt Baylor (4/5/22)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- Only matched email addresses are acted upon
- Rock block is used to upload attribute value file
- fields in CSV are: Email, Value, Action (Add|Remove), AttributeId

Installation:
- CREATE PROCEDURE [dbo].[_rocks_kfs_ohc_spPersonAttributeImport_CSV] AS ;

**************************************************************/

SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRANSACTION

DECLARE @cmd NVARCHAR(MAX)

/* =================================
Start Logging Operations
==================================== */
DECLARE @message nvarchar(250) = 'Started attribute value import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';


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
    RAISERROR(''Email, Value, Action, AttributeId'', 0, 10) WITH NOWAIT;
    WAITFOR DELAY ''00:00:01'';
END
';

EXEC(@cmd)

    -- Update add
SELECT @cmd = '    
    UPDATE AttributeValue
    SET 
        AttributeValue.[Value] = AttributeValue.[Value] + '','' + ud.[Value]
    FROM (
        SELECT 
            p.Id AS EntityId,
            it.AttributeId AS AttributeId,
            it.[Value] AS [Value]
        FROM Person p
            INNER JOIN AttributeValue av ON p.Id = av.EntityId
            INNER JOIN ' + @ImportTable + ' it ON it.Email = p.Email
        WHERE it.[Action] = ''Add''
            AND av.[Value] NOT LIKE ''%'' + it.[Value] + ''%''
            AND av.AttributeId = it.AttributeId
        ) ud
    WHERE AttributeValue.EntityId = ud.EntityId AND AttributeValue.AttributeId = ud.AttributeId;
';
EXEC(@cmd);
--SELECT @cmd;

    --Update Remove
SELECT @cmd = '
    UPDATE AttributeValue
    SET 
        AttributeValue.[Value] = REPLACE(REPLACE(REPLACE(AttributeValue.[Value],'',''+ud.[Value],''''),ud.[Value]+'','',''''),ud.[Value],'''')
    FROM (
        SELECT 
            p.Id AS EntityId,
            it.AttributeId AS AttributeId,
            it.[Value] AS [Value]
        FROM Person p
            INNER JOIN AttributeValue av ON p.Id = av.EntityId
            INNER JOIN ' + @ImportTable + ' it ON it.Email = p.Email
        WHERE it.[Action] = ''Remove''
            AND av.[Value] LIKE ''%'' + it.[Value] + ''%''
            AND av.AttributeId = it.AttributeId
        ) ud
    WHERE AttributeValue.EntityId = ud.EntityId AND AttributeValue.AttributeId = ud.AttributeId
';
EXEC(@cmd);

    --Insert Add
SELECT @cmd = '
    ;WITH toBeAdded (Id,Email) AS (
        SELECT p.Id, p.Email
        FROM Person p
            INNER JOIN ' + @ImportTable + ' it ON it.Email = p.Email
        WHERE p.Id NOT IN (
            SELECT EntityId
            FROM AttributeValue av
            WHERE av.AttributeId = it.AttributeId
        )
    )
    INSERT INTO AttributeValue (IsSystem, AttributeId,EntityId,Value,Guid,CreatedDateTime,ModifiedDateTime)
    SELECT 0 AS IsSystem, 
        it.AttributeId AS AttributeId, 
        tba.Id AS EntityId, 
        it.[Value] AS [Value],
        NEWID() AS [Guid],
        GETDATE() AS CreatedDateTime,
        GETDATE() AS ModifiedDateTime
    FROM ' + @ImportTable + ' it
        INNER JOIN toBeAdded tba ON it.Email = tba.Email
'
EXEC(@cmd);


/* =================================
Cleanup table post processing
==================================== */

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
SELECT @message = 'Attribute Value import completed at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

COMMIT TRANSACTION

