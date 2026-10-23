USE CRM_Kehl_Akhel;

CREATE TABLE drs_customer_mst (
    ID                  BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    Account_no          VARCHAR (9)    NOT NULL,
    FirstName           NVARCHAR (100) NOT NULL,
    LastName            NVARCHAR (100) NOT NULL,
    Email               NVARCHAR (255) NOT NULL,
    MobileNo            VARCHAR (20)   NOT NULL,
    PasswordHash        NVARCHAR (500) NOT NULL,
    IsAdmin             BIT            DEFAULT 0 NOT NULL,
    IsEmailVerified     BIT            DEFAULT 0 NOT NULL,
    IsMobileVerified    BIT            DEFAULT 0 NOT NULL,
    LastLogin           DATETIME       NULL,
    FailedLoginAttempts INT            DEFAULT 0 NOT NULL,
    LastFailedLoginDate DATETIME       NULL,
    AccountLockedUntil  DATETIME       NULL,
    IsActive            BIT            DEFAULT 1 NOT NULL,
    IsDeleted           BIT            DEFAULT 0 NOT NULL,
    CreatedDate         DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate        DATETIME       NULL
);

GO
CREATE SEQUENCE Seq_CustomerAccount
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;

GO
CREATE SEQUENCE Seq_ProductCode
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;

GO
CREATE TABLE drs_customer_address_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id  BIGINT         NOT NULL,
    AddressTitle NVARCHAR (50)  NULL,
    AddressType  NVARCHAR (50)  NULL,
    FullName     NVARCHAR (200) NOT NULL,
    MobileNo     VARCHAR (20)   NOT NULL,
    AddressLine1 NVARCHAR (255) NOT NULL,
    AddressLine2 NVARCHAR (255) NULL,
    Landmark     NVARCHAR (150) NULL,
    City         NVARCHAR (100) NOT NULL,
    State        NVARCHAR (100) NOT NULL,
    Country      NVARCHAR (100) NOT NULL,
    Pincode      VARCHAR (20)   NOT NULL,
    country_id   BIGINT         NULL,
    state_id     BIGINT         NULL,
    IsDefault    BIT            DEFAULT 0 NOT NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
CREATE TABLE drs_product_category_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR (100) NOT NULL,
    Description  NVARCHAR (500) NULL,
    DisplayOrder INT            DEFAULT 0 NOT NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
CREATE TABLE drs_product_mst (
    ID                  BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    product_category_id BIGINT          NOT NULL,
    ProductName         NVARCHAR (200)  NOT NULL,
    ProductCode         VARCHAR (50)    NOT NULL,
    SKU                 VARCHAR (50)    NOT NULL,
    ShortDescription    NVARCHAR (500)  NULL,
    Description         NVARCHAR (MAX)  NULL,
    MRP                 DECIMAL (18, 2) NOT NULL,
    SellingPrice        DECIMAL (18, 2) NOT NULL,
    IsFeatured          BIT             DEFAULT 0 NOT NULL,
    IsNewArrival        BIT             DEFAULT 0 NOT NULL,
    IsBestSeller        BIT             DEFAULT 0 NOT NULL,
    IsTrending          BIT             DEFAULT 0 NOT NULL,
    IsCustomerFavourite BIT             DEFAULT 0 NOT NULL,
    IsActive            BIT             DEFAULT 1 NOT NULL,
    IsDeleted           BIT             DEFAULT 0 NOT NULL,
    CreatedDate         DATETIME        DEFAULT GETDATE() NOT NULL,
    ModifiedDate        DATETIME        NULL,
    CONSTRAINT CK_drs_product_mst_MRP CHECK (MRP >= 0),
    CONSTRAINT CK_drs_product_mst_SellingPrice CHECK (SellingPrice >= 0)
);

GO
CREATE TABLE drs_product_stock_mst (
    ID           BIGINT   IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    product_id   BIGINT   NOT NULL,
    AvailableQty INT      DEFAULT 0 NOT NULL,
    ReservedQty  INT      DEFAULT 0 NOT NULL,
    IsActive     BIT      DEFAULT 1 NOT NULL,
    IsDeleted    BIT      DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME NULL,
    CONSTRAINT CK_drs_product_stock_mst_AvailableQty CHECK (AvailableQty >= 0),
    CONSTRAINT CK_drs_product_stock_mst_ReservedQty CHECK (ReservedQty >= 0)
);

GO
CREATE TABLE drs_product_image_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    product_id   BIGINT         NOT NULL,
    ImagePath    NVARCHAR (500) NOT NULL,
    DisplayOrder INT            DEFAULT 0 NOT NULL,
    IsDefault    BIT            DEFAULT 0 NOT NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
CREATE TABLE drs_refresh_token_mst (
    ID                  BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id         BIGINT         NOT NULL,
    TokenHash           NVARCHAR (500) NOT NULL,
    CreatedUtc          DATETIME       DEFAULT GETUTCDATE() NOT NULL,
    ExpiresUtc          DATETIME       NOT NULL,
    RevokedUtc          DATETIME       NULL,
    ReplacedByTokenHash NVARCHAR (500) NULL,
    CreatedByIp         VARCHAR (50)   NULL,
    RevokedByIp         VARCHAR (50)   NULL,
    DeviceName          NVARCHAR (200) NULL,
    Browser             NVARCHAR (100) NULL,
    OperatingSystem     NVARCHAR (100) NULL,
    IsActive            BIT            DEFAULT 1 NOT NULL,
    IsDeleted           BIT            DEFAULT 0 NOT NULL,
    CreatedDate         DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate        DATETIME       NULL
);

GO
CREATE TABLE drs_login_history_mst (
    ID            BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id   BIGINT         NULL,
    LoginType     VARCHAR (50)   NOT NULL,
    DeviceInfo    NVARCHAR (255) NULL,
    IPAddress     VARCHAR (50)   NULL,
    IsSuccess     BIT            NOT NULL,
    FailureReason NVARCHAR (255) NULL,
    CreatedDate   DATETIME       DEFAULT GETDATE() NOT NULL
);

GO
CREATE TABLE drs_audit_mst (
    ID          BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id BIGINT         NULL,
    ModuleName  NVARCHAR (100) NOT NULL,
    Action      NVARCHAR (100) NOT NULL,
    TableName   NVARCHAR (100) NULL,
    EntityId    BIGINT         NULL,
    OldValue    NVARCHAR (MAX) NULL,
    NewValue    NVARCHAR (MAX) NULL,
    IPAddress   VARCHAR (50)   NULL,
    UserAgent   NVARCHAR (500) NULL,
    CreatedDate DATETIME       DEFAULT GETDATE() NOT NULL
);

GO
CREATE TABLE drs_error_message_mst (
    ID             BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    MessageCode    VARCHAR (100)  NOT NULL,
    MessageText    NVARCHAR (500) NOT NULL,
    MessageType    VARCHAR (20)   DEFAULT 'ERROR' NOT NULL,
    HttpStatusCode INT            DEFAULT 400 NOT NULL,
    IsActive       BIT            DEFAULT 1 NOT NULL,
    IsDeleted      BIT            DEFAULT 0 NOT NULL,
    CreatedDate    DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate   DATETIME       NULL
);

GO
CREATE TABLE drs_country_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    CountryName  NVARCHAR (100) NOT NULL,
    CountryCode  VARCHAR (10)   NOT NULL,
    PhoneCode    VARCHAR (10)   NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
CREATE TABLE drs_state_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    country_id   BIGINT         NOT NULL,
    StateName    NVARCHAR (100) NOT NULL,
    StateCode    VARCHAR (20)   NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
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
CREATE SEQUENCE Seq_CommissionRequestNo
    AS BIGINT
    START WITH 1
    INCREMENT BY 1;

GO
CREATE TABLE drs_commission_category_mst (
    ID           BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    CategoryName NVARCHAR (100) NOT NULL,
    Description  NVARCHAR (500) NULL,
    DisplayOrder INT            DEFAULT 0 NOT NULL,
    IsActive     BIT            DEFAULT 1 NOT NULL,
    IsDeleted    BIT            DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME       NULL
);

GO
CREATE TABLE drs_commission_request_mst (
    ID                       BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    RequestNo                VARCHAR (30)   NOT NULL,
    FullName                 NVARCHAR (150) NOT NULL,
    MobileNo                 VARCHAR (20)   NOT NULL,
    Email                    NVARCHAR (255) NOT NULL,
    category_id              BIGINT         NOT NULL,
    OtherCategoryDescription NVARCHAR (255) NULL,
    ExpectedTimeline         DATE           NOT NULL,
    ProjectLocation          NVARCHAR (255) NOT NULL,
    ProjectDescription       NVARCHAR (MAX) NOT NULL,
    Keywords                 NVARCHAR (500) NULL,
    Status                   NVARCHAR (50)  DEFAULT 'Submitted' NOT NULL,
    AdminNotes               NVARCHAR (MAX) NULL,
    customer_id              BIGINT         NULL,
    IpAddress                VARCHAR (50)   NULL,
    UserAgent                NVARCHAR (500) NULL,
    IsActive                 BIT            DEFAULT 1 NOT NULL,
    IsDeleted                BIT            DEFAULT 0 NOT NULL,
    CreatedDate              DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate             DATETIME       NULL
);

GO
CREATE TABLE drs_commission_request_image_mst (
    ID                    BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    commission_request_id BIGINT         NOT NULL,
    OriginalFileName      NVARCHAR (255) NOT NULL,
    StoredFileName        NVARCHAR (255) NOT NULL,
    StoragePath           NVARCHAR (500) NOT NULL,
    ContentType           VARCHAR (100)  NOT NULL,
    FileSize              BIGINT         NOT NULL,
    DisplayOrder          INT            DEFAULT 1 NOT NULL,
    IsActive              BIT            DEFAULT 1 NOT NULL,
    IsDeleted             BIT            DEFAULT 0 NOT NULL,
    CreatedDate           DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate          DATETIME       NULL
);

GO
CREATE TABLE drs_project_mst (
    ID                  BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    Heading             NVARCHAR (200)  NOT NULL,
    Subheading          NVARCHAR (250)  NULL,
    ContentType         NVARCHAR (50)   NULL,
    ProjectOrEventTitle NVARCHAR (200)  NULL,
    DisplayYear         VARCHAR (50)    NULL,
    ShortDescription    NVARCHAR (1000) NULL,
    DetailedDescription NVARCHAR (MAX)  NULL,
    Credits             NVARCHAR (1000) NULL,
    Location            NVARCHAR (200)  NULL,
    ExternalUrl         NVARCHAR (500)  NULL,
    DisplayOrder        INT             DEFAULT 0 NOT NULL,
    LayoutType          NVARCHAR (50)   DEFAULT 'grid-3' NOT NULL,
    IsPublished         BIT             DEFAULT 0 NOT NULL,
    customer_id         BIGINT          NULL,
    IsActive            BIT             DEFAULT 1 NOT NULL,
    IsDeleted           BIT             DEFAULT 0 NOT NULL,
    CreatedDate         DATETIME        DEFAULT GETDATE() NOT NULL,
    ModifiedDate        DATETIME        NULL
);

GO
CREATE TABLE drs_project_image_mst (
    ID               BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    project_id       BIGINT         NOT NULL,
    OriginalFileName NVARCHAR (255) NOT NULL,
    StoredFileName   NVARCHAR (255) NOT NULL,
    ImagePath        NVARCHAR (500) NOT NULL,
    ContentType      VARCHAR (100)  NOT NULL,
    FileSize         BIGINT         NOT NULL,
    DisplayOrder     INT            DEFAULT 1 NOT NULL,
    IsActive         BIT            DEFAULT 1 NOT NULL,
    IsDeleted        BIT            DEFAULT 0 NOT NULL,
    CreatedDate      DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate     DATETIME       NULL
);

GO
CREATE TABLE drs_wishlist_mst (
    ID              BIGINT         IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id     BIGINT         NULL,
    GuestTokenHash  VARCHAR (128)  NULL,
    IsActive        BIT            DEFAULT 1 NOT NULL,
    IsDeleted       BIT            DEFAULT 0 NOT NULL,
    CreatedDate     DATETIME       DEFAULT GETDATE() NOT NULL,
    ModifiedDate    DATETIME       NULL,
    CONSTRAINT CK_drs_wishlist_mst_OwnerCheck CHECK (
        (customer_id IS NOT NULL AND GuestTokenHash IS NULL) OR
        (customer_id IS NULL AND GuestTokenHash IS NOT NULL)
    )
);

GO
CREATE TABLE drs_wishlist_item_mst (
    ID           BIGINT   IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    wishlist_id  BIGINT   NOT NULL,
    product_id   BIGINT   NOT NULL,
    IsActive     BIT      DEFAULT 1 NOT NULL,
    IsDeleted    BIT      DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME NULL
);

GO
CREATE TABLE drs_cart_mst (
    ID           BIGINT       IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    customer_id  BIGINT       NOT NULL,
    CartStatus   VARCHAR (20) DEFAULT 'Active' NOT NULL,
    IsActive     BIT          DEFAULT 1 NOT NULL,
    IsDeleted    BIT          DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME     DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME     NULL,
    CONSTRAINT CK_drs_cart_mst_Status CHECK (CartStatus IN ('Active', 'ConvertedToOrder', 'Abandoned'))
);

GO
CREATE TABLE drs_cart_item_mst (
    ID           BIGINT   IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    cart_id      BIGINT   NOT NULL,
    product_id   BIGINT   NOT NULL,
    Quantity     INT      DEFAULT 1 NOT NULL,
    IsActive     BIT      DEFAULT 1 NOT NULL,
    IsDeleted    BIT      DEFAULT 0 NOT NULL,
    CreatedDate  DATETIME DEFAULT GETDATE() NOT NULL,
    ModifiedDate DATETIME NULL,
    CONSTRAINT CK_drs_cart_item_mst_Quantity CHECK (Quantity > 0)
);

GO
CREATE TABLE drs_contact_inquiry_mst (
    ID                 BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
    FullName           NVARCHAR (150)  NOT NULL,
    MobileNo           NVARCHAR (20)   NOT NULL,
    Email              NVARCHAR (254)  NOT NULL,
    CompanyName        NVARCHAR (200)  NULL,
    ServiceDescription NVARCHAR (300)  NOT NULL,
    customer_id        BIGINT          NULL,
    IpAddress          VARCHAR (50)    NULL,
    UserAgent          NVARCHAR (500)  NULL,
    IsActive           BIT             DEFAULT 1 NOT NULL,
    IsDeleted          BIT             DEFAULT 0 NOT NULL,
    CreatedDate        DATETIME        DEFAULT GETDATE() NOT NULL,
    ModifiedDate       DATETIME        NULL,
    CONSTRAINT CK_drs_contact_inquiry_mst_ServiceDescLength CHECK (LEN(ServiceDescription) <= 300)
);

GO

CREATE TABLE HeroBanners (
        Id           BIGINT          IDENTITY (1, 1) NOT NULL PRIMARY KEY,
        Title        NVARCHAR (200)  NULL,
        MediaUrl     NVARCHAR (500)  NOT NULL,
        IsImage      BIT             NOT NULL,
        IsVideo      BIT             NOT NULL,
        DisplayOrder INT             DEFAULT 0 NOT NULL,
        IsActive     BIT             DEFAULT 1 NOT NULL,
        IsDeleted    BIT             DEFAULT 0 NOT NULL,
        CreatedAt    DATETIME        DEFAULT GETDATE() NOT NULL,
        UpdatedAt    DATETIME        NULL,
        CONSTRAINT CK_HeroBanners_MediaType CHECK (
            (IsImage = 1 AND IsVideo = 0) OR
            (IsImage = 0 AND IsVideo = 1)
        )
    );



CREATE TABLE drs_password_reset_request_mst
    (
        ID BIGINT IDENTITY(1,1) NOT NULL
            CONSTRAINT PK_drs_password_reset_request_mst PRIMARY KEY,
        customer_id BIGINT NOT NULL,
        Source VARCHAR(20) NOT NULL,
        Status VARCHAR(20) NOT NULL
            CONSTRAINT DF_drs_password_reset_request_mst_Status
            DEFAULT ('PENDING'),
        RequestedIp NVARCHAR(64) NULL,
        UserAgent NVARCHAR(500) NULL,
        TokenHash NVARCHAR(128) NULL,
        TokenExpiresUtc DATETIME NULL,
        IssuedByAdminId BIGINT NULL,
        IssuedUtc DATETIME NULL,
        UsedUtc DATETIME NULL,
        AdminNotes NVARCHAR(1000) NULL,
        IsActive BIT NOT NULL
            CONSTRAINT DF_drs_password_reset_request_mst_IsActive
            DEFAULT (1),
        IsDeleted BIT NOT NULL
            CONSTRAINT DF_drs_password_reset_request_mst_IsDeleted
            DEFAULT (0),
        CreatedDate DATETIME NOT NULL
            CONSTRAINT DF_drs_password_reset_request_mst_CreatedDate
            DEFAULT (GETDATE()),
        ModifiedDate DATETIME NULL
    );

