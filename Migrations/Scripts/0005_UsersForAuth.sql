USE [InstallmentBusiness]
GO

-- ═══════════════════════════════════════════════════════════════════════════
-- MIGRATION: Users table (backs JWT authentication)
-- Purely additive -- does not touch any existing table. Idempotent: safe to
-- run whether this has already been applied or not.
-- ═══════════════════════════════════════════════════════════════════════════

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [dbo].[Users](
        [UserId] [int] IDENTITY(1,1) NOT NULL,
        [Username] [varchar](100) NOT NULL,
        [PasswordHash] [varchar](500) NOT NULL,
        [DisplayName] [varchar](100) NOT NULL,
        [IsActive] [bit] NOT NULL,
        [CreatedAt] [datetime] NULL,
    PRIMARY KEY CLUSTERED
    (
        [UserId] ASC
    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    ) ON [PRIMARY]

    ALTER TABLE [dbo].[Users] ADD DEFAULT ((1)) FOR [IsActive]
    ALTER TABLE [dbo].[Users] ADD DEFAULT (getutcdate()) FOR [CreatedAt]
    ALTER TABLE [dbo].[Users] ADD CONSTRAINT [UQ_Users_Username] UNIQUE ([Username])
END
GO

-- Note: the API itself seeds a default 'admin' account (password 'ChangeMe123!')
-- on first run if this table is empty -- see Program.cs. Nothing to insert
-- manually here; just make sure this table exists before running the API
-- for the first time after this update.
