USE KhelAkhe_CRM;

-- Customer Address Customer FK
ALTER TABLE [dbo].[drs_customer_address_mst]
ADD CONSTRAINT [FK_drs_customer_address_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

-- Product Category FK
ALTER TABLE [dbo].[drs_product_mst]
ADD CONSTRAINT [FK_drs_product_mst_product_category_id]
FOREIGN KEY ([product_category_id]) REFERENCES [dbo].[drs_product_category_mst] ([ID]);
GO

-- Product Stock FK
ALTER TABLE [dbo].[drs_product_stock_mst]
ADD CONSTRAINT [FK_drs_product_stock_mst_product_id]
FOREIGN KEY ([product_id]) REFERENCES [dbo].[drs_product_mst] ([ID]);
GO

-- Product Image FK
ALTER TABLE [dbo].[drs_product_image_mst]
ADD CONSTRAINT [FK_drs_product_image_mst_product_id]
FOREIGN KEY ([product_id]) REFERENCES [dbo].[drs_product_mst] ([ID]);
GO

-- Refresh Token Customer FK
ALTER TABLE [dbo].[drs_refresh_token_mst]
ADD CONSTRAINT [FK_drs_refresh_token_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

-- Login History Customer FK
ALTER TABLE [dbo].[drs_login_history_mst]
ADD CONSTRAINT [FK_drs_login_history_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

-- Audit Customer FK
ALTER TABLE [dbo].[drs_audit_mst]
ADD CONSTRAINT [FK_drs_audit_mst_customer_id]
FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
GO

-- State Country FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_state_mst_country_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_state_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_state_mst] WITH CHECK ADD CONSTRAINT [FK_drs_state_mst_country_id]
    FOREIGN KEY ([country_id]) REFERENCES [dbo].[drs_country_mst] ([ID]);
    ALTER TABLE [dbo].[drs_state_mst] CHECK CONSTRAINT [FK_drs_state_mst_country_id];
END;
GO

-- Customer Address Country FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_customer_address_mst_country_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_customer_address_mst] WITH CHECK ADD CONSTRAINT [FK_drs_customer_address_mst_country_id]
    FOREIGN KEY ([country_id]) REFERENCES [dbo].[drs_country_mst] ([ID]);
    ALTER TABLE [dbo].[drs_customer_address_mst] CHECK CONSTRAINT [FK_drs_customer_address_mst_country_id];
END;
GO

-- Customer Address State FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_customer_address_mst_state_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_customer_address_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_customer_address_mst] WITH CHECK ADD CONSTRAINT [FK_drs_customer_address_mst_state_id]
    FOREIGN KEY ([state_id]) REFERENCES [dbo].[drs_state_mst] ([ID]);
    ALTER TABLE [dbo].[drs_customer_address_mst] CHECK CONSTRAINT [FK_drs_customer_address_mst_state_id];
END;
GO

-- Commission Request Category FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_mst_category_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_mst_category_id]
    FOREIGN KEY ([category_id]) REFERENCES [dbo].[drs_commission_category_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_mst] CHECK CONSTRAINT [FK_drs_commission_request_mst_category_id];
END;
GO

-- Commission Request Customer FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_mst_customer_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_mst_customer_id]
    FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_mst] CHECK CONSTRAINT [FK_drs_commission_request_mst_customer_id];
END;
GO

-- Commission Request Image FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_commission_request_image_mst_request_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_commission_request_image_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_commission_request_image_mst] WITH CHECK ADD CONSTRAINT [FK_drs_commission_request_image_mst_request_id]
    FOREIGN KEY ([commission_request_id]) REFERENCES [dbo].[drs_commission_request_mst] ([ID]);
    ALTER TABLE [dbo].[drs_commission_request_image_mst] CHECK CONSTRAINT [FK_drs_commission_request_image_mst_request_id];
END;
GO

-- Project Customer FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_project_mst_customer_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_project_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_project_mst] WITH CHECK ADD CONSTRAINT [FK_drs_project_mst_customer_id]
    FOREIGN KEY ([customer_id]) REFERENCES [dbo].[drs_customer_mst] ([ID]);
    ALTER TABLE [dbo].[drs_project_mst] CHECK CONSTRAINT [FK_drs_project_mst_customer_id];
END;
GO

-- Project Image FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_drs_project_image_mst_project_id]') AND parent_object_id = OBJECT_ID(N'[dbo].[drs_project_image_mst]'))
BEGIN
    ALTER TABLE [dbo].[drs_project_image_mst] WITH CHECK ADD CONSTRAINT [FK_drs_project_image_mst_project_id]
    FOREIGN KEY ([project_id]) REFERENCES [dbo].[drs_project_mst] ([ID]);
    ALTER TABLE [dbo].[drs_project_image_mst] CHECK CONSTRAINT [FK_drs_project_image_mst_project_id];
END;
GO