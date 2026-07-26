USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- 0000: The ORIGINAL schema -- the tables and views that existed before this
-- project began. Everything after this script (0001 onward) only ever
-- assumed these already existed; this is what makes a genuinely blank
-- database buildable from nothing.
--
-- Honest note: unlike every other script in this project, this one is
-- reconstructed from this project's own history rather than diffed against
-- a fresh live export -- there was no live copy of the pre-project schema
-- left to verify against by the time this was written. Recommended: run
-- this against a throwaway test database once and compare the result to
-- the original tables in an existing, already-migrated database, the same
-- way every other script here was verified.
--
-- Idempotent: safe to run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProductCategories')
BEGIN
	CREATE TABLE [dbo].[ProductCategories](
		[CategoryId] [int] IDENTITY(1,1) NOT NULL,
		[CategoryName] [varchar](100) NOT NULL,
		[Description] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([CategoryId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[ProductCategories] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[ProductCategories] ADD CONSTRAINT [UQ_ProductCategories_CategoryName] UNIQUE ([CategoryName])
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Products')
BEGIN
	CREATE TABLE [dbo].[Products](
		[ProductId] [int] IDENTITY(1,1) NOT NULL,
		[ProductName] [varchar](150) NOT NULL,
		[Brand] [varchar](100) NULL,
		[Model] [varchar](100) NULL,
		[CategoryId] [int] NULL,
		[CostPrice] [decimal](10, 2) NOT NULL,
		[SalePrice] [decimal](10, 2) NOT NULL,
		[Status] [varchar](50) NULL,
		[Description] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([ProductId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Products] ADD DEFAULT ('Available') FOR [Status]
	ALTER TABLE [dbo].[Products] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Products] ADD CONSTRAINT [UQ_Products_ProductName] UNIQUE ([ProductName])
	ALTER TABLE [dbo].[Products] WITH CHECK ADD FOREIGN KEY([CategoryId]) REFERENCES [dbo].[ProductCategories] ([CategoryId]) ON DELETE SET NULL
	ALTER TABLE [dbo].[Products] WITH CHECK ADD CHECK (([CostPrice]>=(0)))
	ALTER TABLE [dbo].[Products] WITH CHECK ADD CHECK (([SalePrice]>=(0)))
	ALTER TABLE [dbo].[Products] WITH CHECK ADD CHECK (([Status]='Available' OR [Status]='OutOfStock' OR [Status]='Discontinued'))
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Customers')
BEGIN
	CREATE TABLE [dbo].[Customers](
		[CustomerId] [int] IDENTITY(1,1) NOT NULL,
		[FirstName] [varchar](100) NOT NULL,
		[LastName] [varchar](100) NOT NULL,
		[CNIC] [varchar](20) NOT NULL,
		[Phone] [varchar](20) NOT NULL,
		[AlternatePhone] [varchar](20) NULL,
		[Email] [varchar](100) NULL,
		[Address] [varchar](255) NULL,
		[City] [varchar](50) NULL,
		[DateOfBirth] [date] NULL,
		[Occupation] [varchar](100) NULL,
		[EmployerName] [varchar](100) NULL,
		[MonthlyIncome] [decimal](10, 2) NULL,
		[Status] [varchar](50) NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
		[UpdatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([CustomerId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Customers] ADD DEFAULT ('Active') FOR [Status]
	ALTER TABLE [dbo].[Customers] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Customers] ADD DEFAULT (getutcdate()) FOR [UpdatedAt]
	ALTER TABLE [dbo].[Customers] ADD CONSTRAINT [UQ_Customers_CNIC] UNIQUE ([CNIC])
	ALTER TABLE [dbo].[Customers] WITH CHECK ADD CHECK (([Status]='Active' OR [Status]='Inactive' OR [Status]='Blacklisted'))
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Guarantors')
BEGIN
	CREATE TABLE [dbo].[Guarantors](
		[GuarantorId] [int] IDENTITY(1,1) NOT NULL,
		[CustomerId] [int] NOT NULL,
		[FirstName] [varchar](100) NOT NULL,
		[LastName] [varchar](100) NOT NULL,
		[CNIC] [varchar](20) NOT NULL,
		[Phone] [varchar](20) NOT NULL,
		[Relation] [varchar](50) NULL,
		[Address] [varchar](255) NULL,
		[Occupation] [varchar](100) NULL,
		[MonthlyIncome] [decimal](10, 2) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([GuarantorId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Guarantors] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Guarantors] ADD CONSTRAINT [UQ_Guarantors_CNIC] UNIQUE ([CNIC])
	ALTER TABLE [dbo].[Guarantors] WITH CHECK ADD FOREIGN KEY([CustomerId]) REFERENCES [dbo].[Customers] ([CustomerId])
END
GO

-- Original form: includes GuarantorId (dropped later by 0003) and no
-- ProductCostPrice (added later by 0004). Status originally had no
-- 'Proposed' value and defaulted to 'Active' (both changed by 0003).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InstallmentPlans')
BEGIN
	CREATE TABLE [dbo].[InstallmentPlans](
		[PlanId] [int] IDENTITY(1,1) NOT NULL,
		[CustomerId] [int] NOT NULL,
		[ProductId] [int] NOT NULL,
		[GuarantorId] [int] NULL,
		[ProductSalePrice] [decimal](12, 2) NOT NULL,
		[DownPayment] [decimal](12, 2) NOT NULL,
		[LoanAmount] [decimal](12, 2) NOT NULL,
		[TenureMonths] [int] NOT NULL,
		[MonthlyInstallment] [decimal](12, 2) NOT NULL,
		[TotalPayable] [decimal](12, 2) NOT NULL,
		[StartDate] [date] NOT NULL,
		[EndDate] [date] NOT NULL,
		[Status] [varchar](50) NULL,
		[ApprovedBy] [varchar](100) NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([PlanId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[InstallmentPlans] ADD DEFAULT ('Active') FOR [Status]
	ALTER TABLE [dbo].[InstallmentPlans] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[InstallmentPlans] WITH CHECK ADD FOREIGN KEY([CustomerId]) REFERENCES [dbo].[Customers] ([CustomerId])
	ALTER TABLE [dbo].[InstallmentPlans] WITH CHECK ADD FOREIGN KEY([ProductId]) REFERENCES [dbo].[Products] ([ProductId])
	ALTER TABLE [dbo].[InstallmentPlans] WITH CHECK ADD FOREIGN KEY([GuarantorId]) REFERENCES [dbo].[Guarantors] ([GuarantorId]) ON DELETE SET NULL
	ALTER TABLE [dbo].[InstallmentPlans] WITH CHECK ADD CHECK (([TenureMonths]>=(6) AND [TenureMonths]<=(30)))
	ALTER TABLE [dbo].[InstallmentPlans] WITH CHECK ADD CHECK (([Status]='Active' OR [Status]='Completed' OR [Status]='Defaulted' OR [Status]='Cancelled'))
END
GO

-- Original form: no UNIQUE(PlanId, InstallmentNumber) and no cost+profit
-- CHECK constraint (both added later by 0001).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InstallmentPayments')
BEGIN
	CREATE TABLE [dbo].[InstallmentPayments](
		[PaymentId] [int] IDENTITY(1,1) NOT NULL,
		[PlanId] [int] NOT NULL,
		[InstallmentNumber] [int] NOT NULL,
		[AmountDue] [decimal](12, 2) NOT NULL,
		[AmountPaid] [decimal](12, 2) NULL,
		[DueDate] [date] NOT NULL,
		[PaidDate] [date] NULL,
		[PenaltyAmount] [decimal](12, 2) NULL,
		[CostRecoveryAmount] [decimal](12, 2) NULL,
		[ProfitAmount] [decimal](12, 2) NULL,
		[PaymentMethod] [varchar](50) NULL,
		[ReferenceNo] [varchar](100) NULL,
		[Status] [varchar](50) NULL,
		[ReceivedBy] [varchar](100) NULL,
		[Notes] [varchar](max) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([PaymentId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT ((0)) FOR [AmountPaid]
	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT ((0)) FOR [PenaltyAmount]
	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT ((0)) FOR [CostRecoveryAmount]
	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT ((0)) FOR [ProfitAmount]
	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT ('Pending') FOR [Status]
	ALTER TABLE [dbo].[InstallmentPayments] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[InstallmentPayments] WITH CHECK ADD FOREIGN KEY([PlanId]) REFERENCES [dbo].[InstallmentPlans] ([PlanId])
	ALTER TABLE [dbo].[InstallmentPayments] WITH CHECK ADD CHECK (([Status]='Pending' OR [Status]='PartiallyPaid' OR [Status]='Paid' OR [Status]='Overdue' OR [Status]='Waived'))
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Investors')
BEGIN
	CREATE TABLE [dbo].[Investors](
		[InvestorId] [int] IDENTITY(1,1) NOT NULL,
		[FirstName] [varchar](100) NOT NULL,
		[LastName] [varchar](100) NOT NULL,
		[CNIC] [varchar](20) NOT NULL,
		[Phone] [varchar](20) NULL,
		[Email] [varchar](100) NULL,
		[Address] [varchar](255) NULL,
		[DefaultProfitRate] [decimal](5, 2) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([InvestorId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Investors] ADD DEFAULT ((15.00)) FOR [DefaultProfitRate]
	ALTER TABLE [dbo].[Investors] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Investors] ADD CONSTRAINT [UQ_Investors_CNIC] UNIQUE ([CNIC])
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Investments')
BEGIN
	CREATE TABLE [dbo].[Investments](
		[InvestmentId] [int] IDENTITY(1,1) NOT NULL,
		[InvestorId] [int] NOT NULL,
		[Amount] [decimal](12, 2) NOT NULL,
		[InvestmentDate] [date] NOT NULL,
		[ProfitRate] [decimal](5, 2) NULL,
		[Status] [varchar](50) NULL,
		[MaturityDate] [date] NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([InvestmentId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[Investments] ADD DEFAULT ((15.00)) FOR [ProfitRate]
	ALTER TABLE [dbo].[Investments] ADD DEFAULT ('Active') FOR [Status]
	ALTER TABLE [dbo].[Investments] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[Investments] WITH CHECK ADD FOREIGN KEY([InvestorId]) REFERENCES [dbo].[Investors] ([InvestorId])
	ALTER TABLE [dbo].[Investments] WITH CHECK ADD CHECK (([Status]='Active' OR [Status]='Withdrawn' OR [Status]='Matured' OR [Status]='Cancelled'))
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProfitPayments')
BEGIN
	CREATE TABLE [dbo].[ProfitPayments](
		[ProfitPaymentId] [int] IDENTITY(1,1) NOT NULL,
		[InvestmentId] [int] NOT NULL,
		[ProfitAmount] [decimal](12, 2) NOT NULL,
		[PaymentDate] [date] NOT NULL,
		[PaymentMethod] [varchar](50) NULL,
		[Status] [varchar](50) NULL,
		[CreatedAt] [datetime] NULL,
	PRIMARY KEY CLUSTERED ([ProfitPaymentId] ASC)
	) ON [PRIMARY]

	ALTER TABLE [dbo].[ProfitPayments] ADD DEFAULT ('Paid') FOR [Status]
	ALTER TABLE [dbo].[ProfitPayments] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
	ALTER TABLE [dbo].[ProfitPayments] WITH CHECK ADD FOREIGN KEY([InvestmentId]) REFERENCES [dbo].[Investments] ([InvestmentId])
	ALTER TABLE [dbo].[ProfitPayments] WITH CHECK ADD CHECK (([Status]='Paid' OR [Status]='Pending' OR [Status]='Cancelled'))
END
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- Original views. vw_ProfitByPeriod is never touched by any later script, so
-- it needs to be right here. vw_InvestorSummary's original form is
-- immediately superseded by 0007 -- it exists here only for historical
-- fidelity to what originally existed, not because its exact wording matters.
-- ═══════════════════════════════════════════════════════════════════════════

IF OBJECT_ID('[dbo].[vw_ProfitByPeriod]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_ProfitByPeriod]
GO
CREATE VIEW [dbo].[vw_ProfitByPeriod] AS
SELECT
    YEAR(PaidDate) AS [Year],
    MONTH(PaidDate) AS [Month],
    DATENAME(MONTH, PaidDate) + ' ' + CAST(YEAR(PaidDate) AS VARCHAR) AS PeriodName,
    SUM(ProfitAmount) AS TotalProfit,
    SUM(CostRecoveryAmount) AS TotalCostRecovery,
    SUM(AmountPaid) AS TotalCollected,
    COUNT(*) AS PaymentsReceived
FROM [dbo].[InstallmentPayments]
WHERE PaidDate IS NOT NULL AND (Status = 'Paid' OR Status = 'PartiallyPaid')
GROUP BY YEAR(PaidDate), MONTH(PaidDate), DATENAME(MONTH, PaidDate) + ' ' + CAST(YEAR(PaidDate) AS VARCHAR);
GO

IF OBJECT_ID('[dbo].[vw_InvestorSummary]', 'V') IS NOT NULL
    DROP VIEW [dbo].[vw_InvestorSummary]
GO
CREATE VIEW [dbo].[vw_InvestorSummary] AS
SELECT
    inv.InvestorId,
    inv.FirstName + ' ' + inv.LastName AS InvestorName,
    ISNULL(SUM(i.Amount), 0) AS TotalInvested,
    ISNULL(SUM(CASE WHEN i.Status = 'Active' THEN i.Amount ELSE 0 END), 0) AS ActiveInvestment,
    ISNULL((SELECT SUM(pp.ProfitAmount) FROM [dbo].[ProfitPayments] pp
            JOIN [dbo].[Investments] i2 ON pp.InvestmentId = i2.InvestmentId
            WHERE i2.InvestorId = inv.InvestorId AND pp.Status = 'Paid'), 0) AS TotalProfitPaid,
    COUNT(CASE WHEN i.Status = 'Active' THEN 1 END) AS ActiveInvestments
FROM [dbo].[Investors] inv
LEFT JOIN [dbo].[Investments] i ON inv.InvestorId = i.InvestorId
GROUP BY inv.InvestorId, inv.FirstName, inv.LastName;
GO
