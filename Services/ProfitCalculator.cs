using InstallmentBusiness.Api.Models.Entities;

namespace InstallmentBusiness.Api.Services;

// The single place the profit-split formula lives. Used identically for
// the down payment (Installment 0) and every subsequent installment, so
// every payment on a plan is split at the same rate.
//
//   TotalCustomerPays = DownPayment + TotalPayable
//   TotalProfit       = TotalCustomerPays - ProductCostPrice   (frozen at proposal time)
//   ProfitRate        = TotalProfit / TotalCustomerPays
//
// For any single payment amount:
//   ProfitAmount        = Round(Amount * ProfitRate, 2)
//   CostRecoveryAmount  = Amount - ProfitAmount   (never computed independently,
//                                                   so the two always sum exactly
//                                                   to the amount received)
public static class ProfitCalculator
{
    public static decimal CalculateProfitRate(InstallmentPlan plan)
    {
        var totalCustomerPays = plan.DownPayment + plan.TotalPayable;
        if (totalCustomerPays <= 0)
            throw new InvalidOperationException("Plan's total payable amount must be greater than zero to compute a profit rate.");

        var totalProfit = totalCustomerPays - plan.ProductCostPrice;
        return totalProfit / totalCustomerPays;
    }

    public static (decimal ProfitPortion, decimal CostRecoveryPortion) Split(decimal amount, decimal profitRate)
    {
        var profitPortion = Math.Round(amount * profitRate, 2, MidpointRounding.AwayFromZero);
        var costRecoveryPortion = amount - profitPortion;
        return (profitPortion, costRecoveryPortion);
    }
}
