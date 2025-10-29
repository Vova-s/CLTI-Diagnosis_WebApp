-- SQL Server Distributed Cache Table for Session Storage
-- This ensures sessions survive server restarts
-- Run this script on your SQL Server database

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SessionCache')
BEGIN
    CREATE TABLE [dbo].[SessionCache] (
        [Id] NVARCHAR(449) NOT NULL,
        [Value] VARBINARY(MAX) NOT NULL,
        [ExpiresAtTime] DATETIME2 NOT NULL,
        [SlidingExpirationInSeconds] BIGINT NULL,
        [AbsoluteExpiration] DATETIME2 NULL,
        CONSTRAINT [PK_SessionCache] PRIMARY KEY CLUSTERED ([Id] ASC)
    );

    CREATE NONCLUSTERED INDEX [IX_SessionCache_ExpiresAtTime]
        ON [dbo].[SessionCache]([ExpiresAtTime] ASC);

    PRINT 'SessionCache table created successfully';
END
ELSE
BEGIN
    PRINT 'SessionCache table already exists';
END
GO

