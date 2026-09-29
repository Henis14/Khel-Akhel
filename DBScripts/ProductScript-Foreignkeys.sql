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