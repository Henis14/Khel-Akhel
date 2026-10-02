USE KhelAkhe_CRM;

-- 1. Customer Master Table
CREATE TABLE [dbo].[drs_customer_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Account_no] VARCHAR(9) NOT NULL,
    [FirstName] NVARCHAR(100) NOT NULL,
    [LastName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(255) NOT NULL,
    [MobileNo] VARCHAR(20) NOT NULL,
    [PasswordHash] NVARCHAR(500) NOT NULL,
    [IsAdmin] BIT NOT NULL DEFAULT 0,
    [IsEmailVerified] BIT NOT NULL DEFAULT 0,
    [IsMobileVerified] BIT NOT NULL DEFAULT 0,
    [LastLogin] DATETIME NULL,
    [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
    [LastFailedLoginDate] DATETIME NULL,
    [AccountLockedUntil] DATETIME NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- Sequences for Auto-generation
CREATE SEQUENCE [dbo].[Seq_CustomerAccount]
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

CREATE SEQUENCE [dbo].[Seq_ProductCode]
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;
GO

-- 2. Customer Address Master Table
CREATE TABLE [dbo].[drs_customer_address_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [customer_id] BIGINT NOT NULL,
    [AddressTitle] NVARCHAR(50) NULL,
    [AddressType] NVARCHAR(50) NULL,
    [FullName] NVARCHAR(200) NOT NULL,
    [MobileNo] VARCHAR(20) NOT NULL,
    [AddressLine1] NVARCHAR(255) NOT NULL,
    [AddressLine2] NVARCHAR(255) NULL,
    [Landmark] NVARCHAR(150) NULL,
    [City] NVARCHAR(100) NOT NULL,
    [State] NVARCHAR(100) NOT NULL,
    [Country] NVARCHAR(100) NOT NULL,
    [Pincode] VARCHAR(20) NOT NULL,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- 3. Product Category Master Table
CREATE TABLE [dbo].[drs_product_category_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CategoryName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- 4. Product Master Table
CREATE TABLE [dbo].[drs_product_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [product_category_id] BIGINT NOT NULL,
    [ProductName] NVARCHAR(200) NOT NULL,
    [ProductCode] VARCHAR(50) NOT NULL,
    [SKU] VARCHAR(50) NOT NULL,
    [ShortDescription] NVARCHAR(500) NULL,
    [Description] NVARCHAR(MAX) NULL,
    [MRP] DECIMAL(18,2) NOT NULL,
    [SellingPrice] DECIMAL(18,2) NOT NULL,
    [IsFeatured] BIT NOT NULL DEFAULT 0,
    [IsNewArrival] BIT NOT NULL DEFAULT 0,
    [IsBestSeller] BIT NOT NULL DEFAULT 0,
    [IsTrending] BIT NOT NULL DEFAULT 0,
    [IsCustomerFavourite] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL,
    CONSTRAINT [CK_drs_product_mst_MRP] CHECK ([MRP] >= 0),
    CONSTRAINT [CK_drs_product_mst_SellingPrice] CHECK ([SellingPrice] >= 0)
);
GO

-- 5. Product Stock Master Table
CREATE TABLE [dbo].[drs_product_stock_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [product_id] BIGINT NOT NULL,
    [AvailableQty] INT NOT NULL DEFAULT 0,
    [ReservedQty] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL,
    CONSTRAINT [CK_drs_product_stock_mst_AvailableQty] CHECK ([AvailableQty] >= 0),
    CONSTRAINT [CK_drs_product_stock_mst_ReservedQty] CHECK ([ReservedQty] >= 0)
);
GO

-- 6. Product Image Master Table
CREATE TABLE [dbo].[drs_product_image_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [product_id] BIGINT NOT NULL,
    [ImagePath] NVARCHAR(500) NOT NULL,
    [DisplayOrder] INT NOT NULL DEFAULT 0,
    [IsDefault] BIT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- 7. Refresh Token Master Table
CREATE TABLE [dbo].[drs_refresh_token_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [customer_id] BIGINT NOT NULL,
    [TokenHash] NVARCHAR(500) NOT NULL,
    [CreatedUtc] DATETIME NOT NULL DEFAULT GETUTCDATE(),
    [ExpiresUtc] DATETIME NOT NULL,
    [RevokedUtc] DATETIME NULL,
    [ReplacedByTokenHash] NVARCHAR(500) NULL,
    [CreatedByIp] VARCHAR(50) NULL,
    [RevokedByIp] VARCHAR(50) NULL,
    [DeviceName] NVARCHAR(200) NULL,
    [Browser] NVARCHAR(100) NULL,
    [OperatingSystem] NVARCHAR(100) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- 8. Login History Master Table
CREATE TABLE [dbo].[drs_login_history_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [customer_id] BIGINT NULL,
    [LoginType] VARCHAR(50) NOT NULL,
    [DeviceInfo] NVARCHAR(255) NULL,
    [IPAddress] VARCHAR(50) NULL,
    [IsSuccess] BIT NOT NULL,
    [FailureReason] NVARCHAR(255) NULL,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- 9. Audit Master Table
CREATE TABLE [dbo].[drs_audit_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [customer_id] BIGINT NULL,
    [ModuleName] NVARCHAR(100) NOT NULL,
    [Action] NVARCHAR(100) NOT NULL,
    [TableName] NVARCHAR(100) NULL,
    [EntityId] BIGINT NULL,
    [OldValue] NVARCHAR(MAX) NULL,
    [NewValue] NVARCHAR(MAX) NULL,
    [IPAddress] VARCHAR(50) NULL,
    [UserAgent] NVARCHAR(500) NULL,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE()
);
GO

-- 10. Error Message Master Table
CREATE TABLE [dbo].[drs_error_message_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [MessageCode] VARCHAR(100) NOT NULL,
    [MessageText] NVARCHAR(500) NOT NULL,
    [MessageType] VARCHAR(20) NOT NULL DEFAULT 'ERROR',
    [HttpStatusCode] INT NOT NULL DEFAULT 400,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);
GO

-- 11. Country Master Table

CREATE TABLE [dbo].[drs_country_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CountryName] NVARCHAR(100) NOT NULL,
    [CountryCode] VARCHAR(10) NOT NULL,
    [PhoneCode] VARCHAR(10) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);

GO

-- 12. State Master Table

CREATE TABLE [dbo].[drs_state_mst] (
    [ID] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [country_id] BIGINT NOT NULL,
    [StateName] NVARCHAR(100) NOT NULL,
    [StateCode] VARCHAR(20) NULL,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [IsDeleted] BIT NOT NULL DEFAULT 0,
    [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
    [ModifiedDate] DATETIME NULL
);

GO

-- Alter Customer Address Table for country_id and state_id columns
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]') AND type in (N'U'))
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]') AND name = 'country_id')
    BEGIN
        ALTER TABLE [dbo].[drs_customer_address_mst] ADD [country_id] BIGINT NULL;
    END;
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]') AND name = 'state_id')
    BEGIN
        ALTER TABLE [dbo].[drs_customer_address_mst] ADD [state_id] BIGINT NULL;
    END;
END;
GO





