USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- MIGRATION: Expenses (general operating costs -- rent, salaries, utilities,
-- etc. NOT tied to a specific product purchase; inventory/stock-purchase
-- tracking remains a separate, still-unsolved concept).
--
-- Follows the exact pattern already used by Withdrawals and ProfitPayments:
-- a source table + an AFTER INSERT trigger writing to CashLedger, so
-- "CashLedger is populated only by triggers" stays true -- no direct-write
-- path is added for this.
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Expenses')
BEGIN
    CREATE TABLE [dbo].[Expenses](
        [ExpenseId] [int] IDENTITY(1,1) NOT NULL,
        [Category] [varchar](50) NOT NULL,
        [Amount] [decimal](12, 2) NOT NULL,
        [ExpenseDate] [date] NOT NULL,
        [Description] [varchar](max) NULL,
        [PaidTo] [varchar](100) NULL,
        [PaymentMethod] [varchar](50) NULL,
        [ReferenceNo] [varchar](100) NULL,
        [CreatedAt] [datetime] NULL,
    PRIMARY KEY CLUSTERED
    (
        [ExpenseId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]

    ALTER TABLE [dbo].[Expenses] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[Expenses]  WITH CHECK ADD CHECK  (([Amount]>(0)))

    CREATE NONCLUSTERED INDEX [IX_Expenses_ExpenseDate] ON [dbo].[Expenses]([ExpenseDate] ASC)
    CREATE NONCLUSTERED INDEX [IX_Expenses_Category] ON [dbo].[Expenses]([Category] ASC)
END
GO

-- Cash OUT whenever an expense is recorded. Deliberately INSERT-only, unlike
-- Withdrawals/ProfitPayments which also support a Pending-then-Paid path --
-- an expense is recorded after the money has already gone out, so there's
-- no pending/approval state to model here (yet -- easy to add later by
-- mirroring the exact _Insert/_Update pair pattern those two tables use).
IF OBJECT_ID('[dbo].[trg_Expenses_CashOut]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Expenses_CashOut]
GO
CREATE TRIGGER [dbo].[trg_Expenses_CashOut] ON [dbo].[Expenses]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT
	    ExpenseDate,
	    'Expense',
	    'Out',
	    Amount,
	    'Expenses',
	    ExpenseId,
	    Category + CASE WHEN Description IS NOT NULL THEN ': ' + Description ELSE '' END
	FROM inserted;
END
GO
