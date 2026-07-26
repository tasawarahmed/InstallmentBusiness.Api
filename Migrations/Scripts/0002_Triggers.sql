USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- TRIGGERS — restores the automation layer that keeps:
--   1) InstallmentPayments in sync with PaymentTransactions
--   2) CashLedger populated automatically from every cash-affecting table
--
-- Safe to re-run: each trigger is dropped first if it already exists.
-- ═══════════════════════════════════════════════════════════════════════════

-- ───────────────────────────────────────────────────────────────────────────
-- 1. Keep InstallmentPayments.AmountPaid / Status / PaidDate in sync
--    whenever a payment transaction is recorded against an installment
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_PaymentTransactions_SyncInstallment]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_PaymentTransactions_SyncInstallment]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_PaymentTransactions_SyncInstallment] ON [dbo].[PaymentTransactions]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	UPDATE pay
	SET AmountPaid = ISNULL(pay.AmountPaid,0) + t.TotalReceived,
	    PaidDate = t.LastPaymentDate,
	    Status = CASE
	                WHEN ISNULL(pay.AmountPaid,0) + t.TotalReceived >= pay.AmountDue THEN 'Paid'
	                WHEN ISNULL(pay.AmountPaid,0) + t.TotalReceived > 0 THEN 'PartiallyPaid'
	                ELSE pay.Status
	              END
	FROM [dbo].[InstallmentPayments] pay
	JOIN (
	    SELECT PaymentId, SUM(AmountReceived) AS TotalReceived, MAX(TransactionDate) AS LastPaymentDate
	    FROM inserted
	    WHERE PaymentId IS NOT NULL
	    GROUP BY PaymentId
	) t ON pay.PaymentId = t.PaymentId;
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 2. Cash IN — customer payment recorded
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_PaymentTransactions_CashIn]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_PaymentTransactions_CashIn]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_PaymentTransactions_CashIn] ON [dbo].[PaymentTransactions]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT TransactionDate, 'CustomerPayment', 'In', AmountReceived, 'PaymentTransactions', TransactionId, Notes
	FROM inserted;
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 3. Cash IN — new investor capital received
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_Investments_CashIn]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Investments_CashIn]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_Investments_CashIn] ON [dbo].[Investments]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT InvestmentDate, 'Investment', 'In', Amount, 'Investments', InvestmentId, 'Investor capital received'
	FROM inserted;
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 4. Cash OUT — profit payout to investor
--    Covers BOTH: inserted directly as 'Paid', AND inserted as 'Pending'
--    then later updated to 'Paid' (the original design only caught the
--    first case — added the update trigger so a pending-then-paid workflow
--    can't silently skip the ledger).
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_ProfitPayments_CashOut_Insert]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_ProfitPayments_CashOut_Insert]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_ProfitPayments_CashOut_Insert] ON [dbo].[ProfitPayments]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT PaymentDate, 'ProfitPayout', 'Out', ProfitAmount, 'ProfitPayments', ProfitPaymentId, 'Profit distributed to investor'
	FROM inserted
	WHERE Status = 'Paid';
END
GO

IF OBJECT_ID('[dbo].[trg_ProfitPayments_CashOut_Update]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_ProfitPayments_CashOut_Update]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_ProfitPayments_CashOut_Update] ON [dbo].[ProfitPayments]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT i.PaymentDate, 'ProfitPayout', 'Out', i.ProfitAmount, 'ProfitPayments', i.ProfitPaymentId,
	       'Profit distributed to investor (status changed to Paid)'
	FROM inserted i
	JOIN deleted d ON i.ProfitPaymentId = d.ProfitPaymentId
	WHERE i.Status = 'Paid' AND ISNULL(d.Status,'') <> 'Paid';
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 5. Cash OUT — investor withdrawal
--    Same insert + update coverage as profit payouts, for the same reason.
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_Withdrawals_CashOut_Insert]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Withdrawals_CashOut_Insert]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_Withdrawals_CashOut_Insert] ON [dbo].[Withdrawals]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT WithdrawalDate, 'Withdrawal', 'Out', Amount, 'Withdrawals', WithdrawalId, 'Investor withdrawal'
	FROM inserted
	WHERE Status = 'Completed';
END
GO

IF OBJECT_ID('[dbo].[trg_Withdrawals_CashOut_Update]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_Withdrawals_CashOut_Update]
GO
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TRIGGER [dbo].[trg_Withdrawals_CashOut_Update] ON [dbo].[Withdrawals]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	INSERT INTO [dbo].[CashLedger] (TransactionDate, TransactionType, Direction, Amount, ReferenceTable, ReferenceId, Notes)
	SELECT i.WithdrawalDate, 'Withdrawal', 'Out', i.Amount, 'Withdrawals', i.WithdrawalId,
	       'Investor withdrawal (status changed to Completed)'
	FROM inserted i
	JOIN deleted d ON i.WithdrawalId = d.WithdrawalId
	WHERE i.Status = 'Completed' AND ISNULL(d.Status,'') <> 'Completed';
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- VERIFICATION — run this after the script to confirm all 7 triggers exist
-- ═══════════════════════════════════════════════════════════════════════════
SELECT
    t.name AS TriggerName,
    OBJECT_NAME(t.parent_id) AS TableName,
    t.is_disabled AS IsDisabled
FROM sys.triggers t
WHERE OBJECT_NAME(t.parent_id) IN ('PaymentTransactions','Investments','ProfitPayments','Withdrawals')
ORDER BY TableName, TriggerName;
GO
