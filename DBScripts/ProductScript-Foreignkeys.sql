USE KhelAkhe_CRM;

-- Customer Address Customer FK
ALTER TABLE drs_customer_address_mst
    ADD CONSTRAINT FK_drs_customer_address_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);


GO
-- Product Category FK
ALTER TABLE drs_product_mst
    ADD CONSTRAINT FK_drs_product_mst_product_category_id FOREIGN KEY (product_category_id) REFERENCES drs_product_category_mst (ID);


GO
-- Product Stock FK
ALTER TABLE drs_product_stock_mst
    ADD CONSTRAINT FK_drs_product_stock_mst_product_id FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);


GO
-- Product Image FK
ALTER TABLE drs_product_image_mst
    ADD CONSTRAINT FK_drs_product_image_mst_product_id FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);


GO
-- Refresh Token Customer FK
ALTER TABLE drs_refresh_token_mst
    ADD CONSTRAINT FK_drs_refresh_token_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);


GO
-- Login History Customer FK
ALTER TABLE drs_login_history_mst
    ADD CONSTRAINT FK_drs_login_history_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);


GO
-- Audit Customer FK
ALTER TABLE drs_audit_mst
    ADD CONSTRAINT FK_drs_audit_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);


GO
-- State Country FK
ALTER TABLE drs_state_mst WITH CHECK
    ADD CONSTRAINT FK_drs_state_mst_country_id FOREIGN KEY (country_id) REFERENCES drs_country_mst (ID);

ALTER TABLE drs_state_mst CHECK CONSTRAINT FK_drs_state_mst_country_id;


GO
-- Customer Address Country FK
ALTER TABLE drs_customer_address_mst WITH CHECK
    ADD CONSTRAINT FK_drs_customer_address_mst_country_id FOREIGN KEY (country_id) REFERENCES drs_country_mst (ID);

ALTER TABLE drs_customer_address_mst CHECK CONSTRAINT FK_drs_customer_address_mst_country_id;


GO
-- Customer Address State FK
ALTER TABLE drs_customer_address_mst WITH CHECK
    ADD CONSTRAINT FK_drs_customer_address_mst_state_id FOREIGN KEY (state_id) REFERENCES drs_state_mst (ID);

ALTER TABLE drs_customer_address_mst CHECK CONSTRAINT FK_drs_customer_address_mst_state_id;


GO
-- Commission Request Category FK
ALTER TABLE drs_commission_request_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_mst_category_id FOREIGN KEY (category_id) REFERENCES drs_commission_category_mst (ID);

ALTER TABLE drs_commission_request_mst CHECK CONSTRAINT FK_drs_commission_request_mst_category_id;


GO
-- Commission Request Customer FK
ALTER TABLE drs_commission_request_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_commission_request_mst CHECK CONSTRAINT FK_drs_commission_request_mst_customer_id;


GO
-- Commission Request Image FK
ALTER TABLE drs_commission_request_image_mst WITH CHECK
    ADD CONSTRAINT FK_drs_commission_request_image_mst_request_id FOREIGN KEY (commission_request_id) REFERENCES drs_commission_request_mst (ID);

ALTER TABLE drs_commission_request_image_mst CHECK CONSTRAINT FK_drs_commission_request_image_mst_request_id;


GO
-- Project Customer FK
ALTER TABLE drs_project_mst WITH CHECK
    ADD CONSTRAINT FK_drs_project_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_project_mst CHECK CONSTRAINT FK_drs_project_mst_customer_id;


GO
-- Project Image FK
ALTER TABLE drs_project_image_mst WITH CHECK
    ADD CONSTRAINT FK_drs_project_image_mst_project_id FOREIGN KEY (project_id) REFERENCES drs_project_mst (ID);

ALTER TABLE drs_project_image_mst CHECK CONSTRAINT FK_drs_project_image_mst_project_id;


GO
-- Wishlist Customer FK
ALTER TABLE drs_wishlist_mst WITH CHECK
    ADD CONSTRAINT FK_drs_wishlist_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_wishlist_mst CHECK CONSTRAINT FK_drs_wishlist_mst_customer_id;


GO
-- Wishlist Item Wishlist FK
ALTER TABLE drs_wishlist_item_mst WITH CHECK
    ADD CONSTRAINT FK_drs_wishlist_item_mst_wishlist_id FOREIGN KEY (wishlist_id) REFERENCES drs_wishlist_mst (ID);

ALTER TABLE drs_wishlist_item_mst CHECK CONSTRAINT FK_drs_wishlist_item_mst_wishlist_id;


GO
-- Wishlist Item Product FK
ALTER TABLE drs_wishlist_item_mst WITH CHECK
    ADD CONSTRAINT FK_drs_wishlist_item_mst_product_id FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);

ALTER TABLE drs_wishlist_item_mst CHECK CONSTRAINT FK_drs_wishlist_item_mst_product_id;


GO
-- Cart Customer FK
ALTER TABLE drs_cart_mst WITH CHECK
    ADD CONSTRAINT FK_drs_cart_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_cart_mst CHECK CONSTRAINT FK_drs_cart_mst_customer_id;


GO
-- Cart Item Cart FK
ALTER TABLE drs_cart_item_mst WITH CHECK
    ADD CONSTRAINT FK_drs_cart_item_mst_cart_id FOREIGN KEY (cart_id) REFERENCES drs_cart_mst (ID);

ALTER TABLE drs_cart_item_mst CHECK CONSTRAINT FK_drs_cart_item_mst_cart_id;


GO
-- Cart Item Product FK
ALTER TABLE drs_cart_item_mst WITH CHECK
    ADD CONSTRAINT FK_drs_cart_item_mst_product_id FOREIGN KEY (product_id) REFERENCES drs_product_mst (ID);

ALTER TABLE drs_cart_item_mst CHECK CONSTRAINT FK_drs_cart_item_mst_product_id;


GO
-- Contact Inquiry Customer FK
ALTER TABLE drs_contact_inquiry_mst WITH CHECK
    ADD CONSTRAINT FK_drs_contact_inquiry_mst_customer_id FOREIGN KEY (customer_id) REFERENCES drs_customer_mst (ID);

ALTER TABLE drs_contact_inquiry_mst CHECK CONSTRAINT FK_drs_contact_inquiry_mst_customer_id;


GO
