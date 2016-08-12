CREATE PROC dbo.sp_CustKfs_BatchAccountTransactionDate (@entityId INT)
AS
--declare @entityId int = 59;
SELECT a.NAME AS [AccountName]
	,a.Id
	,a.GlCode
	,cast(convert(VARCHAR(10), t.TransactionDateTime, 101) AS DATE) AS [TransactionDate]
	,sum(d.Amount) AS [AccountTransactionDateTotal]
FROM FinancialTransaction t
JOIN FinancialTransactionDetail d ON t.Id = d.TransactionId
JOIN FinancialBatch b ON t.BatchId = b.Id
JOIN FinancialAccount a ON d.AccountId = a.Id
WHERE b.Id IN (
		SELECT EntityId
		FROM EntitySetItem
		WHERE EntitySetId = @entityId
		)
GROUP BY a.Id
	,a.NAME
	,a.GlCode
	,cast(convert(VARCHAR(10), t.TransactionDateTime, 101) AS DATE)
ORDER BY a.NAME
	,a.GlCode
	,cast(convert(VARCHAR(10), t.TransactionDateTime, 101) AS DATE)
