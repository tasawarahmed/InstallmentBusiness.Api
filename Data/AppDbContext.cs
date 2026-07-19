using InstallmentBusiness.Api.Models.Entities;
using InstallmentBusiness.Api.Models.Views;
using Microsoft.EntityFrameworkCore;

namespace InstallmentBusiness.Api.Data;

// Database-first: this context maps to a schema that already exists and is
// managed by hand-written SQL migration scripts (see the schema/migration
// .sql files). EF Migrations are NOT used here -- do not run
// `dotnet ef migrations add`/`database update` against this context, as
// there is no migration history table and none should be created.
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Guarantor> Guarantors => Set<Guarantor>();
    public DbSet<PlanGuarantor> PlanGuarantors => Set<PlanGuarantor>();
    public DbSet<InstallmentPlan> InstallmentPlans => Set<InstallmentPlan>();
    public DbSet<InstallmentPayment> InstallmentPayments => Set<InstallmentPayment>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();
    public DbSet<Investor> Investors => Set<Investor>();
    public DbSet<Investment> Investments => Set<Investment>();
    public DbSet<PlanFunding> PlanFundings => Set<PlanFunding>();
    public DbSet<ProfitPayment> ProfitPayments => Set<ProfitPayment>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<CashLedger> CashLedgerEntries => Set<CashLedger>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // Views -- keyless, read-only
    public DbSet<PlanSummary> PlanSummaries => Set<PlanSummary>();
    public DbSet<InvestorSummary> InvestorSummaries => Set<InvestorSummary>();
    public DbSet<InvestorLedgerEntry> InvestorLedger => Set<InvestorLedgerEntry>();
    public DbSet<PlanFundingSummary> PlanFundingSummaries => Set<PlanFundingSummary>();
    public DbSet<ProfitByPeriod> ProfitByPeriod => Set<ProfitByPeriod>();
    public DbSet<CashLedgerByPeriod> CashLedgerByPeriod => Set<CashLedgerByPeriod>();
    public DbSet<PendingInstallment> PendingInstallments => Set<PendingInstallment>();
    public DbSet<GuarantorPlanCount> GuarantorPlanCounts => Set<GuarantorPlanCount>();
    public DbSet<CashInHand> CashInHand => Set<CashInHand>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // ─── ProductCategory ────────────────────────────────────────────
        b.Entity<ProductCategory>(e =>
        {
            e.ToTable("ProductCategories");
            e.HasKey(x => x.CategoryId);
            e.Property(x => x.CategoryName).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.CategoryName).IsUnique();
        });

        // ─── Product ────────────────────────────────────────────────────
        b.Entity<Product>(e =>
        {
            e.ToTable("Products");
            e.HasKey(x => x.ProductId);
            e.Property(x => x.ProductName).HasMaxLength(150).IsRequired();
            e.HasIndex(x => x.ProductName).IsUnique();
            e.Property(x => x.Brand).HasMaxLength(100);
            e.Property(x => x.Model).HasMaxLength(100);
            e.Property(x => x.CostPrice).HasPrecision(10, 2);
            e.Property(x => x.SalePrice).HasPrecision(10, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasOne(x => x.Category).WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        });

        // ─── Customer ───────────────────────────────────────────────────
        b.Entity<Customer>(e =>
        {
            e.ToTable("Customers");
            e.HasKey(x => x.CustomerId);
            e.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            e.Property(x => x.LastName).HasMaxLength(100).IsRequired();
            e.Property(x => x.CNIC).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.CNIC).IsUnique();
            e.Property(x => x.Phone).HasMaxLength(20).IsRequired();
            e.Property(x => x.MonthlyIncome).HasPrecision(10, 2);
            e.Property(x => x.Status).HasMaxLength(50);
        });

        // ─── Guarantor ──────────────────────────────────────────────────
        b.Entity<Guarantor>(e =>
        {
            e.ToTable("Guarantors");
            e.HasKey(x => x.GuarantorId);
            e.Property(x => x.CNIC).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.CNIC).IsUnique();
            e.Property(x => x.MonthlyIncome).HasPrecision(10, 2);
            e.HasOne(x => x.Customer).WithMany(x => x.GuarantorProfiles)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── PlanGuarantor (junction) ───────────────────────────────────
        b.Entity<PlanGuarantor>(e =>
        {
            e.ToTable("PlanGuarantors");
            e.HasKey(x => x.PlanGuarantorId);
            e.HasIndex(x => new { x.PlanId, x.GuarantorId }).IsUnique();
            e.HasOne(x => x.Plan).WithMany(x => x.PlanGuarantors)
                .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Guarantor).WithMany(x => x.PlanGuarantors)
                .HasForeignKey(x => x.GuarantorId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── InstallmentPlan ────────────────────────────────────────────
        b.Entity<InstallmentPlan>(e =>
        {
            e.ToTable("InstallmentPlans");
            e.HasKey(x => x.PlanId);
            e.Property(x => x.ProductSalePrice).HasPrecision(12, 2);
            e.Property(x => x.ProductCostPrice).HasPrecision(10, 2);
            e.Property(x => x.DownPayment).HasPrecision(12, 2);
            e.Property(x => x.LoanAmount).HasPrecision(12, 2);
            e.Property(x => x.MonthlyInstallment).HasPrecision(12, 2);
            e.Property(x => x.TotalPayable).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Property(x => x.ApprovedBy).HasMaxLength(100);
            e.Ignore(x => x.GrandTotal);
            e.HasOne(x => x.Customer).WithMany(x => x.Plans)
                .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Product).WithMany(x => x.Plans)
                .HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── InstallmentPayment ─────────────────────────────────────────
        b.Entity<InstallmentPayment>(e =>
        {
            e.ToTable("InstallmentPayments");
            e.HasKey(x => x.PaymentId);
            e.HasIndex(x => new { x.PlanId, x.InstallmentNumber }).IsUnique();
            e.Property(x => x.AmountDue).HasPrecision(12, 2);
            e.Property(x => x.AmountPaid).HasPrecision(12, 2);
            e.Property(x => x.PenaltyAmount).HasPrecision(12, 2);
            e.Property(x => x.CostRecoveryAmount).HasPrecision(12, 2);
            e.Property(x => x.ProfitAmount).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.Ignore(x => x.Outstanding);
            e.HasOne(x => x.Plan).WithMany(x => x.Installments)
                .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── PaymentTransaction ─────────────────────────────────────────
        b.Entity<PaymentTransaction>(e =>
        {
            e.ToTable("PaymentTransactions");
            e.HasKey(x => x.TransactionId);
            e.Property(x => x.AmountReceived).HasPrecision(12, 2);
            e.HasOne(x => x.Plan).WithMany(x => x.PaymentTransactions)
                .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Installment).WithMany(x => x.Transactions)
                .HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Investor ───────────────────────────────────────────────────
        b.Entity<Investor>(e =>
        {
            e.ToTable("Investors");
            e.HasKey(x => x.InvestorId);
            e.Property(x => x.CNIC).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.CNIC).IsUnique();
            e.Property(x => x.DefaultProfitRate).HasPrecision(5, 2);
        });

        // ─── Investment ─────────────────────────────────────────────────
        b.Entity<Investment>(e =>
        {
            e.ToTable("Investments");
            e.HasKey(x => x.InvestmentId);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.ProfitRate).HasPrecision(5, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasOne(x => x.Investor).WithMany(x => x.Investments)
                .HasForeignKey(x => x.InvestorId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── PlanFunding ────────────────────────────────────────────────
        b.Entity<PlanFunding>(e =>
        {
            e.ToTable("PlanFunding");
            e.HasKey(x => x.PlanFundingId);
            e.HasIndex(x => new { x.PlanId, x.InvestmentId }).IsUnique();
            e.Property(x => x.AmountAllocated).HasPrecision(12, 2);
            e.HasOne(x => x.Plan).WithMany(x => x.PlanFundings)
                .HasForeignKey(x => x.PlanId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Investment).WithMany(x => x.PlanFundings)
                .HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── ProfitPayment ──────────────────────────────────────────────
        b.Entity<ProfitPayment>(e =>
        {
            e.ToTable("ProfitPayments");
            e.HasKey(x => x.ProfitPaymentId);
            e.Property(x => x.ProfitAmount).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasOne(x => x.Investment).WithMany(x => x.ProfitPayments)
                .HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── Withdrawal ─────────────────────────────────────────────────
        b.Entity<Withdrawal>(e =>
        {
            e.ToTable("Withdrawals");
            e.HasKey(x => x.WithdrawalId);
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.Status).HasMaxLength(50);
            e.HasOne(x => x.Investment).WithMany(x => x.Withdrawals)
                .HasForeignKey(x => x.InvestmentId).OnDelete(DeleteBehavior.Restrict);
        });

        // ─── CashLedger (populated only by DB triggers -- read-only from the API) ──
        b.Entity<CashLedger>(e =>
        {
            e.ToTable("CashLedger");
            e.HasKey(x => x.LedgerId);
            e.Property(x => x.TransactionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Direction).HasMaxLength(3).IsRequired();
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.ReferenceTable).HasMaxLength(50);
        });

        // ─── User (authentication) ──────────────────────────────────────
        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Username).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Username).IsUnique();
            e.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
            e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        });

        // ─── Expense ─────────────────────────────────────────────────────
        b.Entity<Expense>(e =>
        {
            e.ToTable("Expenses");
            e.HasKey(x => x.ExpenseId);
            e.Property(x => x.Category).HasMaxLength(50).IsRequired();
            e.Property(x => x.Amount).HasPrecision(12, 2);
            e.Property(x => x.PaidTo).HasMaxLength(100);
            e.Property(x => x.PaymentMethod).HasMaxLength(50);
            e.Property(x => x.ReferenceNo).HasMaxLength(100);
        });

        // ─── Views: keyless, mapped read-only ───────────────────────────
        b.Entity<PlanSummary>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_PlanSummary");
        });
        b.Entity<InvestorSummary>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_InvestorSummary");
        });
        b.Entity<InvestorLedgerEntry>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_InvestorLedger");
        });
        b.Entity<PlanFundingSummary>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_PlanFundingSummary");
        });
        b.Entity<ProfitByPeriod>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_ProfitByPeriod");
            e.Property(x => x.Year).HasColumnName("Year");
            e.Property(x => x.Month).HasColumnName("Month");
        });
        b.Entity<CashLedgerByPeriod>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_CashLedgerByPeriod");
            e.Property(x => x.Year).HasColumnName("Year");
            e.Property(x => x.Month).HasColumnName("Month");
        });
        b.Entity<PendingInstallment>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_PendingInstallments");
        });
        b.Entity<GuarantorPlanCount>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_GuarantorPlanCount");
        });
        b.Entity<CashInHand>(e =>
        {
            e.HasNoKey();
            e.ToView("vw_CashInHand");
            e.Property(x => x.CashInHandAmount).HasColumnName("CashInHand");
        });
    }
}