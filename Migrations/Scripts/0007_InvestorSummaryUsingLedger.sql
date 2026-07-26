USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 0007: Rebuild vw_InvestorSummary to aggregate from vw_InvestorLedger
-- instead of recomputing withdrawal logic independently, so the two views
-- can't drift again (the original vw_InvestorSummary never subtracted
-- withdrawals from ActiveInvestment).
--
-- This exact change was already applied to the original dev database, but
-- only ever as a one-off SQL snippet given directly in chat -- it was never
-- added to the tracked migration chain until now. Capturing it here as its
-- own script (rather than folding it into 0000) so it's explicit that this
-- was a deliberate fix, not part of the original schema.
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('[dbo].[vw_InvestorSummary]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_InvestorSummary]
GO

CREATE VIEW [dbo].[vw_InvestorSummary] AS
SELECT
    vil.InvestorId,
    vil.InvestorName,
    ISNULL(SUM(vil.InvestedAmount), 0) AS TotalInvested,
    ISNULL(SUM(CASE WHEN vil.Status = 'Active' THEN vil.RemainingPrincipal ELSE 0 END), 0) AS ActiveInvestment,
    ISNULL(SUM(vil.TotalWithdrawn), 0) AS TotalWithdrawn,
    ISNULL(SUM(vil.TotalProfitPaid), 0) AS TotalProfitPaid,
    COUNT(CASE WHEN vil.Status = 'Active' THEN 1 END) AS ActiveInvestments
FROM [dbo].[vw_InvestorLedger] vil
GROUP BY vil.InvestorId, vil.InvestorName;
GO
