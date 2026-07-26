USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 0001: Payment Transactions, Investor Funding Linkage, Withdrawals,
--       Unified Cash Ledger -- tables, constraints, indexes, and reporting
--       views only. Triggers live exclusively in 0002_Triggers.sql.
--
-- Note on consolidation: the very first version of this migration (run
-- earlier in this project, by hand) also created 5 triggers directly in
-- this same script. A later, separate script then replaced/renamed some of
-- those triggers, and for a period both the old and new ones existed
-- side by side, double-counting cash movements, until that was caught and
-- fixed. This consolidated version removes that entire intermediate,
-- buggy state from history: this script creates no triggers at all, so a
-- database built from this script set never passes through it.
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
	CREATE TABLE [dbo].[PaymentTransactions](
		[TransactionId] [int] IDENTITY(1,1) NOT NULL,
		[PlanId] [int] NOT NULL,
		[PaymentId] [int] NULL,
		[AmountReceived] [decimal](12, 2) NOT NULL,
		[TransactionDate] [date] NOT NULL,
		[PaymentMethod] [varchar](50) NULL,
		[ReferenceNo] [varchar](100) NULL,
		[ReceivedBy] [varchar](100) NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED
	(
		[TransactionId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[PaymentTransactions] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[PaymentTransactions]  WITH CHECK ADD FOREIGN KEY([PlanId]) REFERENCES [dbo].[InstallmentPlans] ([PlanId])
	ALTER TABLE [dbo].[PaymentTransactions]  WITH CHECK ADD FOREIGN KEY([PaymentId]) REFERENCES [dbo].[InstallmentPayments] ([PaymentId])
	ALTER TABLE [dbo].[PaymentTransactions]  WITH CHECK ADD CHECK  (([AmountReceived]>(0)))

	CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_PlanId] ON [dbo].[PaymentTransactions]([PlanId] ASC)
	CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_PaymentId] ON [dbo].[PaymentTransactions]([PaymentId] ASC)
	CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_TransactionDate] ON [dbo].[PaymentTransactions]([TransactionDate] ASC)

	-- Prevent duplicate installment numbers under the same plan
	ALTER TABLE [dbo].[InstallmentPayments] ADD CONSTRAINT [UQ_InstallmentPayments_PlanInstallment] UNIQUE ([PlanId],[InstallmentNumber])

	-- Safeguard: recorded cost-recovery + profit on an installment can never exceed what was actually paid
	ALTER TABLE [dbo].[InstallmentPayments]  WITH CHECK ADD CHECK
		((ISNULL([CostRecoveryAmount],(0)) + ISNULL([ProfitAmount],(0))) <= ISNULL([AmountPaid],(0)))

	CREATE TABLE [dbo].[PlanFunding](
		[PlanFundingId] [int] IDENTITY(1,1) NOT NULL,
		[PlanId] [int] NOT NULL,
		[InvestmentId] [int] NOT NULL,
		[AmountAllocated] [decimal](12, 2) NOT NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED
	(
		[PlanFundingId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[PlanFunding] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[PlanFunding]  WITH CHECK ADD FOREIGN KEY([PlanId]) REFERENCES [dbo].[InstallmentPlans] ([PlanId])
	ALTER TABLE [dbo].[PlanFunding]  WITH CHECK ADD FOREIGN KEY([InvestmentId]) REFERENCES [dbo].[Investments] ([InvestmentId])
	ALTER TABLE [dbo].[PlanFunding]  WITH CHECK ADD CHECK  (([AmountAllocated]>(0)))
	ALTER TABLE [dbo].[PlanFunding] ADD CONSTRAINT [UQ_PlanFunding_PlanInvestment] UNIQUE ([PlanId],[InvestmentId])

	CREATE NONCLUSTERED INDEX [IX_PlanFunding_PlanId] ON [dbo].[PlanFunding]([PlanId] ASC)
	CREATE NONCLUSTERED INDEX [IX_PlanFunding_InvestmentId] ON [dbo].[PlanFunding]([InvestmentId] ASC)

	CREATE TABLE [dbo].[Withdrawals](
		[WithdrawalId] [int] IDENTITY(1,1) NOT NULL,
		[InvestmentId] [int] NOT NULL,
		[Amount] [decimal](12, 2) NOT NULL,
		[WithdrawalDate] [date] NOT NULL,
		[Status] [varchar](50) NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED
	(
		[WithdrawalId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Withdrawals] ADD DEFAULT ('Completed') FOR [Status]
	ALTER TABLE [dbo].[Withdrawals] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Withdrawals]  WITH CHECK ADD FOREIGN KEY([InvestmentId]) REFERENCES [dbo].[Investments] ([InvestmentId])
	ALTER TABLE [dbo].[Withdrawals]  WITH CHECK ADD CHECK  (([Amount]>(0)))
	ALTER TABLE [dbo].[Withdrawals]  WITH CHECK ADD CHECK  (([Status]='Completed' OR [Status]='Pending' OR [Status]='Cancelled'))

	CREATE NONCLUSTERED INDEX [IX_Withdrawals_InvestmentId] ON [dbo].[Withdrawals]([InvestmentId] ASC)
	CREATE NONCLUSTERED INDEX [IX_Withdrawals_WithdrawalDate] ON [dbo].[Withdrawals]([WithdrawalDate] ASC)

	CREATE TABLE [dbo].[CashLedger](
		[LedgerId] [int] IDENTITY(1,1) NOT NULL,
		[TransactionDate] [date] NOT NULL,
		[TransactionType] [varchar](50) NOT NULL,
		[Direction] [varchar](3) NOT NULL,
		[Amount] [decimal](12, 2) NOT NULL,
		[ReferenceTable] [varchar](50) NULL,
		[ReferenceId] [int] NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED
	(
		[LedgerId] ASC
	)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
	) ON [PRIMARY]

	ALTER TABLE [dbo].[CashLedger] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[CashLedger]  WITH CHECK ADD CHECK  (([Amount]>(0)))
	ALTER TABLE [dbo].[CashLedger]  WITH CHECK ADD CHECK  (([Direction]='In' OR [Direction]='Out'))
	ALTER TABLE [dbo].[CashLedger]  WITH CHECK ADD CHECK
		(([TransactionType]='CustomerPayment' OR [TransactionType]='Investment' OR [TransactionType]='ProfitPayout'
		  OR [TransactionType]='Withdrawal' OR [TransactionType]='ProductPurchase' OR [TransactionType]='Expense'
		  OR [TransactionType]='Other'))

	CREATE NONCLUSTERED INDEX [IX_CashLedger_TransactionDate] ON [dbo].[CashLedger]([TransactionDate] ASC)
	CREATE NONCLUSTERED INDEX [IX_CashLedger_TransactionType] ON [dbo].[CashLedger]([TransactionType] ASC)
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- REPORTING VIEWS (each self-guarded individually -- CREATE VIEW must be
-- alone in its batch, so these can't share the table-creation guard above)
-- ═══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('[dbo].[vw_PendingInstallments]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_PendingInstallments]
GO
CREATE VIEW [dbo].[vw_PendingInstallments] AS
SELECT
    ip.PlanId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    pay.PaymentId,
    pay.InstallmentNumber,
    pay.AmountDue,
    ISNULL(pay.AmountPaid,0) AS AmountPaid,
    pay.AmountDue - ISNULL(pay.AmountPaid,0) AS AmountOutstanding,
    pay.DueDate,
    pay.Status
FROM [dbo].[InstallmentPayments] pay
JOIN [dbo].[InstallmentPlans] ip ON pay.PlanId = ip.PlanId
JOIN [dbo].[Customers] c ON ip.CustomerId = c.CustomerId
WHERE pay.Status IN ('Pending','Overdue','PartiallyPaid');
GO

IF OBJECT_ID('[dbo].[vw_InvestorLedger]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_InvestorLedger]
GO
CREATE VIEW [dbo].[vw_InvestorLedger] AS
SELECT
    inv.InvestorId,
    inv.FirstName + ' ' + inv.LastName AS InvestorName,
    i.InvestmentId,
    i.Amount AS InvestedAmount,
    i.InvestmentDate,
    ISNULL((SELECT SUM(pp.ProfitAmount) FROM [dbo].[ProfitPayments] pp
            WHERE pp.InvestmentId = i.InvestmentId AND pp.Status = 'Paid'),0) AS TotalProfitPaid,
    ISNULL((SELECT SUM(w.Amount) FROM [dbo].[Withdrawals] w
            WHERE w.InvestmentId = i.InvestmentId AND w.Status = 'Completed'),0) AS TotalWithdrawn,
    i.Amount - ISNULL((SELECT SUM(w.Amount) FROM [dbo].[Withdrawals] w
            WHERE w.InvestmentId = i.InvestmentId AND w.Status = 'Completed'),0) AS RemainingPrincipal,
    i.Status
FROM [dbo].[Investments] i
JOIN [dbo].[Investors] inv ON i.InvestorId = inv.InvestorId;
GO

IF OBJECT_ID('[dbo].[vw_PlanFundingSummary]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_PlanFundingSummary]
GO
CREATE VIEW [dbo].[vw_PlanFundingSummary] AS
SELECT
    pf.PlanId,
    ip.LoanAmount,
    invr.InvestorId,
    invr.FirstName + ' ' + invr.LastName AS InvestorName,
    pf.AmountAllocated,
    pf.AmountAllocated / NULLIF(ip.LoanAmount,0) * 100 AS FundingSharePercent
FROM [dbo].[PlanFunding] pf
JOIN [dbo].[InstallmentPlans] ip ON pf.PlanId = ip.PlanId
JOIN [dbo].[Investments] i ON pf.InvestmentId = i.InvestmentId
JOIN [dbo].[Investors] invr ON i.InvestorId = invr.InvestorId;
GO

IF OBJECT_ID('[dbo].[vw_CashInHand]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_CashInHand]
GO
CREATE VIEW [dbo].[vw_CashInHand] AS
SELECT
    ISNULL(SUM(CASE WHEN Direction = 'In' THEN Amount ELSE 0 END),0)
    - ISNULL(SUM(CASE WHEN Direction = 'Out' THEN Amount ELSE 0 END),0) AS CashInHand
FROM [dbo].[CashLedger];
GO

IF OBJECT_ID('[dbo].[vw_CashLedgerByPeriod]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_CashLedgerByPeriod]
GO
CREATE VIEW [dbo].[vw_CashLedgerByPeriod] AS
SELECT
    YEAR(TransactionDate) AS [Year],
    MONTH(TransactionDate) AS [Month],
    DATENAME(MONTH, TransactionDate) + ' ' + CAST(YEAR(TransactionDate) AS VARCHAR) AS PeriodName,
    SUM(CASE WHEN Direction = 'In' THEN Amount ELSE 0 END) AS TotalIn,
    SUM(CASE WHEN Direction = 'Out' THEN Amount ELSE 0 END) AS TotalOut,
    SUM(CASE WHEN Direction = 'In' THEN Amount ELSE -Amount END) AS NetChange
FROM [dbo].[CashLedger]
GROUP BY YEAR(TransactionDate), MONTH(TransactionDate),
         DATENAME(MONTH, TransactionDate) + ' ' + CAST(YEAR(TransactionDate) AS VARCHAR);
GO
