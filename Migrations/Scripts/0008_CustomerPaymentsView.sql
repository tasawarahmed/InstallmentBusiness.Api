USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 0008: vw_CustomerPayments -- one row per customer payment transaction
-- (down payments and installments alike), with cost-recovery/profit split
-- computed per line, for period-based payment reporting.
--
-- This does NOT add any columns to PaymentTransactions. The split is
-- recomputed here using the same formula ProfitCalculator uses in the API:
--   ProfitRate = (DownPayment + TotalPayable - ProductCostPrice) / (DownPayment + TotalPayable)
--   ProfitAmount(payment)       = ROUND(AmountReceived * ProfitRate, 2)
--   CostRecoveryAmount(payment) = AmountReceived - ProfitAmount(payment)
-- This is safe because DownPayment/TotalPayable/ProductCostPrice are never
-- modified anywhere after a plan is proposed (verified directly against the
-- codebase before writing this) -- the rate used here is guaranteed
-- identical to the rate that was actually in effect when each payment was
-- recorded. T-SQL's ROUND() rounds .5 away from zero, matching C#'s
-- Math.Round(..., MidpointRounding.AwayFromZero) exactly, so this view's
-- numbers reconcile with what's already stored (cumulatively) on
-- InstallmentPayments.CostRecoveryAmount/ProfitAmount.
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('[dbo].[vw_CustomerPayments]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_CustomerPayments]
GO

CREATE VIEW [dbo].[vw_CustomerPayments] AS
SELECT
    pt.TransactionId,
    pt.TransactionDate,
    pt.PlanId,
    ip.CustomerId,
    c.FirstName + ' ' + c.LastName AS CustomerName,
    pay.InstallmentNumber,
    pt.AmountReceived AS TotalPayment,
    ROUND(pt.AmountReceived * ((ip.DownPayment + ip.TotalPayable - ip.ProductCostPrice) / (ip.DownPayment + ip.TotalPayable)), 2) AS ProfitAmount,
    pt.AmountReceived - ROUND(pt.AmountReceived * ((ip.DownPayment + ip.TotalPayable - ip.ProductCostPrice) / (ip.DownPayment + ip.TotalPayable)), 2) AS CostRecoveryAmount
FROM [dbo].[PaymentTransactions] pt
JOIN [dbo].[InstallmentPlans] ip ON pt.PlanId = ip.PlanId
JOIN [dbo].[Customers] c ON ip.CustomerId = c.CustomerId
LEFT JOIN [dbo].[InstallmentPayments] pay ON pt.PaymentId = pay.PaymentId;
GO
