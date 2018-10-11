ALTER PROCEDURE [dbo].[com_kfs_spSimpleGive]
    @ImportTable NVARCHAR(250),
    @CleanupTable bit = 1
AS

/**************************************************************
- Copyright: Kingdom First Solutions 
- Module: SimpleGive Transaction Import
- Author: David Stevens (10/11/18)
- Contact: support@kingdomfirstsolutions.com

Assumptions:
- Person records must match by FirstName, LastName, Email.  If a 
  single person does not match exactly a new one will be created.
- Fund name must match exactly with FinancialAccount Name, Description,
  or PublicDescription. 
- Transactions will have a unique donation ID.
- Rock block is used to upload transaction file

Installation:
- CREATE PROCEDURE [dbo].[com_kfs_spSimpleGive] AS ;

**************************************************************/

SET NOCOUNT ON
SET XACT_ABORT ON
BEGIN TRANSACTION

DECLARE @cmd NVARCHAR(MAX)
	, @BatchPrefix VARCHAR(100) = 'SimpleGive'
	, @BatchId INT
	, @BatchDate DATE = GETDATE()
    --, @ImportTable NVARCHAR(250)
    --, @CleanupTable bit = 1


/* =================================
Start Logging Operations
==================================== */
DECLARE @message nvarchar(250) = 'Started ' + @BatchPrefix + ' import at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Variables
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
WHERE [TABLE_NAME] = ''' + @ImportTable + '''
   AND COLUMN_NAME = ''TxnID''

IF @Status < 1 
BEGIN 
    RAISERROR(''Imported file does not contain the correct column definitions:'', 0, 10) WITH NOWAIT;
    RAISERROR(''Amount, Type, Txn ID, Fund, Name, Email'', 0, 10) WITH NOWAIT;
    WAITFOR DELAY ''00:00:01'';
END
';

EXEC(@cmd)

/* =================================
Output Matched/Unmatched Transactions
==================================== */
RAISERROR('Checking for existing transactions...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

-- Output existing transaction codes
SELECT @cmd = '
DECLARE @Status NVARCHAR(MAX)
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
'), RockMatch AS (
    SELECT STRING_AGG( CONVERT(VARCHAR(MAX), fd.[TxnID] ), '', '') AS transactionCodes
    FROM financialData fd
    JOIN FinancialTransaction ft
        ON fd.[TxnID] = ft.TransactionCode
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
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
'), RockMatch AS (
    SELECT STRING_AGG(fd.[Name], '', '') AS missingPeople
    FROM financialData fd
    LEFT JOIN Person p
		ON p.Email = fd.Email
		AND p.LastName = RTRIM(LTRIM(SUBSTRING(fd.Name,CHARINDEX('' '',fd.Name), LEN(fd.Name))))
		AND (p.FirstName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name))))
			OR p.NickName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name)))))
    WHERE p.[Id] IS NULL
)
SELECT @Status = ISNULL(''Missing Person records will be created: '' + NULLIF(missingPeople, ''''), ''No unmatched person records...'')
FROM RockMatch;

RAISERROR(@Status, 0, 10) WITH NOWAIT;
';

EXEC(@cmd)


-- Create unmatched people
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
'), NewPeople AS (
    SELECT
		RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name)))) FirstName,
		RTRIM(LTRIM(SUBSTRING(fd.Name,CHARINDEX('' '',fd.Name), LEN(fd.Name)))) LastName,
		fd.Email, 
		fd.[Date] CreatedDateTime,
		ROW_NUMBER() OVER ( PARTITION BY fd.[Name] ORDER BY fd.[Date]) rownum
    FROM financialData fd
    LEFT JOIN Person p
		ON p.Email = fd.Email
		AND p.LastName = RTRIM(LTRIM(SUBSTRING(fd.Name,CHARINDEX('' '',fd.Name), LEN(fd.Name))))
		AND (p.FirstName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name))))
			OR p.NickName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name)))))
    WHERE p.[Id] IS NULL
)
INSERT Person (FirstName, NickName, LastName, Email, CreatedDateTime, ModifiedDateTime, IsSystem, RecordTypeValueId, RecordStatusValueId, ConnectionStatusValueId, IsDeceased, Gender, IsEmailActive, Guid, EmailPreference, CommunicationPreference)
SELECT FirstName, FirstName, LastName, Email, CreatedDateTime, GETDATE(), 0, ' + CONVERT(VARCHAR(20), @PersonRecordTypeId) + ', ' + CONVERT(VARCHAR(20), @PendingRecordStatusId) + ', ' + CONVERT(VARCHAR(20), @NewWebsiteConnectionStatusId) + ', 0, 0, 1, NEWID(), 0, 1
FROM NewPeople
WHERE rownum = 1
';

EXEC(@cmd)

-- Assign PersonAliasId
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
'), PersonRecords AS (
    SELECT DISTINCT
		p.Id PersonId,
		p.Guid PersonGuid
    FROM financialData fd
	JOIN Person p
		ON p.Email = fd.Email
		AND p.LastName = RTRIM(LTRIM(SUBSTRING(fd.Name,CHARINDEX('' '',fd.Name), LEN(fd.Name))))
		AND (p.FirstName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name))))
			OR p.NickName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name)))))
    LEFT JOIN PersonAlias pa
        ON pa.[PersonId] = p.Id
    WHERE pa.[Id] IS NULL
)
INSERT PersonAlias( PersonId, AliasPersonId, AliasPersonGuid, Guid)
SELECT DISTINCT PersonId, PersonId, PersonGuid, PersonGuid
FROM PersonRecords
';

EXEC(@cmd)


/* =================================
Import Transactions
==================================== */
RAISERROR('Importing transactions...', 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

IF (@BatchPrefix IS NOT NULL AND @BatchPrefix <> '')
BEGIN
	DECLARE @BatchName VARCHAR(50) = @BatchPrefix
	SELECT @BatchName = CONVERT(VARCHAR(10), @BatchDate, 10) + ' ' + @BatchPrefix

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
			SELECT * FROM ' + QUOTENAME(@ImportTable) +
		')
		INSERT FinancialBatch (Name, BatchStartDateTime, BatchEndDateTime, Status, ControlAmount, Guid, CreatedDateTime, ForeignKey, IsAutomated)
		SELECT ''' + @BatchName + ''', MIN(CONVERT(DATE, [Date])), ''' + CONVERT(VARCHAR(20), @BatchDate) + ''', 0, SUM(CONVERT(MONEY, Amount)), NEWID(), ''' + CONVERT(VARCHAR(20), @BatchDate) + ''', ''' + @BatchPrefix + ''', 0
		FROM financialData';

		EXEC(@cmd)
	END

	SELECT @BatchId = Id
	FROM FinancialBatch
	WHERE [Status] <> 2
		AND [CreatedDateTime] >= @BatchDate 
		AND [Name] = @BatchName
END

-- Use lookup table to enforce unique ids
CREATE TABLE _com_kfs_transactions (
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

-- Generate lookup values
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
')
INSERT _com_kfs_transactions (AccountId, TransactionDate, CurrencyType, CreditType, PersonAliasId, Amount, TransactionGuid, PaymentDetailGuid, TransactionCode, Summary, ForeignKey)
SELECT 
    fa.Id AccountId,
    CONVERT(DATETIME, fd.[Date]), 
    cdv.[Id] CurrencyType,
    dv.[Id] CreditType,
    pa.[Id] PersonAliasId,
    CONVERT(MONEY, fd.[Amount]) Amount,
    NEWID() TransactionGuid, 
    NEWID() PaymentDetailGuid, 
    ISNULL(fd.[TxnId], '''') TransactionCode,
    fd.[Fund] + '' - '' + fd.[Name] + '' ('' + fd.[TxnId] + '')'' Summary,
    ''' + @BatchPrefix + ''' ForeignKey
FROM financialData fd
JOIN Person p
	ON p.Email = fd.Email
	AND p.LastName = RTRIM(LTRIM(SUBSTRING(fd.Name,CHARINDEX('' '',fd.Name), LEN(fd.Name))))
	AND (p.FirstName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name))))
		OR p.NickName = RTRIM(LTRIM(SUBSTRING(fd.Name,1,CHARINDEX('' '',fd.Name)))))
JOIN PersonAlias pa
    ON pa.[PersonId] = p.Id
JOIN FinancialAccount fa
    ON fd.[Fund] = fa.[Name]
    OR fd.[Fund] = fa.[Description]
	OR fd.[Fund] = fa.[PublicDescription]
LEFT JOIN DefinedValue cdv
    ON cdv.[DefinedTypeId] = ' + CONVERT(VARCHAR(50), @CurrencyTypeId) + '
    AND REPLACE(LEFT(fd.[Type], 3), ''Car'', ''Credit Card'') = cdv.Value
LEFT JOIN DefinedValue dv
    ON dv.[DefinedTypeId] = ' + CONVERT(VARCHAR(50), @CreditCardTypeId) + '
    AND REPLACE(fd.[Type], '' MC'', ''MasterCard'') LIKE ''%'' + dv.Value
LEFT JOIN FinancialTransaction ft
	ON fd.[TxnID] <> ''''
	AND fd.[Date] = ft.TransactionDateTime
	AND fd.[TxnID] = ft.TransactionCode
LEFT JOIN FinancialTransaction ftc
	ON ISNUMERIC(fd.[TxnId]) = 1
	AND fd.[Date] = ftc.TransactionDateTime
	AND pa.Id = ftc.AuthorizedPersonAliasId
	AND fd.[TxnId] = ftc.TransactionCode
WHERE ft.Id IS NULL
	AND ftc.Id IS NULL
';

EXEC(@cmd)


-- Financial Payment Detail
SELECT @cmd = '
INSERT FinancialPaymentDetail (CurrencyTypeValueId, CreditCardTypeValueId, CreatedDateTime, [Guid], ForeignKey)
SELECT CurrencyType, CreditType, TransactionDate, PaymentDetailGuid, ForeignKey
FROM _com_kfs_transactions
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
FROM _com_kfs_transactions tl
JOIN FinancialPaymentDetail fpd
    ON tl.[PaymentDetailGuid] = fpd.[Guid]
';

EXEC(@cmd)


-- Financial Transaction Detail
SELECT @cmd = '
;WITH financialData AS (
    SELECT * FROM ' + QUOTENAME(@ImportTable) +
')
INSERT FinancialTransactionDetail (TransactionId, AccountId, Amount, Guid, CreatedDateTime, ForeignKey)
SELECT ft.Id, ISNULL(tl.AccountId, tl.ParentAccountId), tl.Amount, NEWID(), tl.TransactionDate, tl.ForeignKey
FROM _com_kfs_transactions tl
JOIN FinancialTransaction ft
    ON tl.[TransactionGuid] = ft.[Guid]
';

EXEC(@cmd)

-- Remove lookup table
DROP TABLE [_com_kfs_transactions];

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
SELECT @message = '' + @BatchPrefix + ' import completed at ' + CONVERT(VARCHAR(25), CURRENT_TIMESTAMP) + '.';
RAISERROR(@message, 0, 10) WITH NOWAIT;
WAITFOR DELAY '00:00:01';

COMMIT TRANSACTION