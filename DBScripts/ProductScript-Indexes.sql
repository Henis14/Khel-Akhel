USE KhelAkhe_CRM;

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- Customer Account No Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_Account_no
    ON drs_customer_mst(Account_no);


GO
-- Customer Email Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_Email
    ON drs_customer_mst(Email) WHERE IsDeleted = 0;


GO
-- Customer Mobile Number Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_customer_mst_MobileNo
    ON drs_customer_mst(MobileNo) WHERE IsDeleted = 0;


GO
-- Customer Address Customer FK Index
CREATE NONCLUSTERED INDEX IX_drs_customer_address_mst_customer_id
    ON drs_customer_address_mst(customer_id);


GO
-- Product Category Name Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_category_mst_CategoryName
    ON drs_product_category_mst(CategoryName) WHERE IsDeleted = 0;


GO
-- Product Code Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_mst_ProductCode
    ON drs_product_mst(ProductCode);


GO
-- Product SKU Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_mst_SKU
    ON drs_product_mst(SKU) WHERE IsDeleted = 0;


GO
-- Product Category FK Index
CREATE NONCLUSTERED INDEX IX_drs_product_mst_product_category_id
    ON drs_product_mst(product_category_id);


GO
-- Product Stock Product FK Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_product_stock_mst_product_id
    ON drs_product_stock_mst(product_id) WHERE IsDeleted = 0;


GO
-- Product Image Product FK Index
CREATE NONCLUSTERED INDEX IX_drs_product_image_mst_product_id
    ON drs_product_image_mst(product_id);


GO
-- Refresh Token Customer FK Index
CREATE NONCLUSTERED INDEX IX_drs_refresh_token_mst_customer_id
    ON drs_refresh_token_mst(customer_id);


GO
-- Refresh Token Token Hash Index
CREATE NONCLUSTERED INDEX IX_drs_refresh_token_mst_TokenHash
    ON drs_refresh_token_mst(TokenHash);


GO
-- Login History Customer FK Index
CREATE NONCLUSTERED INDEX IX_drs_login_history_mst_customer_id
    ON drs_login_history_mst(customer_id);


GO
-- Audit Module and Entity Index
CREATE NONCLUSTERED INDEX IX_drs_audit_mst_Module_Entity
    ON drs_audit_mst(ModuleName, EntityId);


GO
-- Error Message Code Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_error_message_mst_MessageCode
    ON drs_error_message_mst(MessageCode) WHERE IsDeleted = 0;


GO
-- Country Name Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_country_mst_CountryName
    ON drs_country_mst(CountryName) WHERE IsDeleted = 0;


GO
-- Country Code Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_country_mst_CountryCode
    ON drs_country_mst(CountryCode) WHERE IsDeleted = 0;


GO
-- State Name Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_state_mst_Country_StateName
    ON drs_state_mst(country_id, StateName) WHERE IsDeleted = 0;


GO
-- Commission Category Name Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_commission_category_mst_CategoryName
    ON drs_commission_category_mst(CategoryName) WHERE IsDeleted = 0;


GO
-- Commission Category Active List Index
CREATE NONCLUSTERED INDEX IX_drs_commission_category_mst_IsActive
    ON drs_commission_category_mst(IsActive, DisplayOrder) WHERE IsDeleted = 0;


GO
-- Commission Request Number Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_commission_request_mst_RequestNo
    ON drs_commission_request_mst(RequestNo);


GO
-- Commission Request Status and Created Date Index
CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_Status_CreatedDate
    ON drs_commission_request_mst(Status, CreatedDate DESC) WHERE IsDeleted = 0;


GO
-- Commission Request Category FK Index
CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_category_id
    ON drs_commission_request_mst(category_id);


GO
-- Commission Request Email Index
CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_Email
    ON drs_commission_request_mst(Email) WHERE IsDeleted = 0;


GO
-- Commission Request Mobile Number Index
CREATE NONCLUSTERED INDEX IX_drs_commission_request_mst_MobileNo
    ON drs_commission_request_mst(MobileNo) WHERE IsDeleted = 0;


GO
-- Commission Request Image Request FK Index
CREATE NONCLUSTERED INDEX IX_drs_commission_request_image_mst_request_id
    ON drs_commission_request_image_mst(commission_request_id, DisplayOrder) WHERE IsDeleted = 0;


GO
-- Project Public Listing Index
CREATE NONCLUSTERED INDEX IX_drs_project_mst_PublicList
    ON drs_project_mst(DisplayOrder, ID)
    INCLUDE(Heading, Subheading, ContentType, ProjectOrEventTitle, DisplayYear, ShortDescription, Credits, Location, ExternalUrl, LayoutType) WHERE IsDeleted = 0
                                                                                                                                                    AND IsActive = 1
                                                                                                                                                        AND IsPublished = 1;


GO
-- Project Status and Display Order Index
CREATE NONCLUSTERED INDEX IX_drs_project_mst_Status_DisplayOrder
    ON drs_project_mst(IsPublished, IsActive, DisplayOrder) WHERE IsDeleted = 0;


GO
-- Project Image Project FK Index
CREATE NONCLUSTERED INDEX IX_drs_project_image_mst_project_id_DisplayOrder
    ON drs_project_image_mst(project_id, DisplayOrder) WHERE IsDeleted = 0;


GO
-- Wishlist Customer Unique Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_mst_customer_id
    ON drs_wishlist_mst(customer_id) WHERE customer_id IS NOT NULL AND IsDeleted = 0;


GO
-- Wishlist Guest Token Hash Unique Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_mst_GuestTokenHash
    ON drs_wishlist_mst(GuestTokenHash) WHERE GuestTokenHash IS NOT NULL AND IsDeleted = 0;


GO
-- Wishlist Item Wishlist FK Index
CREATE NONCLUSTERED INDEX IX_drs_wishlist_item_mst_wishlist_id
    ON drs_wishlist_item_mst(wishlist_id) WHERE IsDeleted = 0;


GO
-- Wishlist Item Product FK Index
CREATE NONCLUSTERED INDEX IX_drs_wishlist_item_mst_product_id
    ON drs_wishlist_item_mst(product_id) WHERE IsDeleted = 0;


GO
-- Wishlist Item Unique Product Index (One active entry per product per wishlist)
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_wishlist_item_mst_wishlist_product
    ON drs_wishlist_item_mst(wishlist_id, product_id) WHERE IsDeleted = 0;


GO
-- Cart Customer Active Unique Index
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_cart_mst_customer_active
    ON drs_cart_mst(customer_id) WHERE CartStatus = 'Active' AND IsDeleted = 0;


GO
-- Cart Customer FK Index
CREATE NONCLUSTERED INDEX IX_drs_cart_mst_customer_id
    ON drs_cart_mst(customer_id) WHERE IsDeleted = 0;


GO
-- Cart Item Cart FK Index
CREATE NONCLUSTERED INDEX IX_drs_cart_item_mst_cart_id
    ON drs_cart_item_mst(cart_id) WHERE IsDeleted = 0;


GO
-- Cart Item Product FK Index
CREATE NONCLUSTERED INDEX IX_drs_cart_item_mst_product_id
    ON drs_cart_item_mst(product_id) WHERE IsDeleted = 0;


GO
-- Cart Item Unique Product Index (One active entry per product per cart)
CREATE UNIQUE NONCLUSTERED INDEX UQ_drs_cart_item_mst_cart_product
    ON drs_cart_item_mst(cart_id, product_id) WHERE IsDeleted = 0;


GO
-- Contact Inquiry CreatedDate Index (Admin chronological sorting)
CREATE NONCLUSTERED INDEX IX_drs_contact_inquiry_mst_CreatedDate
    ON drs_contact_inquiry_mst(CreatedDate DESC) WHERE IsDeleted = 0;


GO
-- Contact Inquiry Email Index (Search and duplicate detection)
CREATE NONCLUSTERED INDEX IX_drs_contact_inquiry_mst_Email
    ON drs_contact_inquiry_mst(Email) WHERE IsDeleted = 0;

GO
