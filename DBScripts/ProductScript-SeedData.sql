USE KhelAkhe_CRM;


INSERT  INTO [dbo].[drs_error_message_mst] ([MessageCode], [MessageText], [MessageType], [HttpStatusCode], [IsActive], [IsDeleted], [CreatedDate])
VALUES                                    ('LOGIN_SUCCESS', 'Login successful.', 'SUCCESS', 200, 1, 0, GETDATE()),
('LOGOUT_SUCCESS', 'Logout successful.', 'SUCCESS', 200, 1, 0, GETDATE()),
('TOKEN_REFRESHED', 'Token refreshed successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('PASSWORD_CHANGED', 'Password changed successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('PASSWORD_RESET_REQUESTED', 'Password reset request generated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('CUSTOMER_CREATED', 'Customer created successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('CUSTOMER_UPDATED', 'Customer updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('CUSTOMER_DELETED', 'Customer deleted successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('ADDRESS_CREATED', 'Address created successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('ADDRESS_UPDATED', 'Address updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('CATEGORY_CREATED', 'Category created successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('CATEGORY_UPDATED', 'Category updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('PRODUCT_CREATED', 'Product created successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('PRODUCT_UPDATED', 'Product updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('PRODUCT_DELETED', 'Product deleted successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('STOCK_ADDED', 'Stock added successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('STOCK_UPDATED', 'Stock updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('SUCCESS', 'Operation completed successfully.', 'SUCCESS', 200, 1, 0, GETDATE()),
('INVALID_REQUEST', 'Invalid request parameters.', 'ERROR', 400, 1, 0, GETDATE()),
('REQUEST_BODY_REQUIRED', 'Request body is required.', 'ERROR', 400, 1, 0, GETDATE()),
('VALIDATION_FAILED', 'Validation failed for the request.', 'ERROR', 400, 1, 0, GETDATE()),
('INVALID_CREDENTIALS', 'Invalid email/mobile or password.', 'ERROR', 401, 1, 0, GETDATE()),
('UNAUTHORIZED', 'Authentication is required to access this resource.', 'ERROR', 401, 1, 0, GETDATE()),
('INVALID_TOKEN', 'Invalid or expired access token.', 'ERROR', 401, 1, 0, GETDATE()),
('INVALID_REFRESH_TOKEN', 'Invalid or expired refresh token.', 'ERROR', 401, 1, 0, GETDATE()),
('REFRESH_TOKEN_REQUIRED', 'Refresh token is required.', 'ERROR', 401, 1, 0, GETDATE()),
('FORBIDDEN', 'You do not have permission to access this resource.', 'ERROR', 403, 1, 0, GETDATE()),
('ACCOUNT_DISABLED', 'Account is disabled.', 'ERROR', 403, 1, 0, GETDATE()),
('ACCOUNT_SUSPENDED', 'Account is suspended.', 'ERROR', 403, 1, 0, GETDATE()),
('ACCOUNT_TERMINATED', 'Account is terminated.', 'ERROR', 403, 1, 0, GETDATE()),
('ACCOUNT_TEMPORARILY_LOCKED', 'Account is temporarily locked due to multiple failed login attempts.', 'ERROR', 423, 1, 0, GETDATE()),
('CUSTOMER_NOT_FOUND', 'Customer record not found.', 'ERROR', 404, 1, 0, GETDATE()),
('CUSTOMER_ALREADY_EXISTS', 'Customer with given email or mobile already exists.', 'ERROR', 409, 1, 0, GETDATE()),
('ADDRESS_NOT_FOUND', 'Customer address record not found.', 'ERROR', 404, 1, 0, GETDATE()),
('CATEGORY_NOT_FOUND', 'Product category not found.', 'ERROR', 404, 1, 0, GETDATE()),
('CATEGORY_ALREADY_EXISTS', 'Product category with given name already exists.', 'ERROR', 409, 1, 0, GETDATE()),
('PRODUCT_NOT_FOUND', 'Product not found.', 'ERROR', 404, 1, 0, GETDATE()),
('PRODUCT_ALREADY_EXISTS', 'Product with given code or SKU already exists.', 'ERROR', 409, 1, 0, GETDATE()),
('INVALID_PRODUCT_ID', 'Invalid product ID specified.', 'ERROR', 400, 1, 0, GETDATE()),
('STOCK_NOT_FOUND', 'Stock information not found for product.', 'ERROR', 404, 1, 0, GETDATE()),
('RATE_LIMIT_EXCEEDED', 'Too many requests. Please try again later.', 'ERROR', 429, 1, 0, GETDATE()),
('SERVER_ERROR', 'An unexpected error occurred on the server.', 'ERROR', 500, 1, 0, GETDATE());
GO

-- Backfill existing Customer Account numbers if missing
UPDATE [dbo].[drs_customer_mst]
SET [Account_no] = 'DRC' + RIGHT('000000' + CAST(ID AS VARCHAR(6)), 6)
WHERE [Account_no] IS NULL OR [Account_no] = '';
GO

-- Synchronize Sequences with maximum existing numeric values
DECLARE @MaxCustId BIGINT = (
    SELECT ISNULL(MAX(CAST(RIGHT(Account_no, 6) AS BIGINT)), 0)
    FROM [dbo].[drs_customer_mst] WITH (NOLOCK)
    WHERE Account_no LIKE 'DRC%' AND LEN(Account_no) = 9
);
IF @MaxCustId > 0
BEGIN
    DECLARE @SqlCust NVARCHAR(MAX) = N'ALTER SEQUENCE [dbo].[Seq_CustomerAccount] RESTART WITH ' + CAST((@MaxCustId + 1) AS NVARCHAR(20));
    EXEC sp_executesql @SqlCust;
END
GO

DECLARE @MaxProdId BIGINT = (
    SELECT ISNULL(MAX(CAST(RIGHT(ProductCode, 6) AS BIGINT)), 0)
    FROM [dbo].[drs_product_mst] WITH (NOLOCK)
    WHERE ProductCode LIKE 'DRP%' AND LEN(ProductCode) = 9
);
IF @MaxProdId > 0
BEGIN
    DECLARE @SqlProd NVARCHAR(MAX) = N'ALTER SEQUENCE [dbo].[Seq_ProductCode] RESTART WITH ' + CAST((@MaxProdId + 1) AS NVARCHAR(20));
    EXEC sp_executesql @SqlProd;
END
GO

INSERT INTO dbo.drs_country_mst
(
    CountryName,
    CountryCode,
    PhoneCode
)
VALUES
(N'India', 'IN', '+91'),
(N'China', 'CN', '+86'),
(N'Japan', 'JP', '+81'),
(N'South Korea', 'KR', '+82'),
(N'Singapore', 'SG', '+65'),
(N'Malaysia', 'MY', '+60'),
(N'Thailand', 'TH', '+66'),
(N'Indonesia', 'ID', '+62'),
(N'Philippines', 'PH', '+63'),
(N'Vietnam', 'VN', '+84'),
(N'Pakistan', 'PK', '+92'),
(N'Bangladesh', 'BD', '+880'),
(N'Sri Lanka', 'LK', '+94'),
(N'Nepal', 'NP', '+977'),
(N'UAE', 'AE', '+971'),
(N'Saudi Arabia', 'SA', '+966'),
(N'Qatar', 'QA', '+974'),
(N'Kuwait', 'KW', '+965'),
(N'Oman', 'OM', '+968'),
(N'Israel', 'IL', '+972'),
(N'Turkey', 'TR', '+90'),
(N'United Kingdom', 'GB', '+44'),
(N'Germany', 'DE', '+49'),
(N'France', 'FR', '+33'),
(N'Italy', 'IT', '+39'),
(N'Spain', 'ES', '+34'),
(N'Netherlands', 'NL', '+31'),
(N'Switzerland', 'CH', '+41'),
(N'Sweden', 'SE', '+46'),
(N'Norway', 'NO', '+47'),
(N'Denmark', 'DK', '+45'),
(N'Ireland', 'IE', '+353'),
(N'Poland', 'PL', '+48'),
(N'Portugal', 'PT', '+351'),
(N'Greece', 'GR', '+30'),
(N'United States', 'US', '+1'),
(N'Canada', 'CA', '+1'),
(N'Mexico', 'MX', '+52'),
(N'Brazil', 'BR', '+55'),
(N'Argentina', 'AR', '+54'),
(N'Chile', 'CL', '+56'),
(N'Colombia', 'CO', '+57'),
(N'South Africa', 'ZA', '+27'),
(N'Nigeria', 'NG', '+234'),
(N'Kenya', 'KE', '+254'),
(N'Egypt', 'EG', '+20'),
(N'Australia', 'AU', '+61'),
(N'New Zealand', 'NZ', '+64');


INSERT INTO dbo.drs_state_mst
(
    country_id,
    StateName,
    StateCode
)
SELECT
    c.ID,
    s.StateName,
    s.StateCode
FROM dbo.drs_country_mst c
CROSS APPLY
(
    VALUES
    (N'Andhra Pradesh', 'AP'),
    (N'Arunachal Pradesh', 'AR'),
    (N'Assam', 'AS'),
    (N'Bihar', 'BR'),
    (N'Chhattisgarh', 'CG'),
    (N'Goa', 'GA'),
    (N'Gujarat', 'GJ'),
    (N'Haryana', 'HR'),
    (N'Himachal Pradesh', 'HP'),
    (N'Jharkhand', 'JH'),
    (N'Karnataka', 'KA'),
    (N'Kerala', 'KL'),
    (N'Madhya Pradesh', 'MP'),
    (N'Maharashtra', 'MH'),
    (N'Manipur', 'MN'),
    (N'Meghalaya', 'ML'),
    (N'Mizoram', 'MZ'),
    (N'Nagaland', 'NL'),
    (N'Odisha', 'OD'),
    (N'Punjab', 'PB'),
    (N'Rajasthan', 'RJ'),
    (N'Sikkim', 'SK'),
    (N'Tamil Nadu', 'TN'),
    (N'Telangana', 'TS'),
    (N'Tripura', 'TR'),
    (N'Uttar Pradesh', 'UP'),
    (N'Uttarakhand', 'UK'),
    (N'West Bengal', 'WB'),

    (N'Andaman and Nicobar Islands', 'AN'),
    (N'Chandigarh', 'CH'),
    (N'Dadra and Nagar Haveli and Daman and Diu', 'DN'),
    (N'Delhi', 'DL'),
    (N'Jammu and Kashmir', 'JK'),
    (N'Ladakh', 'LA'),
    (N'Lakshadweep', 'LD'),
    (N'Puducherry', 'PY')
) AS s(StateName, StateCode)
WHERE c.CountryCode = 'IN';