USE CRM_Kehl_Akhel;

ALTER TABLE drs_customer_address_mst
    ADD CONSTRAINT FK_drs_customer_address_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);
GO

ALTER TABLE drs_product_mst
    ADD CONSTRAINT FK_drs_product_mst_product_category_id
    FOREIGN KEY (product_category_id) REFERENCES drs_product_category_mst (ID);
GO

ALTER TABLE drs_product_stock_mst
    ADD CONSTRAINT FK_drs_product_stock_mst_product_id
    FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);
GO

ALTER TABLE drs_product_image_mst
    ADD CONSTRAINT FK_drs_product_image_mst_product_id
    FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);
GO

ALTER TABLE drs_refresh_token_mst
    ADD CONSTRAINT FK_drs_refresh_token_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);
GO

ALTER TABLE drs_login_history_mst
    ADD CONSTRAINT FK_drs_login_history_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);
GO

ALTER TABLE drs_audit_mst
    ADD CONSTRAINT FK_drs_audit_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);
GO

ALTER TABLE drs_state_mst WITH CHECK
    ADD CONSTRAINT FK_drs_state_mst_country_id
    FOREIGN KEY (country_id) REFERENCES drs_country_mst (ID);

ALTER TABLE drs_state_mst
    CHECK CONSTRAINT FK_drs_state_mst_country_id;
GO

ALTER TABLE drs_customer_address_mst WITH CHECK
    ADD CONSTRAINT FK_drs_customer_address_mst_country_id
    FOREIGN KEY (country_id) REFERENCES drs_country_mst (ID);

ALTER TABLE drs_customer_address_mst
    CHECK CONSTRAINT FK_drs_customer_address_mst_country_id;
GO

ALTER TABLE drs_customer_address_mst WITH CHECK
    ADD CONSTRAINT FK_drs_customer_address_mst_state_id
    FOREIGN KEY (state_id) REFERENCES drs_state_mst (ID);

ALTER TABLE drs_customer_address_mst
    CHECK CONSTRAINT FK_drs_customer_address_mst_state_id;
GO

ALTER TABLE drs_commission_request_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_mst_category_id
    FOREIGN KEY (category_id) REFERENCES drs_commission_category_mst (ID);

ALTER TABLE drs_commission_request_mst
    CHECK CONSTRAINT FK_drs_commission_request_mst_category_id;
GO

ALTER TABLE drs_commission_request_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_commission_request_mst
    CHECK CONSTRAINT FK_drs_commission_request_mst_customer_id;
GO

ALTER TABLE drs_commission_request_image_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_image_mst_request_id
    FOREIGN KEY (commission_request_id) REFERENCES drs_commission_request_mst (ID);

ALTER TABLE drs_commission_request_image_mst
    CHECK CONSTRAINT FK_drs_commission_request_image_mst_request_id;
GO

ALTER TABLE drs_project_mst WITH CHECK
    ADD CONSTRAINT FK_drs_project_mst_customer_id
    FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_project_mst
    CHECK CONSTRAINT FK_drs_project_mst_customer_id;
GO

ALTER TABLE drs_project_image_mst WITH CHECK
    ADD CONSTRAINT FK_drs_project_image_mst_project_id
    FOREIGN KEY (project_id) REFERENCES drs_project_mst (ID);

ALTER TABLE drs_project_image_mst
    CHECK CONSTRAINT FK_drs_project_image_mst_project_id;
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_wishlist_mst_customer_id')
BEGIN
    ALTER TABLE drs_wishlist_mst WITH CHECK
        ADD CONSTRAINT FK_drs_wishlist_mst_customer_id
        FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

    ALTER TABLE drs_wishlist_mst
        CHECK CONSTRAINT FK_drs_wishlist_mst_customer_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_wishlist_item_mst_wishlist_id')
BEGIN
    ALTER TABLE drs_wishlist_item_mst WITH CHECK
        ADD CONSTRAINT FK_drs_wishlist_item_mst_wishlist_id
        FOREIGN KEY (wishlist_id) REFERENCES drs_wishlist_mst (ID);

    ALTER TABLE drs_wishlist_item_mst
        CHECK CONSTRAINT FK_drs_wishlist_item_mst_wishlist_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_wishlist_item_mst_product_id')
BEGIN
    ALTER TABLE drs_wishlist_item_mst WITH CHECK
        ADD CONSTRAINT FK_drs_wishlist_item_mst_product_id
        FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);

    ALTER TABLE drs_wishlist_item_mst
        CHECK CONSTRAINT FK_drs_wishlist_item_mst_product_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_cart_mst_customer_id')
BEGIN
    ALTER TABLE drs_cart_mst WITH CHECK
        ADD CONSTRAINT FK_drs_cart_mst_customer_id
        FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

    ALTER TABLE drs_cart_mst
        CHECK CONSTRAINT FK_drs_cart_mst_customer_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_cart_item_mst_cart_id')
BEGIN
    ALTER TABLE drs_cart_item_mst WITH CHECK
        ADD CONSTRAINT FK_drs_cart_item_mst_cart_id
        FOREIGN KEY (cart_id) REFERENCES drs_cart_mst (ID);

    ALTER TABLE drs_cart_item_mst
        CHECK CONSTRAINT FK_drs_cart_item_mst_cart_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_cart_item_mst_product_id')
BEGIN
    ALTER TABLE drs_cart_item_mst WITH CHECK
        ADD CONSTRAINT FK_drs_cart_item_mst_product_id
        FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);

    ALTER TABLE drs_cart_item_mst
        CHECK CONSTRAINT FK_drs_cart_item_mst_product_id;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_contact_inquiry_mst_customer_id')
BEGIN
    ALTER TABLE drs_contact_inquiry_mst WITH CHECK
        ADD CONSTRAINT FK_drs_contact_inquiry_mst_customer_id
        FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

    ALTER TABLE drs_contact_inquiry_mst
        CHECK CONSTRAINT FK_drs_contact_inquiry_mst_customer_id;
END
GO


IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_drs_password_reset_request_mst_customer_id'
      AND parent_object_id = OBJECT_ID('drs_password_reset_request_mst')
)
BEGIN
    ALTER TABLE drs_password_reset_request_mst
        ADD CONSTRAINT FK_drs_password_reset_request_mst_customer_id
        FOREIGN KEY (customer_id)
        REFERENCES drs_customer_mst (ID);
END;
GO

IF NOT EXISTS ( SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_drs_password_reset_request_mst_IssuedByAdminId'
      AND parent_object_id = OBJECT_ID('drs_password_reset_request_mst')
)
BEGIN
    ALTER TABLE drs_password_reset_request_mst
        ADD CONSTRAINT FK_drs_password_reset_request_mst_IssuedByAdminId
        FOREIGN KEY (IssuedByAdminId)
        REFERENCES drs_customer_mst (ID);
END;
GO

-- SubCategory Foreign Keys
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_drs_product_subcategory_mst_product_category_id'
      AND parent_object_id = OBJECT_ID('drs_product_subcategory_mst')
)
BEGIN
    ALTER TABLE drs_product_subcategory_mst WITH CHECK
        ADD CONSTRAINT FK_drs_product_subcategory_mst_product_category_id
        FOREIGN KEY (product_category_id) REFERENCES drs_product_category_mst (ID);

    ALTER TABLE drs_product_subcategory_mst
        CHECK CONSTRAINT FK_drs_product_subcategory_mst_product_category_id;
END
GO

-- Ensure product_subcategory_id column exists on drs_product_mst before adding foreign key
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
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_drs_product_mst_product_subcategory_id'
      AND parent_object_id = OBJECT_ID('drs_product_mst')
)
BEGIN
    ALTER TABLE drs_product_mst WITH CHECK
        ADD CONSTRAINT FK_drs_product_mst_product_subcategory_id
        FOREIGN KEY (product_subcategory_id) REFERENCES drs_product_subcategory_mst (ID);

    ALTER TABLE drs_product_mst
        CHECK CONSTRAINT FK_drs_product_mst_product_subcategory_id;
END
GO
