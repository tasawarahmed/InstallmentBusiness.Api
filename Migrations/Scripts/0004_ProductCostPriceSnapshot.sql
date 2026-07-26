USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 0004: Freeze the product's cost price on the plan at proposal time.
-- Mirrors the existing ProductSalePrice snapshot -- both cost and sale price
-- for a plan must stay fixed even if Products.CostPrice/SalePrice change later.
-- This is the basis for the per-payment profit split:
--   ProfitRate  = ((DownPayment + TotalPayable) - ProductCostPrice) / (DownPayment + TotalPayable)
--   ProfitAmount(payment) = AmountDue(payment) * ProfitRate
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='InstallmentPlans' AND COLUMN_NAME='ProductCostPrice')
BEGIN
	ALTER TABLE [dbo].[InstallmentPlans] ADD [ProductCostPrice] [decimal](10,2) NULL
END
GO

-- Best-effort backfill for any existing plans: uses the product's current
-- cost price since historical cost isn't otherwise recorded anywhere.
-- Safe to re-run: only touches rows that are still NULL, so it's a no-op
-- once every plan has a value.
UPDATE ip
SET ip.ProductCostPrice = p.CostPrice
FROM [dbo].[InstallmentPlans] ip
JOIN [dbo].[Products] p ON ip.ProductId = p.ProductId
WHERE ip.ProductCostPrice IS NULL
GO

-- Tighten to NOT NULL only once every row has a value -- safe to re-run,
-- and a no-op once already NOT NULL.
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='InstallmentPlans' AND COLUMN_NAME='ProductCostPrice' AND IS_NULLABLE='YES'
)
BEGIN
	ALTER TABLE [dbo].[InstallmentPlans] ALTER COLUMN [ProductCostPrice] [decimal](10,2) NOT NULL
END
GO
