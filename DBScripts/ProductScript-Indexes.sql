USE KhelAkhe_CRM;

-- Customer Account No Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_customer_mst_Account_no]
ON [dbo].[drs_customer_mst]([Account_no]);
GO

-- Customer Email Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_customer_mst_Email]
ON [dbo].[drs_customer_mst]([Email])
WHERE [IsDeleted] = 0;
GO

-- Customer Mobile Number Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_customer_mst_MobileNo]
ON [dbo].[drs_customer_mst]([MobileNo])
WHERE [IsDeleted] = 0;
GO

-- Customer Address Customer FK Index
CREATE NONCLUSTERED INDEX [IX_drs_customer_address_mst_customer_id]
ON [dbo].[drs_customer_address_mst]([customer_id]);
GO

-- Product Category Name Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_category_mst_CategoryName]
ON [dbo].[drs_product_category_mst]([CategoryName])
WHERE [IsDeleted] = 0;
GO

-- Product Code Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_mst_ProductCode]
ON [dbo].[drs_product_mst]([ProductCode]);
GO

-- Product SKU Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_mst_SKU]
ON [dbo].[drs_product_mst]([SKU])
WHERE [IsDeleted] = 0;
GO

-- Product Category FK Index
CREATE NONCLUSTERED INDEX [IX_drs_product_mst_product_category_id]
ON [dbo].[drs_product_mst]([product_category_id]);
GO

-- Product Stock Product FK Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_product_stock_mst_product_id]
ON [dbo].[drs_product_stock_mst]([product_id])
WHERE [IsDeleted] = 0;
GO

-- Product Image Product FK Index
CREATE NONCLUSTERED INDEX [IX_drs_product_image_mst_product_id]
ON [dbo].[drs_product_image_mst]([product_id]);
GO

-- Refresh Token Customer FK Index
CREATE NONCLUSTERED INDEX [IX_drs_refresh_token_mst_customer_id]
ON [dbo].[drs_refresh_token_mst]([customer_id]);
GO

-- Refresh Token Token Hash Index
CREATE NONCLUSTERED INDEX [IX_drs_refresh_token_mst_TokenHash]
ON [dbo].[drs_refresh_token_mst]([TokenHash]);
GO

-- Login History Customer FK Index
CREATE NONCLUSTERED INDEX [IX_drs_login_history_mst_customer_id]
ON [dbo].[drs_login_history_mst]([customer_id]);
GO

-- Audit Module and Entity Index
CREATE NONCLUSTERED INDEX [IX_drs_audit_mst_Module_Entity]
ON [dbo].[drs_audit_mst]([ModuleName], [EntityId]);
GO

-- Error Message Code Index
CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_error_message_mst_MessageCode]
ON [dbo].[drs_error_message_mst]([MessageCode])
WHERE [IsDeleted] = 0;
GO

-- Country Name Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_drs_country_mst_CountryName' AND object_id = OBJECT_ID('drs_country_mst'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_country_mst_CountryName]
    ON [dbo].[drs_country_mst] ([CountryName])
    WHERE [IsDeleted] = 0;
END;
GO

-- Country Code Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_drs_country_mst_CountryCode' AND object_id = OBJECT_ID('drs_country_mst'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_country_mst_CountryCode]
    ON [dbo].[drs_country_mst] ([CountryCode])
    WHERE [IsDeleted] = 0;
END;
GO

-- State Name Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_drs_state_mst_Country_StateName' AND object_id = OBJECT_ID('drs_state_mst'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_state_mst_Country_StateName]
    ON [dbo].[drs_state_mst] ([country_id], [StateName])
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Category Name Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_drs_commission_category_mst_CategoryName' AND object_id = OBJECT_ID('drs_commission_category_mst'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_commission_category_mst_CategoryName]
    ON [dbo].[drs_commission_category_mst] ([CategoryName])
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Category Active List Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_category_mst_IsActive' AND object_id = OBJECT_ID('drs_commission_category_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_category_mst_IsActive]
    ON [dbo].[drs_commission_category_mst] ([IsActive], [DisplayOrder])
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Request Number Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'UQ_drs_commission_request_mst_RequestNo' AND object_id = OBJECT_ID('drs_commission_request_mst'))
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX [UQ_drs_commission_request_mst_RequestNo]
    ON [dbo].[drs_commission_request_mst] ([RequestNo]);
END;
GO

-- Commission Request Status and Created Date Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_request_mst_Status_CreatedDate' AND object_id = OBJECT_ID('drs_commission_request_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_request_mst_Status_CreatedDate]
    ON [dbo].[drs_commission_request_mst] ([Status], [CreatedDate] DESC)
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Request Category FK Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_request_mst_category_id' AND object_id = OBJECT_ID('drs_commission_request_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_request_mst_category_id]
    ON [dbo].[drs_commission_request_mst] ([category_id]);
END;
GO

-- Commission Request Email Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_request_mst_Email' AND object_id = OBJECT_ID('drs_commission_request_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_request_mst_Email]
    ON [dbo].[drs_commission_request_mst] ([Email])
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Request Mobile Number Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_request_mst_MobileNo' AND object_id = OBJECT_ID('drs_commission_request_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_request_mst_MobileNo]
    ON [dbo].[drs_commission_request_mst] ([MobileNo])
    WHERE [IsDeleted] = 0;
END;
GO

-- Commission Request Image Request FK Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_commission_request_image_mst_request_id' AND object_id = OBJECT_ID('drs_commission_request_image_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_commission_request_image_mst_request_id]
    ON [dbo].[drs_commission_request_image_mst] ([commission_request_id], [DisplayOrder])
    WHERE [IsDeleted] = 0;
END;
GO

-- Project Public Listing Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_project_mst_PublicList' AND object_id = OBJECT_ID('drs_project_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_project_mst_PublicList]
    ON [dbo].[drs_project_mst] ([DisplayOrder], [ID])
    INCLUDE ([Heading], [Subheading], [ContentType], [ProjectOrEventTitle], [DisplayYear], [ShortDescription], [Credits], [Location], [ExternalUrl], [LayoutType])
    WHERE [IsDeleted] = 0 AND [IsActive] = 1 AND [IsPublished] = 1;
END;
GO

-- Project Status and Display Order Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_project_mst_Status_DisplayOrder' AND object_id = OBJECT_ID('drs_project_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_project_mst_Status_DisplayOrder]
    ON [dbo].[drs_project_mst] ([IsPublished], [IsActive], [DisplayOrder])
    WHERE [IsDeleted] = 0;
END;
GO

-- Project Image Project FK Index
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_drs_project_image_mst_project_id_DisplayOrder' AND object_id = OBJECT_ID('drs_project_image_mst'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_drs_project_image_mst_project_id_DisplayOrder]
    ON [dbo].[drs_project_image_mst] ([project_id], [DisplayOrder])
    WHERE [IsDeleted] = 0;
END;
GO


