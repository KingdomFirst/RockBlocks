USE [tvc-rock-db]
GO
/****** Object:  StoredProcedure [dbo].[com_kfs_spTransactionImport]    Script Date: 7/10/2018 4:34:15 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[com_kfs_spTransactionImport]
    @TransactionDatabase NVARCHAR(250) = 'dbo',
    @TransactionTable NVARCHAR(250),
    @BatchName NVARCHAR(250) = 'FellowshipOne',
    @CleanupTable bit = 1

AS

/**************************************************************
- Copyright: Kingdom First Solutions 
- Module: FellowshipOne Transaction Import
- Author: David Stevens (5/15/18)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- People are imported on the destination server
- Rock block is used to upload transaction file

Installation:
- CREATE PROCEDURE [dbo].[com_kfs_spTransactionImport] AS ;

**************************************************************/

SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRANSACTION

DECLARE @cmd NVARCHAR(MAX)
    --, @TransactionDatabase NVARCHAR(250) = 'dbo'
    --, @TransactionTable NVARCHAR(250)
    --, @BatchName NVARCHAR(250) = 'FellowshipOne'
    --, @CleanupTable bit = 1


/* =================================
Start Logging Operations
==================================== */
DECLARE @message nvarchar(250) = 'Started transaction import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Variables
DECLARE @GatewayId AS INT = (SELECT [Id] FROM FinancialGateway WHERE [Name] = 'Network Merchants (NMI)');
DECLARE @FrequencyTypeId AS INT = (SELECT [Id] FROM DefinedType WHERE [Name] = 'Recurring Transaction Frequency');
DECLARE @CurrencyTypeId AS INT = (SELECT [Id] FROM DefinedType WHERE [Name] = 'Currency Type');
DECLARE @CreditCardTypeId AS INT = (SELECT [Id] FROM DefinedType WHERE [Name] = 'Credit Card Type');
DECLARE @ContributionValueId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Contribution' AND DefinedTypeId = 25);
DECLARE @WebsiteValueId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Website' AND DefinedTypeId = 12);
DECLARE @PersonRecordTypeId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Person' AND DefinedTypeId = 1);
DECLARE @PendingRecordStatusId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'Pending' AND DefinedTypeId = 2);
DECLARE @NewWebsiteConnectionStatusId AS INT = (SELECT [Id] FROM DefinedValue WHERE [Value] = 'New From Website' AND DefinedTypeId = 4);


/* =================================
Initial cleanup tasks
==================================== */
RAISERROR('Checking table for consistency...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- First verify schema matches
SELECT @cmd = '
DECLARE @Status INT
SELECT @Status = COUNT(1) FROM INFORMATION_SCHEMA.COLUMNS
WHERE [TABLE_NAME] = ''' + @TransactionTable + '''
   AND COLUMN_NAME = ''ContributorId''

IF @Status < 1 
BEGIN 
    RAISERROR(''Table does not contain the correct column definitions:'', 0, 10) WITH NOWAIT;
    RAISERROR(''ContributorId, ContributorName, PreferredEmail, Fund, SubFund, ReceivedDate, ReceivedTime, Type, Amount, Memo'', 0, 10) WITH NOWAIT;
    WAITFOR DELAY ''00:00:01'';
END
';

EXEC(@cmd)

-- Remove empty rows so they don't throw off date calculations
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
')
DELETE FROM financialData
WHERE ContributorId = ''''
';

EXEC(@cmd)


-- Update frequency column
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
')
UPDATE financialData
SET [Frequency] = CASE 
    WHEN [Frequency] = ''One Time'' THEN ''One-Time''
    WHEN [Frequency] = ''Every 2 Weeks'' THEN ''Bi-Weekly''
    ELSE [Frequency]
END 
';

EXEC(@cmd)

RAISERROR('Combining ReceivedDate and ReceivedTime...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Update received date column
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
')
UPDATE fd
SET [ReceivedDate] = CONVERT(DATETIME, CONVERT(DATE, [ReceivedDate])) + CONVERT(DATETIME, CONVERT(TIME, [ReceivedTime]))
FROM financialData fd
WHERE [ReceivedTime] IS NOT NULL;
';

EXEC(@cmd)


-- Update type column
--SELECT @cmd = '
--;WITH financialData AS (
--    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
--')
--UPDATE financialData
--SET [Type] = CASE 
--    WHEN Reference = ''amex'' 
--        THEN ''American Express''
--    WHEN Reference = ''discover''
--        THEN ''Discover''
--    WHEN Reference = ''mc'' 
--        THEN ''MasterCard''
--    WHEN Reference = ''visa'' 
--        THEN ''Visa''
--END
--WHERE [Type] IN (''amex'', ''mc'', ''visa'', ''discover'');
--';

--EXEC(@cmd)


-- Update reference column
--SELECT @cmd = '
--;WITH financialData AS (
--    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
--')
--UPDATE financialData
--SET [Memo] = REPLACE([Memo], ''Reference Number: '', '''')
--';

--EXEC(@cmd)


/* =================================
Output Matched/Unmatched Transactions
==================================== */
RAISERROR('Checking for existing transactions...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Output existing transaction codes
SELECT @cmd = '
DECLARE @Status NVARCHAR(MAX)
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
'), RockMatch AS (
    SELECT STRING_AGG( CONVERT(VARCHAR(MAX), REPLACE([Memo], ''Reference Number: '', '''')), '', '') AS transactionCodes
    FROM financialData fd
    JOIN FinancialTransaction ft
        ON fd.[Memo] = ft.Summary    
		AND fd.[Memo] <> ''''
)
SELECT @Status = ISNULL(''Existing transactions will be skipped: '' + NULLIF(transactionCodes, ''''), ''No transactions matched existing data...'')
FROM RockMatch;

RAISERROR(@Status, 0, 10) WITH NOWAIT;
';

EXEC(@cmd)


-- Output unmatched people
SELECT @cmd = '
DECLARE @Status NVARCHAR(1000)
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
'), RockMatch AS (
    SELECT STRING_AGG([ContributorId], '', '') AS missingPeople
    FROM financialData fd
    LEFT JOIN PersonAlias pa
        ON fd.[ContributorId] = pa.ForeignId
    WHERE pa.[PersonId] IS NULL
)
SELECT @Status = ISNULL(''Missing Person records will be created: '' + NULLIF(missingPeople, ''''), ''No unmatched person records...'')
FROM RockMatch;

RAISERROR(@Status, 0, 10) WITH NOWAIT;
';

EXEC(@cmd)


-- Create unmatched people
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
'), NewPeopleRecords AS (
    SELECT DISTINCT
		ContributorID ForeignId, 
		LTRIM(RTRIM(REPLACE(RIGHT(ContributorName, CHARINDEX('','', REVERSE(ContributorName))), '','', ''''))) FirstName,
		REPLACE(LEFT(ContributorName, ISNULL(NULLIF(CHARINDEX('','', ContributorName), 0), LEN(ContributorName))), '','', '''') LastName,
		PreferredEmail Email, 
		ReceivedDate CreatedDateTime
    FROM financialData fd
    LEFT JOIN PersonAlias pa
        ON fd.[ContributorId] = pa.ForeignId
    WHERE pa.[PersonId] IS NULL
)
INSERT Person (FirstName, LastName, Email, ForeignKey, ForeignId, CreatedDateTime, ModifiedDateTime, IsSystem, RecordTypeValueId, RecordStatusValueId, ConnectionStatusValueId, IsDeceased, Gender, IsEmailActive, Guid, EmailPreference, CommunicationPreference)
SELECT FirstName, LastName, Email, ForeignId, ForeignId, CreatedDateTime, GETDATE(), 0, ' + CONVERT(VARCHAR(20), @PersonRecordTypeId) + ', ' + CONVERT(VARCHAR(20), @PendingRecordStatusId) + ', ' + CONVERT(VARCHAR(20), @NewWebsiteConnectionStatusId) + ', 0, 0, 1, NEWID(), 0, 1
FROM NewPeopleRecords
';

EXEC(@cmd)

-- Assign PersonAliasId
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
'), PersonRecords AS (
    SELECT DISTINCT
		ContributorID ForeignId, 
		p.Id PersonId,
		p.Guid PersonGuid
    FROM financialData fd
	JOIN Person p
		ON p.ForeignId = fd.ContributorId
    LEFT JOIN PersonAlias pa
        ON fd.[ContributorId] = pa.ForeignId
    WHERE pa.[PersonId] IS NULL
)
INSERT PersonAlias( PersonId, AliasPersonId, AliasPersonGuid, Guid, ForeignKey, ForeignId)
SELECT DISTINCT PersonId, PersonId, PersonGuid, PersonGuid, ForeignId, ForeignId
FROM PersonRecords
';

EXEC(@cmd)


/* =================================
Import Transactions
==================================== */
RAISERROR('Importing transactions...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Use lookup table to enforce unique ids
CREATE TABLE _com_kfs_transactionLookup (
	ParentAccountId INT,
    AccountId INT,
	CampusId INT,
	TransactionDate DATETIME,
	CurrencyType INT,
	CreditType INT,
	PersonAliasId INT,
	Amount decimal(18, 2),
	TransactionGuid UNIQUEIDENTIFIER,
	PaymentDetailGuid UNIQUEIDENTIFIER,
    TransactionCode NVARCHAR(50),
	Summary NVARCHAR(1000),
    ForeignKey NVARCHAR(50)
);

DECLARE @BatchId INT, @BatchDate DATE = GETDATE()
IF (@BatchName IS NOT NULL AND @BatchName <> '')
BEGIN
	SELECT @BatchName = CONVERT(VARCHAR(10), @BatchDate, 10) + ' ' + @BatchName


	SELECT @BatchId = Id
	FROM FinancialBatch
	WHERE [Status] <> 2
		AND [BatchEndDateTime] <= @BatchDate
		AND [Name] = @BatchName

	IF @BatchId IS NULL
	BEGIN 
		-- Insert a batch for today
		SELECT @cmd = '
		;WITH financialData AS (
			SELECT * FROM ' + QUOTENAME(@TransactionTable) +
		')
		INSERT FinancialBatch (Name, BatchStartDateTime, BatchEndDateTime, Status, ControlAmount, Guid, CreatedDateTime, ForeignKey, IsAutomated)
		SELECT ''' + @BatchName + ''', MIN(CONVERT(DATE, ReceivedDate)), ''' + CONVERT(VARCHAR(20), @BatchDate) + ''', 0, SUM(CONVERT(MONEY, Amount)), NEWID(), ''' + CONVERT(VARCHAR(20), @BatchDate) + ''', ''FellowshipOne'', 0
		FROM financialData
		';

		EXEC(@cmd)
	END

	SELECT @BatchId = Id
	FROM FinancialBatch
	WHERE [Status] <> 2
		AND [CreatedDateTime] >= @BatchDate 
		AND [Name] = @BatchName
END



-- Generate lookup values
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
')
INSERT _com_kfs_transactionLookup (ParentAccountId, AccountId, CampusId, TransactionDate, CurrencyType, CreditType, PersonAliasId, Amount, TransactionGuid, PaymentDetailGuid, TransactionCode, Summary, ForeignKey)
SELECT 
    pfa.Id ParentAccountId,
    fa.Id AccountId,
    ISNULL(pfa.CampusId, fa.CampusId) CampusId,
    CONVERT(DATETIME, fd.ReceivedDate), 
    cdv.[Id] CurrencyType,
    NULL CreditType,
    pa.[Id] PersonAliasId,
    CONVERT(MONEY, fd.[Amount]) Amount,
    NEWID() TransactionGuid, 
    NEWID() PaymentDetailGuid, 
    ISNULL(NULLIF(REPLACE(fd.[Memo], ''Reference Number: '', ''''), ''''), [Reference]) TransactionCode,
    fd.[Memo] Summary,
    ''FellowshipOne'' ForeignKey
FROM financialData fd
JOIN PersonAlias pa
    ON fd.[ContributorId] = pa.[ForeignId]
JOIN FinancialAccount pfa
    ON fd.[Fund] = pfa.[Name]
    OR fd.[Fund] = pfa.[Description]
LEFT JOIN FinancialAccount fa
    ON fd.[SubFund] <> '''' 
	AND (fa.[Name] = fd.[SubFund] + '' '' + fd.[Fund]
		OR fd.[SubFund] = fa.[Description])
LEFT JOIN DefinedValue cdv
    ON cdv.[DefinedTypeId] = ' + CONVERT(VARCHAR(50), @CurrencyTypeId) + '
    AND fd.[Type] = cdv.[Value]
LEFT JOIN Campus c
    ON LTRIM(RTRIM(REPLACE(fd.Fund, ''Campus'', ''''))) LIKE CONCAT(''%'', c.[Name])
    OR LTRIM(RTRIM(REPLACE(fd.Fund, ''Campus'', ''''))) LIKE CONCAT(c.[ShortCode],''%'')
LEFT JOIN FinancialTransaction ft
	ON fd.Memo <> ''''
	AND ft.TransactionDateTime = fd.ReceivedDate
	AND REPLACE(fd.[Memo], ''Reference Number: '', '''') = ft.TransactionCode
LEFT JOIN FinancialTransaction ftc
	ON ISNUMERIC(fd.Reference) = 1
	AND ftc.TransactionDateTime = fd.ReceivedDate
	AND ftc.AuthorizedPersonAliasId = pa.Id
	AND fd.Reference = ftc.TransactionCode
WHERE ft.Id IS NULL
	AND ftc.Id IS NULL
';

EXEC(@cmd)


-- Financial Payment Detail
SELECT @cmd = '
INSERT FinancialPaymentDetail (CurrencyTypeValueId, CreditCardTypeValueId,	CreatedDateTime, [Guid], ForeignKey)
SELECT CurrencyType, NULL, TransactionDate, PaymentDetailGuid, ForeignKey
FROM _com_kfs_transactionLookup
';

EXEC(@cmd)


-- Financial Transaction
SELECT @cmd = '
INSERT FinancialTransaction (BatchId, TransactionDateTime, TransactionCode, Summary, TransactionTypeValueId, SourceTypeValueId, [Guid], CreatedDateTime, AuthorizedPersonAliasId, FinancialPaymentDetailId, ForeignKey)
SELECT 
    ' + CONVERT(VARCHAR(50), @BatchId) + ' BatchId, 
    tl.TransactionDate, 
    tl.TransactionCode,
    tl.Summary,
    ' + CONVERT(VARCHAR(50), @ContributionValueId) + ' TransactionValueId,
    ' + CONVERT(VARCHAR(50), @WebsiteValueId) + ' SourceValueId,
    tl.TransactionGuid,
    tl.TransactionDate, 
    tl.PersonAliasId, 
    fpd.[Id] DetailId, 
    tl.ForeignKey
FROM _com_kfs_transactionLookup tl
JOIN FinancialPaymentDetail fpd
    ON tl.[PaymentDetailGuid] = fpd.[Guid]
';

EXEC(@cmd)


-- Financial Transaction Detail
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@TransactionTable) +
')
INSERT FinancialTransactionDetail (TransactionId, AccountId, Amount, Guid, CreatedDateTime, ForeignKey)
SELECT ft.Id, ISNULL(tl.AccountId, tl.ParentAccountId), tl.Amount, NEWID(), tl.TransactionDate, tl.ForeignKey
FROM _com_kfs_transactionLookup tl
JOIN FinancialTransaction ft
    ON tl.[TransactionGuid] = ft.[Guid]
';

EXEC(@cmd)

-- Remove lookup table
DROP TABLE [_com_kfs_transactionLookup];

/* =================================
Cleanup table post processing
==================================== */

IF @CleanupTable = 1
BEGIN
    RAISERROR('Removing imported table...', 0, 10) WITH NOWAIT;
    WAITFOR DELAY '00:00:01';

    SELECT @cmd = 'DROP TABLE ' + QUOTENAME(@TransactionTable);
    EXEC(@cmd)
END


/* =================================
Report Operations Complete
==================================== */
SELECT @message = 'Transaction import completed at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

COMMIT TRANSACTION