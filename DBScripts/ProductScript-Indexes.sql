USE KhelAkhe_CRM;

-- Customer Email (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_customer_mst_Email]
ON [dbo].[drs_customer_mst]([Email])
WHERE [IsDeleted] = 0;
GO

-- Customer MobileNo (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_customer_mst_MobileNo]
ON [dbo].[drs_customer_mst]([MobileNo])
WHERE [IsDeleted] = 0;
GO

-- Customer Address customer_id Index
CREATE NONCLUSTERED INDEX [IX_drs_customer_address_mst_customer_id]
ON [dbo].[drs_customer_address_mst]([customer_id]);
GO

-- Product Category Name (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_category_mst_CategoryName]
ON [dbo].[drs_product_category_mst]([CategoryName])
WHERE [IsDeleted] = 0;
GO

-- Product Code (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_mst_ProductCode]
ON [dbo].[drs_product_mst]([ProductCode])
WHERE [IsDeleted] = 0;
GO

-- Product SKU (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_mst_SKU]
ON [dbo].[drs_product_mst]([SKU])
WHERE [IsDeleted] = 0;
GO

-- Product Category FK Index
CREATE NONCLUSTERED INDEX [IX_drs_product_mst_product_category_id]
ON [dbo].[drs_product_mst]([product_category_id]);
GO

-- Product Stock product_id (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_stock_mst_product_id]
ON [dbo].[drs_product_stock_mst]([product_id])
WHERE [IsDeleted] = 0;
GO

-- Product Image product_id Index
CREATE NONCLUSTERED INDEX [IX_drs_product_image_mst_product_id]
ON [dbo].[drs_product_image_mst]([product_id]);
GO

-- Refresh Token customer_id Index
CREATE NONCLUSTERED INDEX [IX_drs_refresh_token_mst_customer_id]
ON [dbo].[drs_refresh_token_mst]([customer_id]);
GO

-- Refresh Token TokenHash Index
CREATE NONCLUSTERED INDEX [IX_drs_refresh_token_mst_TokenHash]
ON [dbo].[drs_refresh_token_mst]([TokenHash]);
GO

-- Login History customer_id Index
CREATE NONCLUSTERED INDEX [IX_drs_login_history_mst_customer_id]
ON [dbo].[drs_login_history_mst]([customer_id]);
GO

-- Audit ModuleName & EntityId Index
CREATE NONCLUSTERED INDEX [IX_drs_audit_mst_Module_Entity]
ON [dbo].[drs_audit_mst]([ModuleName], [EntityId]);
GO

-- Error Message Code (Filtered Unique Index)
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_error_message_mst_MessageCode]
ON [dbo].[drs_error_message_mst]([MessageCode])
WHERE [IsDeleted] = 0;
GO
