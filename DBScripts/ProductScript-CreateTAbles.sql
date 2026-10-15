USE KhelAkhe_CRM;

-- Customer Master Table
CREATE TABLE [dbo].[drs_customer_mst] (
    [ID]                  BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [Account_no]          VARCHAR (9)    NOT NULL,
    [FirstName]           NVARCHAR (100) NOT NULL,
    [LastName]            NVARCHAR (100) NOT NULL,
    [Email]               NVARCHAR (255) NOT NULL,
    [MobileNo]            VARCHAR (20)   NOT NULL,
    [PasswordHash]        NVARCHAR (500) NOT NULL,
    [IsAdmin]             BIT            DEFAULT 0 NOT NULL,
    [IsEmailVerified]     BIT            DEFAULT 0 NOT NULL,
    [IsMobileVerified]    BIT            DEFAULT 0 NOT NULL,
    [LastLogin]           DATETIME       NULL,
    [FailedLoginAttempts] INT            DEFAULT 0 NOT NULL,
    [LastFailedLoginDate] DATETIME       NULL,
    [AccountLockedUntil]  DATETIME       NULL,
    [IsActive]            BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]           BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]         DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate]        DATETIME       NULL
);


GO
-- Customer and Product Code Sequences
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
-- Customer Address Master Table
CREATE TABLE [dbo].[drs_customer_address_mst] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [customer_id]  BIGINT         NOT NULL,
    [AddressTitle] NVARCHAR (50)  NULL,
    [AddressType]  NVARCHAR (50)  NULL,
    [FullName]     NVARCHAR (200) NOT NULL,
    [MobileNo]     VARCHAR (20)   NOT NULL,
    [AddressLine1] NVARCHAR (255) NOT NULL,
    [AddressLine2] NVARCHAR (255) NULL,
    [Landmark]     NVARCHAR (150) NULL,
    [City]         NVARCHAR (100) NOT NULL,
    [State]        NVARCHAR (100) NOT NULL,
    [Country]      NVARCHAR (100) NOT NULL,
    [Pincode]      VARCHAR (20)   NOT NULL,
    [IsDefault]    BIT            DEFAULT 0 NOT NULL,
    [IsActive]     BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME       NULL
);


GO
-- Product Category Master Table
CREATE TABLE [dbo].[drs_product_category_mst] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [CategoryName] NVARCHAR (100) NOT NULL,
    [Description]  NVARCHAR (500) NULL,
    [DisplayOrder] INT            DEFAULT 0 NOT NULL,
    [IsActive]     BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME       NULL
);


GO
-- Product Master Table
CREATE TABLE [dbo].[drs_product_mst] (
    [ID]                  BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [product_category_id] BIGINT          NOT NULL,
    [ProductName]         NVARCHAR (200)  NOT NULL,
    [ProductCode]         VARCHAR (50)    NOT NULL,
    [SKU]                 VARCHAR (50)    NOT NULL,
    [ShortDescription]    NVARCHAR (500)  NULL,
    [Description]         NVARCHAR (MAX)  NULL,
    [MRP]                 DECIMAL (18, 2) NOT NULL,
    [SellingPrice]        DECIMAL (18, 2) NOT NULL,
    [IsFeatured]          BIT             DEFAULT 0 NOT NULL,
    [IsNewArrival]        BIT             DEFAULT 0 NOT NULL,
    [IsBestSeller]        BIT             DEFAULT 0 NOT NULL,
    [IsTrending]          BIT             DEFAULT 0 NOT NULL,
    [IsCustomerFavourite] BIT             DEFAULT 0 NOT NULL,
    [IsActive]            BIT             DEFAULT 1 NOT NULL,
    [IsDeleted]           BIT             DEFAULT 0 NOT NULL,
    [CreatedDate]         DATETIME        DEFAULT GETDATE() NOT NULL,
    [ModifiedDate]        DATETIME        NULL,
    CONSTRAINT [CK_drs_product_mst_MRP] CHECK ([MRP] >= 0),
    CONSTRAINT [CK_drs_product_mst_SellingPrice] CHECK ([SellingPrice] >= 0)
);


GO
-- Product Stock Master Table
CREATE TABLE [dbo].[drs_product_stock_mst] (
    [ID]           BIGINT   IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [product_id]   BIGINT   NOT NULL,
    [AvailableQty] INT      DEFAULT 0 NOT NULL,
    [ReservedQty]  INT      DEFAULT 0 NOT NULL,
    [IsActive]     BIT      DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT      DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME NULL,
    CONSTRAINT [CK_drs_product_stock_mst_AvailableQty] CHECK ([AvailableQty] >= 0),
    CONSTRAINT [CK_drs_product_stock_mst_ReservedQty] CHECK ([ReservedQty] >= 0)
);


GO
-- Product Image Master Table
CREATE TABLE [dbo].[drs_product_image_mst] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [product_id]   BIGINT         NOT NULL,
    [ImagePath]    NVARCHAR (500) NOT NULL,
    [DisplayOrder] INT            DEFAULT 0 NOT NULL,
    [IsDefault]    BIT            DEFAULT 0 NOT NULL,
    [IsActive]     BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME       NULL
);


GO
-- Refresh Token Master Table
CREATE TABLE [dbo].[drs_refresh_token_mst] (
    [ID]                  BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [customer_id]         BIGINT         NOT NULL,
    [TokenHash]           NVARCHAR (500) NOT NULL,
    [CreatedUtc]          DATETIME       DEFAULT GETUTCDATE() NOT NULL,
    [ExpiresUtc]          DATETIME       NOT NULL,
    [RevokedUtc]          DATETIME       NULL,
    [ReplacedByTokenHash] NVARCHAR (500) NULL,
    [CreatedByIp]         VARCHAR (50)   NULL,
    [RevokedByIp]         VARCHAR (50)   NULL,
    [DeviceName]          NVARCHAR (200) NULL,
    [Browser]             NVARCHAR (100) NULL,
    [OperatingSystem]     NVARCHAR (100) NULL,
    [IsActive]            BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]           BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]         DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate]        DATETIME       NULL
);


GO
-- Login History Master Table
CREATE TABLE [dbo].[drs_login_history_mst] (
    [ID]            BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [customer_id]   BIGINT         NULL,
    [LoginType]     VARCHAR (50)   NOT NULL,
    [DeviceInfo]    NVARCHAR (255) NULL,
    [IPAddress]     VARCHAR (50)   NULL,
    [IsSuccess]     BIT            NOT NULL,
    [FailureReason] NVARCHAR (255) NULL,
    [CreatedDate]   DATETIME       DEFAULT GETDATE() NOT NULL
);


GO
-- Audit Master Table
CREATE TABLE [dbo].[drs_audit_mst] (
    [ID]          BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [customer_id] BIGINT         NULL,
    [ModuleName]  NVARCHAR (100) NOT NULL,
    [Action]      NVARCHAR (100) NOT NULL,
    [TableName]   NVARCHAR (100) NULL,
    [EntityId]    BIGINT         NULL,
    [OldValue]    NVARCHAR (MAX) NULL,
    [NewValue]    NVARCHAR (MAX) NULL,
    [IPAddress]   VARCHAR (50)   NULL,
    [UserAgent]   NVARCHAR (500) NULL,
    [CreatedDate] DATETIME       DEFAULT GETDATE() NOT NULL
);


GO
-- Error Message Master Table
CREATE TABLE [dbo].[drs_error_message_mst] (
    [ID]             BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [MessageCode]    VARCHAR (100)  NOT NULL,
    [MessageText]    NVARCHAR (500) NOT NULL,
    [MessageType]    VARCHAR (20)   DEFAULT 'ERROR' NOT NULL,
    [HttpStatusCode] INT            DEFAULT 400 NOT NULL,
    [IsActive]       BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]      BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]    DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate]   DATETIME       NULL
);


GO
-- Country Master Table
CREATE TABLE [dbo].[drs_country_mst] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [CountryName]  NVARCHAR (100) NOT NULL,
    [CountryCode]  VARCHAR (10)   NOT NULL,
    [PhoneCode]    VARCHAR (10)   NULL,
    [IsActive]     BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME       NULL
);


GO
-- State Master Table
CREATE TABLE [dbo].[drs_state_mst] (
    [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    [country_id]   BIGINT         NOT NULL,
    [StateName]    NVARCHAR (100) NOT NULL,
    [StateCode]    VARCHAR (20)   NULL,
    [IsActive]     BIT            DEFAULT 1 NOT NULL,
    [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
    [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
    [ModifiedDate] DATETIME       NULL
);


GO
-- Customer Address Location Columns
IF EXISTS (SELECT *
           FROM   sys.objects
           WHERE  object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]')
                  AND type IN (
                      N'U'
                  ))
    BEGIN
        IF NOT EXISTS (SELECT *
                       FROM   sys.columns
                       WHERE  object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]')
                              AND name = 'country_id')
            BEGIN
                ALTER TABLE [dbo].[drs_customer_address_mst]
                    ADD [country_id] BIGINT NULL;
            END
        IF NOT EXISTS (SELECT *
                       FROM   sys.columns
                       WHERE  object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]')
                              AND name = 'state_id')
            BEGIN
                ALTER TABLE [dbo].[drs_customer_address_mst]
                    ADD [state_id] BIGINT NULL;
            END
    END

-- FAQ Master Table
CREATE TABLE drs_faq_mst (
    ID                    INT            IDENTITY (1, 1) PRIMARY KEY,
    Category              NVARCHAR (100),
    Question              NVARCHAR (500),
    Answer                NVARCHAR (MAX),
    DisplayOrder          INT            DEFAULT 0,
    IsPublished           BIT            DEFAULT 0,
    DisplayOnHomePage     BIT            DEFAULT 0,
    DisplayOnFaqPage      BIT            DEFAULT 1,
    DisplayOnCheckoutPage BIT            DEFAULT 0,
    DisplayOnContactPage  BIT            DEFAULT 0,
    DisplayOnFooter       BIT            DEFAULT 0,
    IsActive              BIT            DEFAULT 1,
    IsDeleted             BIT            DEFAULT 0,
    CreatedDate           DATETIME       DEFAULT GETDATE(),
    ModifiedDate          DATETIME       NULL
);


GO

-- Commission Request Sequence
IF NOT EXISTS (SELECT * FROM sys.sequences WHERE name = 'Seq_CommissionRequestNo')
BEGIN
    CREATE SEQUENCE [dbo].[Seq_CommissionRequestNo]
        AS BIGINT
        START WITH 1
        INCREMENT BY 1;
END;
GO

-- Commission Category Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_commission_category_mst]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[drs_commission_category_mst] (
        [ID]           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        [CategoryName] NVARCHAR (100) NOT NULL,
        [Description]  NVARCHAR (500) NULL,
        [DisplayOrder] INT            DEFAULT 0 NOT NULL,
        [IsActive]     BIT            DEFAULT 1 NOT NULL,
        [IsDeleted]    BIT            DEFAULT 0 NOT NULL,
        [CreatedDate]  DATETIME       DEFAULT GETDATE() NOT NULL,
        [ModifiedDate] DATETIME       NULL
    );
END;
GO

-- Commission Request Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_commission_request_mst]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[drs_commission_request_mst] (
        [ID]                       BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        [RequestNo]                VARCHAR (30)   NOT NULL,
        [FullName]                 NVARCHAR (150) NOT NULL,
        [MobileNo]                 VARCHAR (20)   NOT NULL,
        [Email]                    NVARCHAR (255) NOT NULL,
        [category_id]              BIGINT         NOT NULL,
        [OtherCategoryDescription] NVARCHAR (255) NULL,
        [ExpectedTimeline]         DATE           NOT NULL,
        [ProjectLocation]          NVARCHAR (255) NOT NULL,
        [ProjectDescription]       NVARCHAR (MAX) NOT NULL,
        [Keywords]                 NVARCHAR (500) NULL,
        [Status]                   NVARCHAR (50)  DEFAULT 'Submitted' NOT NULL,
        [AdminNotes]               NVARCHAR (MAX) NULL,
        [customer_id]              BIGINT         NULL,
        [IpAddress]                VARCHAR (50)   NULL,
        [UserAgent]                NVARCHAR (500) NULL,
        [IsActive]                 BIT            DEFAULT 1 NOT NULL,
        [IsDeleted]                BIT            DEFAULT 0 NOT NULL,
        [CreatedDate]              DATETIME       DEFAULT GETDATE() NOT NULL,
        [ModifiedDate]             DATETIME       NULL
    );
END;
GO

-- Commission Request Image Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_commission_request_image_mst]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[drs_commission_request_image_mst] (
        [ID]                    BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        [commission_request_id] BIGINT         NOT NULL,
        [OriginalFileName]      NVARCHAR (255) NOT NULL,
        [StoredFileName]        NVARCHAR (255) NOT NULL,
        [StoragePath]           NVARCHAR (500) NOT NULL,
        [ContentType]           VARCHAR (100)  NOT NULL,
        [FileSize]              BIGINT         NOT NULL,
        [DisplayOrder]          INT            DEFAULT 1 NOT NULL,
        [IsActive]              BIT            DEFAULT 1 NOT NULL,
        [IsDeleted]             BIT            DEFAULT 0 NOT NULL,
        [CreatedDate]           DATETIME       DEFAULT GETDATE() NOT NULL,
        [ModifiedDate]          DATETIME       NULL
    );
END;
GO

-- Project Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_project_mst]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[drs_project_mst] (
        [ID]                  BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        [Heading]             NVARCHAR (200)  NOT NULL,
        [Subheading]          NVARCHAR (250)  NULL,
        [ContentType]         NVARCHAR (50)   NULL,
        [ProjectOrEventTitle] NVARCHAR (200)  NULL,
        [DisplayYear]         VARCHAR (50)    NULL,
        [ShortDescription]    NVARCHAR (1000) NULL,
        [DetailedDescription] NVARCHAR (MAX)  NULL,
        [Credits]             NVARCHAR (1000) NULL,
        [Location]            NVARCHAR (200)  NULL,
        [ExternalUrl]         NVARCHAR (500)  NULL,
        [DisplayOrder]        INT             DEFAULT 0 NOT NULL,
        [LayoutType]          NVARCHAR (50)   DEFAULT 'grid-3' NOT NULL,
        [IsPublished]         BIT             DEFAULT 0 NOT NULL,
        [customer_id]         BIGINT          NULL,
        [IsActive]            BIT             DEFAULT 1 NOT NULL,
        [IsDeleted]           BIT             DEFAULT 0 NOT NULL,
        [CreatedDate]         DATETIME        DEFAULT GETDATE() NOT NULL,
        [ModifiedDate]        DATETIME        NULL
    );
END;
GO

-- Project Layout Column Migration
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_project_mst]') AND type IN (N'U'))
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[drs_project_mst]') 
        AND name = 'LayoutType'
    )
    BEGIN
        ALTER TABLE [dbo].[drs_project_mst] 
        ADD [LayoutType] NVARCHAR (50) NOT NULL CONSTRAINT DF_drs_project_mst_LayoutType DEFAULT 'grid-3';
    END;
END;
GO

-- Project Image Master Table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[drs_project_image_mst]') AND type IN (N'U'))
BEGIN
    CREATE TABLE [dbo].[drs_project_image_mst] (
        [ID]               BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        [project_id]       BIGINT         NOT NULL,
        [OriginalFileName] NVARCHAR (255) NOT NULL,
        [StoredFileName]   NVARCHAR (255) NOT NULL,
        [ImagePath]        NVARCHAR (500) NOT NULL,
        [ContentType]      VARCHAR (100)  NOT NULL,
        [FileSize]         BIGINT         NOT NULL,
        [DisplayOrder]     INT            DEFAULT 1 NOT NULL,
        [IsActive]         BIT            DEFAULT 1 NOT NULL,
        [IsDeleted]        BIT            DEFAULT 0 NOT NULL,
        [CreatedDate]      DATETIME       DEFAULT GETDATE() NOT NULL,
        [ModifiedDate]     DATETIME       NULL
    );
END;
GO
