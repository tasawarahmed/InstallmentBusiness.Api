USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- MIGRATION (idempotent): Plan lifecycle (Proposed -> Active) + many-to-many
-- guarantors. Every step checks its own precondition, so this is safe to run
-- whether it's never been applied, partially applied, or already fully applied.
-- ═══════════════════════════════════════════════════════════════════════════

-- ───────────────────────────────────────────────────────────────────────────
-- 1. PlanGuarantors junction table (create only if missing)
-- ───────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PlanGuarantors')
BEGIN
    CREATE TABLE [dbo].[PlanGuarantors](
        [PlanGuarantorId] [int] IDENTITY(1,1) NOT NULL,
        [PlanId] [int] NOT NULL,
        [GuarantorId] [int] NOT NULL,
        [CreatedAt] [datetime] NULL,
    PRIMARY KEY CLUSTERED ([PlanGuarantorId] ASC)
    ) ON [PRIMARY]

    ALTER TABLE [dbo].[PlanGuarantors] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[PlanGuarantors]  WITH CHECK ADD FOREIGN KEY([PlanId]) REFERENCES [dbo].[InstallmentPlans] ([PlanId])
    ALTER TABLE [dbo].[PlanGuarantors]  WITH CHECK ADD FOREIGN KEY([GuarantorId]) REFERENCES [dbo].[Guarantors] ([GuarantorId])
    ALTER TABLE [dbo].[PlanGuarantors] ADD CONSTRAINT [UQ_PlanGuarantors_PlanGuarantor] UNIQUE ([PlanId],[GuarantorId])
    CREATE NONCLUSTERED INDEX [IX_PlanGuarantors_PlanId] ON [dbo].[PlanGuarantors]([PlanId] ASC)
    CREATE NONCLUSTERED INDEX [IX_PlanGuarantors_GuarantorId] ON [dbo].[PlanGuarantors]([GuarantorId] ASC)
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 2. Migrate any existing single-guarantor data -- only if the old column
--    still exists, and skip rows already migrated
-- ───────────────────────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='InstallmentPlans' AND COLUMN_NAME='GuarantorId')
BEGIN
    INSERT INTO [dbo].[PlanGuarantors] (PlanId, GuarantorId)
    SELECT ip.PlanId, ip.GuarantorId
    FROM [dbo].[InstallmentPlans] ip
    WHERE ip.GuarantorId IS NOT NULL
    AND NOT EXISTS (
        SELECT 1 FROM [dbo].[PlanGuarantors] pg
        WHERE pg.PlanId = ip.PlanId AND pg.GuarantorId = ip.GuarantorId
    )
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 3. Rebuild vw_GuarantorPlanCount to use the junction table
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[vw_GuarantorPlanCount]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_GuarantorPlanCount]
GO
CREATE VIEW [dbo].[vw_GuarantorPlanCount] AS
SELECT
    g.GuarantorId,
    g.CustomerId,
    g.FirstName + ' ' + g.LastName AS GuarantorName,
    COUNT(CASE WHEN ip.Status = 'Active' THEN 1 END) AS ActivePlans,
    COUNT(ip.PlanId) AS TotalPlans
FROM [dbo].[Guarantors] g
LEFT JOIN [dbo].[PlanGuarantors] pg ON g.GuarantorId = pg.GuarantorId
LEFT JOIN [dbo].[InstallmentPlans] ip ON pg.PlanId = ip.PlanId
GROUP BY g.GuarantorId, g.CustomerId, g.FirstName, g.LastName;
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 4. Drop the old single-guarantor column, its FK, and its index --
--    only if the column still exists
-- ───────────────────────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='InstallmentPlans' AND COLUMN_NAME='GuarantorId')
BEGIN
    DECLARE @fkName NVARCHAR(200);
    SELECT @fkName = fk.name
    FROM sys.foreign_keys fk
    JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
    JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
    WHERE fk.parent_object_id = OBJECT_ID('dbo.InstallmentPlans') AND c.name = 'GuarantorId';

    IF @fkName IS NOT NULL
        EXEC('ALTER TABLE [dbo].[InstallmentPlans] DROP CONSTRAINT [' + @fkName + ']');

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_InstallmentPlans_GuarantorId' AND object_id=OBJECT_ID('dbo.InstallmentPlans'))
        DROP INDEX [IX_InstallmentPlans_GuarantorId] ON [dbo].[InstallmentPlans];

    ALTER TABLE [dbo].[InstallmentPlans] DROP COLUMN [GuarantorId];
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 5. Add 'Proposed' as a valid Status, and make it the default
-- ───────────────────────────────────────────────────────────────────────────
DECLARE @statusCheckName NVARCHAR(200);
SELECT @statusCheckName = cc.name
FROM sys.check_constraints cc
JOIN sys.columns c ON cc.parent_object_id = c.object_id AND cc.parent_column_id = c.column_id
WHERE cc.parent_object_id = OBJECT_ID('dbo.InstallmentPlans') AND c.name = 'Status';

IF @statusCheckName IS NOT NULL
    EXEC('ALTER TABLE [dbo].[InstallmentPlans] DROP CONSTRAINT [' + @statusCheckName + ']');
GO

ALTER TABLE [dbo].[InstallmentPlans]  WITH CHECK ADD CHECK
	(([Status]='Proposed' OR [Status]='Cancelled' OR [Status]='Defaulted' OR [Status]='Completed' OR [Status]='Active'))
GO

DECLARE @statusDefaultName NVARCHAR(200);
SELECT @statusDefaultName = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON dc.parent_object_id = c.object_id AND dc.parent_column_id = c.column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.InstallmentPlans') AND c.name = 'Status';

IF @statusDefaultName IS NOT NULL
    EXEC('ALTER TABLE [dbo].[InstallmentPlans] DROP CONSTRAINT [' + @statusDefaultName + ']');
GO

ALTER TABLE [dbo].[InstallmentPlans] ADD DEFAULT ('Proposed') FOR [Status]
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 6. Guard triggers: a plan cannot become (or be created as) 'Active'
--    without at least one guarantor
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[trg_InstallmentPlans_RequireGuarantor_Insert]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_InstallmentPlans_RequireGuarantor_Insert]
GO
CREATE TRIGGER [dbo].[trg_InstallmentPlans_RequireGuarantor_Insert] ON [dbo].[InstallmentPlans]
AFTER INSERT
AS
BEGIN
	SET NOCOUNT ON;
	IF EXISTS (
	    SELECT 1 FROM inserted i
	    WHERE i.Status = 'Active'
	    AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanGuarantors] pg WHERE pg.PlanId = i.PlanId)
	)
	BEGIN
	    RAISERROR('Cannot create an installment plan as Active without at least one guarantor.', 16, 1);
	    ROLLBACK TRANSACTION;
	END
END
GO

IF OBJECT_ID('[dbo].[trg_InstallmentPlans_RequireGuarantor_Update]', 'TR') IS NOT NULL
    DROP TRIGGER [dbo].[trg_InstallmentPlans_RequireGuarantor_Update]
GO
CREATE TRIGGER [dbo].[trg_InstallmentPlans_RequireGuarantor_Update] ON [dbo].[InstallmentPlans]
AFTER UPDATE
AS
BEGIN
	SET NOCOUNT ON;
	IF EXISTS (
	    SELECT 1
	    FROM inserted i
	    JOIN deleted d ON i.PlanId = d.PlanId
	    WHERE i.Status = 'Active' AND ISNULL(d.Status,'') <> 'Active'
	    AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanGuarantors] pg WHERE pg.PlanId = i.PlanId)
	)
	BEGIN
	    RAISERROR('Cannot finalize an installment plan without at least one guarantor.', 16, 1);
	    ROLLBACK TRANSACTION;
	END
END
GO

-- ───────────────────────────────────────────────────────────────────────────
-- 7. vw_PlanSummary: account for the down payment (Installment 0)
-- ───────────────────────────────────────────────────────────────────────────
IF OBJECT_ID('[dbo].[vw_PlanSummary]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_PlanSummary]
GO
CREATE VIEW [dbo].[vw_PlanSummary] AS
SELECT
    ip.PlanId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    p.ProductName,
    ip.LoanAmount,
    ip.TenureMonths,
    ip.MonthlyInstallment,
    ip.DownPayment,
    ip.TotalPayable,
    (ip.TotalPayable + ip.DownPayment) AS GrandTotal,
    ISNULL(SUM(CASE WHEN pay.Status = 'Paid' OR pay.Status = 'PartiallyPaid' THEN pay.AmountPaid ELSE 0 END), 0) AS TotalCollected,
    (ip.TotalPayable + ip.DownPayment) - ISNULL(SUM(CASE WHEN pay.Status = 'Paid' OR pay.Status = 'PartiallyPaid' THEN pay.AmountPaid ELSE 0 END), 0) AS BalanceRemaining,
    COUNT(CASE WHEN pay.Status = 'Overdue' THEN 1 END) AS OverdueInstallments,
    ip.Status
FROM [dbo].[InstallmentPlans] ip
JOIN [dbo].[Customers] c ON ip.CustomerId = c.CustomerId
JOIN [dbo].[Products] p ON ip.ProductId = p.ProductId
LEFT JOIN [dbo].[InstallmentPayments] pay ON ip.PlanId = pay.PlanId
GROUP BY ip.PlanId, c.FirstName, c.LastName, p.ProductName, ip.LoanAmount, ip.TenureMonths,
         ip.MonthlyInstallment, ip.DownPayment, ip.TotalPayable, ip.Status;
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- CHECK: does any existing data already violate the new rule?
-- ═══════════════════════════════════════════════════════════════════════════
SELECT PlanId, CustomerId, Status, 'No guarantor on file' AS Issue
FROM [dbo].[InstallmentPlans] ip
WHERE ip.Status = 'Active'
AND NOT EXISTS (SELECT 1 FROM [dbo].[PlanGuarantors] pg WHERE pg.PlanId = ip.PlanId);
GO
