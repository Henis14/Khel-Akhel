USE CRM_Kehl_Akhel;

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_Account_no
    ON drs_customer_mst (Account_no);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_Email
    ON drs_customer_mst (Email)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_MobileNo
    ON drs_customer_mst (MobileNo)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_customer_address_mst_customer_id
    ON drs_customer_address_mst (customer_id);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_category_mst_CategoryName
    ON drs_product_category_mst (CategoryName)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_mst_ProductCode
    ON drs_product_mst (ProductCode);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_mst_SKU
    ON drs_product_mst (SKU)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_product_mst_product_category_id
    ON drs_product_mst (product_category_id);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_stock_mst_product_id
    ON drs_product_stock_mst (product_id)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_product_image_mst_product_id
    ON drs_product_image_mst (product_id);
GO

CREATE NONCLUSTERED INDEX IX_drs_refresh_token_mst_customer_id
    ON drs_refresh_token_mst (customer_id);
GO

CREATE NONCLUSTERED INDEX IX_drs_refresh_token_mst_TokenHash
    ON drs_refresh_token_mst (TokenHash);
GO

CREATE NONCLUSTERED INDEX IX_drs_login_history_mst_customer_id
    ON drs_login_history_mst (customer_id);
GO

CREATE NONCLUSTERED INDEX IX_drs_audit_mst_Module_Entity
    ON drs_audit_mst (ModuleName, EntityId);
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_error_message_mst_MessageCode
    ON drs_error_message_mst (MessageCode)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_country_mst_CountryName
    ON drs_country_mst (CountryName)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_country_mst_CountryCode
    ON drs_country_mst (CountryCode)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_state_mst_Country_StateName
    ON drs_state_mst (country_id, StateName)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_commission_category_mst_CategoryName
    ON drs_commission_category_mst (CategoryName)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_category_mst_IsActive
    ON drs_commission_category_mst (IsActive, DisplayOrder)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_commission_request_mst_RequestNo
    ON drs_commission_request_mst (RequestNo);
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_Status_CreatedDate
    ON drs_commission_request_mst (Status, CreatedDate DESC)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_category_id
    ON drs_commission_request_mst (category_id);
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_Email
    ON drs_commission_request_mst (Email)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_MobileNo
    ON drs_commission_request_mst (MobileNo)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_commission_request_image_mst_request_id
    ON drs_commission_request_image_mst (commission_request_id, DisplayOrder)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_project_mst_PublicList
    ON drs_project_mst (DisplayOrder, ID)
    INCLUDE (Heading, Subheading, ContentType, ProjectOrEventTitle, DisplayYear, ShortDescription, Credits, Location, ExternalUrl, LayoutType)
    WHERE IsDeleted = 0
      AND IsActive = 1
      AND IsPublished = 1;
GO

CREATE NONCLUSTERED INDEX IX_drs_project_mst_Status_DisplayOrder
    ON drs_project_mst (IsPublished, IsActive, DisplayOrder)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_project_image_mst_project_id_DisplayOrder
    ON drs_project_image_mst (project_id, DisplayOrder)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_mst_customer_id
    ON drs_wishlist_mst (customer_id)
    WHERE customer_id IS NOT NULL
      AND IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_mst_GuestTokenHash
    ON drs_wishlist_mst (GuestTokenHash)
    WHERE GuestTokenHash IS NOT NULL
      AND IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_wishlist_item_mst_wishlist_id
    ON drs_wishlist_item_mst (wishlist_id)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_wishlist_item_mst_product_id
    ON drs_wishlist_item_mst (product_id)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_item_mst_wishlist_product
    ON drs_wishlist_item_mst (wishlist_id, product_id)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_cart_mst_customer_active
    ON drs_cart_mst (customer_id)
    WHERE CartStatus = 'Active'
      AND IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_cart_mst_customer_id
    ON drs_cart_mst (customer_id)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_cart_item_mst_cart_id
    ON drs_cart_item_mst (cart_id)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_cart_item_mst_product_id
    ON drs_cart_item_mst (product_id)
    WHERE IsDeleted = 0;
GO

CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_cart_item_mst_cart_product
    ON drs_cart_item_mst (cart_id, product_id)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_contact_inquiry_mst_CreatedDate
    ON drs_contact_inquiry_mst (CreatedDate DESC)
    WHERE IsDeleted = 0;
GO

CREATE NONCLUSTERED INDEX IX_drs_contact_inquiry_mst_Email
    ON drs_contact_inquiry_mst (Email)
    WHERE IsDeleted = 0;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_HeroBanners_Active_DisplayOrder'
      AND object_id = OBJECT_ID('HeroBanners')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HeroBanners_Active_DisplayOrder
        ON HeroBanners (IsActive, DisplayOrder, CreatedAt)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_HeroBanners_CreatedAt'
      AND object_id = OBJECT_ID('HeroBanners')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_HeroBanners_CreatedAt
        ON HeroBanners (CreatedAt DESC)
        WHERE IsDeleted = 0;
END
GO


IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_drs_login_history_mst_customer_ip_date'
      AND object_id = OBJECT_ID('drs_login_history_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_login_history_mst_customer_ip_date
        ON drs_login_history_mst (customer_id, IPAddress, CreatedDate)
        INCLUDE (IsSuccess, FailureReason);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'UQ_drs_password_reset_request_mst_TokenHash'
      AND object_id = OBJECT_ID('drs_password_reset_request_mst')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_password_reset_request_mst_TokenHash
        ON drs_password_reset_request_mst (TokenHash)
        WHERE TokenHash IS NOT NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_drs_password_reset_request_mst_Status_CreatedDate'
      AND object_id = OBJECT_ID('drs_password_reset_request_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_password_reset_request_mst_Status_CreatedDate
        ON drs_password_reset_request_mst (Status, CreatedDate);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_drs_password_reset_request_mst_customer_CreatedDate'
      AND object_id = OBJECT_ID('drs_password_reset_request_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_password_reset_request_mst_customer_CreatedDate
        ON drs_password_reset_request_mst (customer_id, CreatedDate);
END;
GO

-- SubCategory Indexes
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_drs_product_subcategory_mst_Parent_Name'
      AND object_id = OBJECT_ID('drs_product_subcategory_mst')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_subcategory_mst_Parent_Name
        ON drs_product_subcategory_mst (product_category_id, SubCategoryName)
        WHERE IsDeleted = 0;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_drs_product_subcategory_mst_product_category_id'
      AND object_id = OBJECT_ID('drs_product_subcategory_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_product_subcategory_mst_product_category_id
        ON drs_product_subcategory_mst (product_category_id, DisplayOrder, SubCategoryName)
        WHERE IsDeleted = 0 AND IsActive = 1;
END
GO

-- Ensure product_subcategory_id column exists before creating index
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('drs_product_mst')
      AND name = 'product_subcategory_id'
)
BEGIN
    ALTER TABLE drs_product_mst ADD product_subcategory_id BIGINT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_drs_product_mst_product_subcategory_id'
      AND object_id = OBJECT_ID('drs_product_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_product_mst_product_subcategory_id
        ON drs_product_mst (product_subcategory_id)
        WHERE product_subcategory_id IS NOT NULL AND IsDeleted = 0;
END
GO

GO

-- Website statistics
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_drs_website_page_mst_PageKey'
      AND object_id = OBJECT_ID('drs_website_page_mst')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_website_page_mst_PageKey
        ON drs_website_page_mst (PageKey)
        WHERE IsDeleted = 0;
END
GO

-- One counter row per page per day; also serves the "track" upsert lookup
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'UQ_drs_page_visit_daily_mst_page_VisitDate'
      AND object_id = OBJECT_ID('drs_page_visit_daily_mst')
)
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_page_visit_daily_mst_page_VisitDate
        ON drs_page_visit_daily_mst (page_id, VisitDate)
        INCLUDE (VisitCount, UniqueVisitCount)
        WHERE IsDeleted = 0;
END
GO

-- Date-range reports (dashboard)
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_drs_page_visit_daily_mst_VisitDate'
      AND object_id = OBJECT_ID('drs_page_visit_daily_mst')
)
BEGIN
    CREATE NONCLUSTERED INDEX IX_drs_page_visit_daily_mst_VisitDate
        ON drs_page_visit_daily_mst (VisitDate)
        INCLUDE (page_id, VisitCount, UniqueVisitCount)
        WHERE IsDeleted = 0;
END
GO
