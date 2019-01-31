/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spRVC_NPSImport]    Script Date: 1/30/2019 11:39:22 AM ******/
IF (EXISTS (SELECT * FROM sys.objects WHERE type = 'P' AND name = '_rocks_kfs_spRVC_NPSImport'))
BEGIN
	DROP PROCEDURE [dbo].[_rocks_kfs_spRVC_NPSImport]
END

/****** Object:  StoredProcedure [dbo].[_rocks_kfs_spRVC_NPSImport]    Script Date: 1/30/2019 11:39:22 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[_rocks_kfs_spRVC_NPSImport]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 0

AS

/**************************************************************
- Copyright: Kingdom First Solutions 
- Module: RVC NPS Import
- Author: Trey Hendon (1/30/19)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- People are imported on the destination server
- Workflows are created to store NPS survey results
- Rock block is used to upload people file

Installation:
- CREATE PROCEDURE [dbo].[_rocks_kfs_spRVC_NPSImport] AS ;

**************************************************************/

SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRANSACTION

DECLARE @cmd NVARCHAR(MAX)

/* =================================
Start Logging Operations
==================================== */
DECLARE @message nvarchar(250) = 'Started NPS Import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Variables
DECLARE @WorkflowTypeId INT = (SELECT [Id] FROM [WorkflowType] WHERE [Guid] LIKE 'D5AA7E3D-E0F2-4329-932D-2BDAE1A8D880');
DECLARE @ActivityTypeId INT = (SELECT [Id] FROM WorkflowActivityType WHERE [Guid] LIKE '18796706-AEB6-44F6-A416-D3FC99A72628');

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
   AND (
       COLUMN_NAME = ''EmailAddress''
	   OR
	   COLUMN_NAME = ''FirstName''
	   OR
	   COLUMN_NAME = ''LastName''
	   OR
	   COLUMN_NAME = ''CampusCode''
	   OR
	   COLUMN_NAME = ''Recommend''
	   OR
	   COLUMN_NAME = ''EvenHigherRating''
	   OR
	   COLUMN_NAME = ''HigherRating''
	   OR
	   COLUMN_NAME = ''DoesReallyWell''
	   OR
	   COLUMN_NAME = ''InvolvementGrowingUp''
	   OR
	   COLUMN_NAME = ''GrewUpCatholic''
	   OR
	   COLUMN_NAME = ''GrewUpLutheran''
	   OR
	   COLUMN_NAME = ''GrewUpPresbyterian''
	   OR
	   COLUMN_NAME = ''GrewUpBaptist''
	   OR
	   COLUMN_NAME = ''GrewUpAssemblyOfGod''
	   OR
	   COLUMN_NAME = ''GrewUpPentecostal''
	   OR
	   COLUMN_NAME = ''GrewUpNonDenominational''
	   OR
	   COLUMN_NAME = ''GrewUpOther''
	   OR
	   COLUMN_NAME = ''InvolvementLastFiveYears''
	   OR
	   COLUMN_NAME = ''LastFiveYearsCatholic''
	   OR
	   COLUMN_NAME = ''LastFiveYearsLutheran''
	   OR
	   COLUMN_NAME = ''LastFiveYearsPresbyterian''
	   OR
	   COLUMN_NAME = ''LastFiveYearsBaptist''
	   OR
	   COLUMN_NAME = ''LastFiveYearsAssemblyOfGod''
	   OR
	   COLUMN_NAME = ''LastFiveYearsPentecostal''
	   OR
	   COLUMN_NAME = ''LastFiveYearsNonDenominational''
	   OR
	   COLUMN_NAME = ''LastFiveYearsOther''
	   OR
	   COLUMN_NAME = ''Gender''
	   OR
	   COLUMN_NAME = ''Age''
	   OR
	   COLUMN_NAME = ''ChildrenInHousehold''
	   OR
	   COLUMN_NAME = ''Campus''
	   OR
	   COLUMN_NAME = ''CampusOther''
   )

IF @Status < 29 
BEGIN 
    RAISERROR(''Table does not contain the correct column definitions:'', 0, 10) WITH NOWAIT;
    RAISERROR(''RespondentID,EmailAddress,FirstName,LastName,CampusCode,Recommend,EvenHigherRating,HigherRating,DoesReallyWell,InvolvementGrowingUp,GrewUpCatholic,GrewUpLutheran,GrewUpPresbyterian,GrewUpBaptist,GrewUpAsswmblyofGod,GrewUpPentecostal,GrewUpNonDenominational,GrewUpOther,InvolvementLastFiveYears,LastFiveYearsCatholic,LastFiveYearsLutheran,LastFiveYearsPresbyterian,LastFiveYearsPentecostal,LastFiveYearsNonDenominational,LastFiveYearsOther,Gender,Age,ChildrenInHousehold,Campus,CampusOther'', 0, 10) WITH NOWAIT;
    WAITFOR DELAY ''00:00:01'';
END
';

EXEC(@cmd)

RAISERROR('Importing NPS Surveys...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

SELECT @cmd = '
DECLARE @WorkflowTypeId INT=
(
    SELECT [Id]
    FROM [WorkflowType]
    WHERE [Guid] LIKE ''D5AA7E3D-E0F2-4329-932D-2BDAE1A8D880''
);
DECLARE @ActivityTypeId INT=
(
    SELECT [Id]
    FROM [WorkflowActivityType]
    WHERE [Guid] LIKE ''18796706-AEB6-44F6-A416-D3FC99A72628''
);
DECLARE @ActionTypeId INT=
(
    SELECT [Id]
    FROM [WorkflowActionType]
    WHERE [Guid] LIKE ''0E19B55C-4E4C-4EA9-8BD2-38280D605E63''
);
DECLARE @FirstName NVARCHAR(4000);
DECLARE @LastName NVARCHAR(4000);
DECLARE @Email NVARCHAR(4000);
DECLARE @Campus NVARCHAR(4000);
DECLARE @SurveyDate NVARCHAR(4000);
DECLARE @CampusCode NVARCHAR(4000);
DECLARE @Recommend NVARCHAR(4000);
DECLARE @EvenHigherRating NVARCHAR(4000);
DECLARE @HigherRating NVARCHAR(4000);
DECLARE @DoesReallyWell NVARCHAR(4000);
DECLARE @InvolvementGrowingUp NVARCHAR(4000);
DECLARE @GrewUp NVARCHAR(4000);
DECLARE @GrewUpOther NVARCHAR(4000);
DECLARE @InvolvementLastFiveYears NVARCHAR(4000);
DECLARE @LastFiveYears NVARCHAR(4000);
DECLARE @LastFiveYearsOther NVARCHAR(4000);
DECLARE @Gender NVARCHAR(4000);
DECLARE @Age NVARCHAR(4000);
DECLARE @ChildrenInHousehold NVARCHAR(4000);

DECLARE cur CURSOR LOCAL
FOR SELECT [FirstName], 
           [LastName], 
           [EmailAddress], 
           [Campus], 
           [SurveyDate], 
           [CampusCode], 
           [Recommend], 
           [EvenHigherRating], 
           [HigherRating], 
           [DoesReallyWell], 
           [InvolvementGrowingUp],
           CASE
               WHEN [GrewUpOther] IS NOT NULL
               THEN STUFF(COALESCE('',''+[GrewUpCatholic], '''')+COALESCE('',''+[GrewUpLutheran], '''')+COALESCE('',''+[GrewUpPresbyterian], '''')+COALESCE('',''+[GrewUpBaptist], '''')+COALESCE('',''+[GrewUpAssemblyofGod], '''')+COALESCE('',''+[GrewUpPentecostal], '''')+COALESCE('',''+[GrewUpNonDenominational], '''')+'',Other'', 1, 1, '''')
               ELSE STUFF(COALESCE('',''+[GrewUpCatholic], '''')+COALESCE('',''+[GrewUpLutheran], '''')+COALESCE('',''+[GrewUpPresbyterian], '''')+COALESCE('',''+[GrewUpBaptist], '''')+COALESCE('',''+[GrewUpAssemblyofGod], '''')+COALESCE('',''+[GrewUpPentecostal], '''')+COALESCE('',''+[GrewUpNonDenominational], ''''), 1, 1, '''')
           END AS [GrewUp], 
           [GrewUpOther], 
           [InvolvementLastFiveYears],
           CASE
               WHEN [LastFiveYearsOther] IS NOT NULL
               THEN STUFF(COALESCE('',''+[LastFiveYearsCatholic], '''')+COALESCE('',''+[LastFiveYearsLutheran], '''')+COALESCE('',''+[LastFiveYearsPresbyterian], '''')+COALESCE('',''+[LastFiveYearsBapist], '''')+COALESCE('',''+[LastFiveYearAssemblyofGod], '''')+COALESCE('',''+[LastFiveYearsPentecostal], '''')+COALESCE('',''+[LastFiveYearsNonDenominational], '''')+'',Other'', 1, 1, '''')
               ELSE STUFF(COALESCE('',''+[LastFiveYearsCatholic], '''')+COALESCE('',''+[LastFiveYearsLutheran], '''')+COALESCE('',''+[LastFiveYearsPresbyterian], '''')+COALESCE('',''+[LastFiveYearsBapist], '''')+COALESCE('',''+[LastFiveYearAssemblyofGod], '''')+COALESCE('',''+[LastFiveYearsPentecostal], '''')+COALESCE('',''+[LastFiveYearsNonDenominational], ''''), 1, 1, '''')
           END AS [LastFiveYears], 
           [LastFiveYearsOther], 
           [Gender], 
           [Age], 
           [ChildrenInHousehold]
    FROM ' + QUOTENAME(@ImportTable) + ';'
SET @cmd = @cmd + '
OPEN cur;
FETCH NEXT FROM cur INTO @FirstName, @LastName, @Email, @Campus, @SurveyDate, @CampusCode, @Recommend, @EvenHigherRating, @HigherRating, @DoesReallyWell, @InvolvementGrowingUp, @GrewUp, @GrewUpOther, @InvolvementLastFiveYears, @LastFiveYears, @LastFiveYearsOther, @Gender, @Age, @ChildrenInHousehold;
WHILE @@FETCH_STATUS = 0
BEGIN

INSERT INTO [dbo].[Workflow]([WorkflowTypeId], 
[Name], 
[Status], 
[IsProcessing], 
[ActivatedDateTime], 
[LastProcessedDateTime], 
[CompletedDateTime], 
[Guid], 
[CreatedDateTime])
VALUES(@WorkflowTypeId, 
''Started via SQL'', 
''Started via SQL'', 
0, 
GETDATE(), 
NULL, 
NULL, 
NEWID(), 
GETDATE());
DECLARE @WorkflowId INT= SCOPE_IDENTITY();

INSERT INTO [dbo].[WorkflowActivity]([WorkflowId], 
[ActivityTypeId], 
[ActivatedDateTime], 
[LastProcessedDateTime], 
[Guid], 
[CreatedDateTime])
VALUES(@WorkflowId, 
@ActivityTypeId, 
GETDATE(), 
GETDATE(), 
NEWID(), 
GETDATE());
DECLARE @ActivityId INT= SCOPE_IDENTITY();

INSERT INTO [dbo].[WorkflowAction]([ActivityId], 
[ActionTypeId], 
[Guid], 
[CreatedDateTime])
VALUES(@ActivityId, 
@ActionTypeId, 
NEWID(), 
GETDATE());

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''BBF065C2-E402-4B45-9970-502F80812DFD'', 
@WorkflowId, 
@FirstName;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''4EE11F9F-40C9-4EC7-83E2-F12AB0512190'', 
@WorkflowId, 
@LastName;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''04963521-71D8-44A8-B2D9-631FDDA40466'', 
@WorkflowId, 
@Email;

DECLARE @CampusGuid NVARCHAR(100)= ISNULL((SELECT TOP 1 CONVERT(NVARCHAR(100), [Guid])
FROM [Campus]
WHERE [Name] LIKE @Campus), '''');
EXEC _rocks_kfs_spUtility_SetAttributeValue 
''7A879AC4-E754-4300-A4C5-12BEC7E772DB'', 
@WorkflowId, 
@CampusGuid;

DECLARE @AdjustedSurveyDate NVARCHAR(100)= (CONVERT(VARCHAR(4), DATEPART(YEAR, CONVERT(DATE, @SurveyDate)))+''-''+RIGHT(''00''+CONVERT(VARCHAR(2), DATEPART(MONTH, CONVERT(DATE, @SurveyDate))), 2)+''-''+RIGHT(''00''+CONVERT(VARCHAR(2), DATEPART(DAY, CONVERT(DATE, @SurveyDate))), 2)+''T00:00:00.0000000'');
EXEC _rocks_kfs_spUtility_SetAttributeValue 
''325999C2-98D4-4B86-BEB9-61D2273FF061'', 
@WorkflowId, 
@AdjustedSurveyDate;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''875292C1-4C31-4BC8-A58D-EB43DBC14F30'', 
@WorkflowId, 
@CampusCode;
'
SET @cmd = @cmd + '
EXEC _rocks_kfs_spUtility_SetAttributeValue 
''0A4267D8-5C71-4B77-89E1-850CD5EACA73'', 
@WorkflowId, 
@Recommend;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''8C646234-19CA-484B-95E8-2C18EC68B7DB'', 
@WorkflowId, 
@EvenHigherRating;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''7DEBE13B-56B3-4760-BE7D-890B9B0C2D48'', 
@WorkflowId, 
@HigherRating;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''E2674FAA-8947-431D-AA95-B36D08BAFCA2'', 
@WorkflowId, 
@DoesReallyWell;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''A6F882CE-3FEA-4368-ACCA-4073406692EF'', 
@WorkflowId, 
@InvolvementGrowingUp;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''0E86A4C2-D08D-41E6-9E2C-9EF9236456F2'', 
@WorkflowId, 
@GrewUp;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''E195161F-C4D2-4D9C-9856-BFB837A4455F'', 
@WorkflowId, 
@GrewUpOther;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''1DF6E1ED-98F0-4D00-90D3-B9E89D80A72C'', 
@WorkflowId, 
@InvolvementLastFiveYears;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''49D9E86F-9D02-450C-8F46-0F060E7229ED'', 
@WorkflowId, 
@LastFiveYears;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''5590F99F-6AB3-4CB4-9593-C76C93F2F5B7'', 
@WorkflowId, 
@LastFiveYearsOther;

DECLARE @GenderInt INT= (SELECT CASE
WHEN @Gender LIKE ''Male''
THEN 1
WHEN @Gender LIKE ''Female''
THEN 2
ELSE 0
END AS [GenderInt]);
EXEC _rocks_kfs_spUtility_SetAttributeValue 
''3DA2BD30-2296-4A1A-8B96-ED92B8A91F15'', 
@WorkflowId, 
@GenderInt;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''3BF3BC31-FE82-46F2-9602-C5438432B280'', 
@WorkflowId, 
@Age;

EXEC _rocks_kfs_spUtility_SetAttributeValue 
''CC757896-9B44-4E23-86BE-AEF3E756703A'', 
@WorkflowId, 
@ChildrenInHousehold;

FETCH NEXT FROM cur INTO @FirstName, @LastName, @Email, @Campus, @SurveyDate, @CampusCode, @Recommend, @EvenHigherRating, @HigherRating, @DoesReallyWell, @InvolvementGrowingUp, @GrewUp, @GrewUpOther, @InvolvementLastFiveYears, @LastFiveYears, @LastFiveYearsOther, @Gender, @Age, @ChildrenInHousehold;
END;
CLOSE cur;
DEALLOCATE cur;
';

EXEC(@cmd)

/* =================================
Cleanup table post processing
==================================== */

-- Remove temp table
--DROP TABLE [_rocks_kfs_NPSTemp];

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

