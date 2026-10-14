USE KhelAkhe_CRM;

ALTER TABLE [dbo].[drs_customer_address_mst]
ADD CONSTRAINT [FK_drs_customer_address_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_product_mst]
ADD CONSTRAINT [FK_drs_product_mst_product_category_id]
FOREIGN KEY ([product_category_id]) REFERENCES [dbo].[drs_product_category_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_product_stock_mst]
ADD CONSTRAINT [FK_drs_product_stock_mst_product_id]
FOREIGN KEY ([product_id]) REFERENCES [dbo].[drs_product_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_product_image_mst]
ADD CONSTRAINT [FK_drs_product_image_mst_product_id]
FOREIGN KEY ([product_id]) REFERENCES [dbo].[drs_product_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_refresh_token_mst]
ADD CONSTRAINT [FK_drs_refresh_token_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_login_history_mst]
ADD CONSTRAINT [FK_drs_login_history_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

ALTER TABLE [dbo].[drs_audit_mst]
ADD CONSTRAINT [FK_drs_audit_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_state_mst_country_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_state_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_state_mst] WITH CHECK ADD CONSTRAINT [FK_drs_state_mst_country_id]
    FOREIGN KEY ([country_id]) REFERENCES [dbo].[drs_country_mst] ([ID]);
    ALTER TABLE [dbo].[drs_state_mst] CHECK CONSTRAINT [FK_drs_state_mst_country_id];
END;
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_customer_address_mst_country_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_customer_address_mst] WITH CHECK ADD CONSTRAINT [FK_drs_customer_address_mst_country_id]
    FOREIGN KEY ([country_id]) REFERENCES [dbo].[drs_country_mst] ([ID]);
    ALTER TABLE [dbo].[drs_customer_address_mst] CHECK CONSTRAINT [FK_drs_customer_address_mst_country_id];
END;
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_customer_address_mst_state_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_customer_address_mst] WITH CHECK ADD CONSTRAINT [FK_drs_customer_address_mst_state_id]
    FOREIGN KEY ([state_id]) REFERENCES [dbo].[drs_state_mst] ([ID]);
    ALTER TABLE [dbo].[drs_customer_address_mst] CHECK CONSTRAINT [FK_drs_customer_address_mst_state_id];
END;
GO

-- =========================================================================
-- Commissioned Projects Foreign Keys
-- =========================================================================

-- Commission Request Category FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_mst_category_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_mst_category_id]
    FOREIGN KEY ([category_id]) REFERENCES [dbo].[drs_commission_category_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_mst] CHECK CONSTRAINT [FK_drs_commission_request_mst_category_id];
END;
GO

-- Commission Request Optional Customer FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_mst_customer_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_mst_customer_id]
    FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_mst] CHECK CONSTRAINT [FK_drs_commission_request_mst_customer_id];
END;
GO

-- Commission Request Image Request FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_image_mst_request_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_image_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_image_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_image_mst_request_id]
    FOREIGN KEY ([commission_request_id]) REFERENCES [dbo].[drs_commission_request_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_image_mst] CHECK CONSTRAINT [FK_drs_commission_request_image_mst_request_id];
END;
GO

-- =========================================================================
-- About Page Foreign Keys
-- =========================================================================

-- About Optional Customer FK (Author/Admin)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_about_mst_customer_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_about_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_about_mst] WITH CHECK ADD CONSTRAINT [FK_drs_about_mst_customer_id]
    FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
    ALTER TABLE [dbo].[drs_about_mst] CHECK CONSTRAINT [FK_drs_about_mst_customer_id];
END;
GO

-- About Image to About Master FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_about_image_mst_about_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_about_image_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_about_image_mst] WITH CHECK ADD CONSTRAINT [FK_drs_about_image_mst_about_id]
    FOREIGN KEY ([about_id]) REFERENCES [dbo].[drs_about_mst] ([ID]);
    ALTER TABLE [dbo].[drs_about_image_mst] CHECK CONSTRAINT [FK_drs_about_image_mst_about_id];
END;
GO