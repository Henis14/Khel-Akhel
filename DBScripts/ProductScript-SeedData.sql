USE KhelAkhe_CRM;

-- Error Message Seed Data
INSERT  INTO drs_error_message_mst (
    MessageCode,
    MessageText,
    MessageType,
    HttpStatusCode,
    IsActive,
    IsDeleted,
    CreatedDate
)
VALUES                             ('LOGIN_SUCCESS', 'Login successful.', 'SUCCESS', 200, 1, 0, GETDATE()),
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
-- Backfill Customer Account Numbers
UPDATE drs_customer_mst
SET    Account_no = 'DRC' + RIGHT('000000' + CAST (ID AS VARCHAR (6)), 6)
WHERE  Account_no IS NULL
       OR Account_no = '';


GO
-- Synchronize Sequences
DECLARE @MaxCustId AS BIGINT = (SELECT ISNULL (
                                           MAX (
                                               CAST (RIGHT (
                                                   Account_no,
                                                   6
                                               ) AS BIGINT)
                                           ),
                                           0
                                       )
                                FROM   drs_customer_mst WITH (
                                           NOLOCK
                                       )
                                WHERE  Account_no LIKE 'DRC%'
                                       AND LEN(Account_no) = 9);

IF @MaxCustId > 0
    BEGIN
        DECLARE @SqlCust AS NVARCHAR (MAX) = N'ALTER SEQUENCE Seq_CustomerAccount RESTART WITH ' + CAST ((@MaxCustId + 1) AS NVARCHAR (20));
        EXECUTE sp_executesql @SqlCust;
    END


GO
DECLARE @MaxProdId AS BIGINT = (SELECT ISNULL (
                                           MAX (
                                               CAST (RIGHT (
                                                   ProductCode,
                                                   6
                                               ) AS BIGINT)
                                           ),
                                           0
                                       )
                                FROM   drs_product_mst WITH (
                                           NOLOCK
                                       )
                                WHERE  ProductCode LIKE 'DRP%'
                                       AND LEN(ProductCode) = 9);

IF @MaxProdId > 0
    BEGIN
        DECLARE @SqlProd AS NVARCHAR (MAX) = N'ALTER SEQUENCE Seq_ProductCode RESTART WITH ' + CAST ((@MaxProdId + 1) AS NVARCHAR (20));
        EXECUTE sp_executesql @SqlProd;
    END


GO
-- Country Seed Data
INSERT  INTO drs_country_mst (
    CountryName,
    CountryCode,
    PhoneCode
)
VALUES                       (N'India', 'IN', '+91'),
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

-- State Seed Data
INSERT INTO drs_state_mst (
    country_id,
    StateName,
    StateCode
)
SELECT c.ID,
       s.StateName,
       s.StateCode
FROM   drs_country_mst AS c
       CROSS APPLY (VALUES (N'Andhra Pradesh', 'AP'), (N'Arunachal Pradesh', 'AR'), (N'Assam', 'AS'), (N'Bihar', 'BR'), (N'Chhattisgarh', 'CG'), (N'Goa', 'GA'), (N'Gujarat', 'GJ'), (N'Haryana', 'HR'), (N'Himachal Pradesh', 'HP'), (N'Jharkhand', 'JH'), (N'Karnataka', 'KA'), (N'Kerala', 'KL'), (N'Madhya Pradesh', 'MP'), (N'Maharashtra', 'MH'), (N'Manipur', 'MN'), (N'Meghalaya', 'ML'), (N'Mizoram', 'MZ'), (N'Nagaland', 'NL'), (N'Odisha', 'OD'), (N'Punjab', 'PB'), (N'Rajasthan', 'RJ'), (N'Sikkim', 'SK'), (N'Tamil Nadu', 'TN'), (N'Telangana', 'TS'), (N'Tripura', 'TR'), (N'Uttar Pradesh', 'UP'), (N'Uttarakhand', 'UK'), (N'West Bengal', 'WB'), (N'Andaman and Nicobar Islands', 'AN'), (N'Chandigarh', 'CH'), (N'Dadra and Nagar Haveli and Daman and Diu', 'DN'), (N'Delhi', 'DL'), (N'Jammu and Kashmir', 'JK'), (N'Ladakh', 'LA'), (N'Lakshadweep', 'LD'), (N'Puducherry', 'PY')) AS s(StateName, StateCode)
WHERE  c.CountryCode = 'IN';


GO
-- Commission Category Seed Data
DECLARE @CommissionCategories TABLE (
    CategoryName NVARCHAR (100),
    Description  NVARCHAR (500),
    DisplayOrder INT           );

INSERT  INTO @CommissionCategories (
    CategoryName,
    Description,
    DisplayOrder
)
VALUES                             (N'Art Installations', N'Custom architectural, experiential, and site-specific art installations.', 1),
                                   (N'Sculptures', N'Bespoke 3D artistic sculptures in metal, stone, wood, or mixed media.', 2),
                                   (N'Wall Murals', N'Handcrafted artistic wall murals and textured artwork for indoor or outdoor spaces.', 3),
                                   (N'Furniture', N'Bespoke studio furniture pieces crafted to custom design specifications.', 4),
                                   (N'Event Decor', N'Curated artistic decor and aesthetic styling for premium events and exhibitions.', 5),
                                   (N'Lighting', N'Artisanal handcrafted lighting fixtures, sculptural lamps, and custom luminaires.', 6),
                                   (N'Wearables', N'Limited-edition wearable art, bespoke textiles, and customized apparel.', 7),
                                   (N'Others', N'Other customized creative projects and specialized commissioned works.', 8);

INSERT INTO drs_commission_category_mst (
    CategoryName,
    Description,
    DisplayOrder,
    IsActive,
    IsDeleted,
    CreatedDate
)
SELECT c.CategoryName,
       c.Description,
       c.DisplayOrder,
       1,
       0,
       GETDATE()
FROM   @CommissionCategories AS c
WHERE  NOT EXISTS (SELECT 1
                   FROM   drs_commission_category_mst AS existing
                   WHERE  existing.CategoryName = c.CategoryName
                          AND existing.IsDeleted = 0);


GO
-- Project Error Message Seed Data
DECLARE @ProjectMessages TABLE (
    MessageCode    VARCHAR (100) ,
    MessageText    NVARCHAR (500),
    MessageType    VARCHAR (20)  ,
    HttpStatusCode INT           );

INSERT  INTO @ProjectMessages (
    MessageCode,
    MessageText,
    MessageType,
    HttpStatusCode
)
VALUES                        ('PROJECT_CREATED', 'Project entry created successfully.', 'SUCCESS', 200),
                              ('PROJECT_UPDATED', 'Project entry updated successfully.', 'SUCCESS', 200),
                              ('PROJECT_DELETED', 'Project entry deleted successfully.', 'SUCCESS', 200),
                              ('PROJECT_PUBLISHED', 'Project entry published successfully.', 'SUCCESS', 200),
                              ('PROJECT_UNPUBLISHED', 'Project entry unpublished successfully.', 'SUCCESS', 200),
                              ('PROJECT_IMAGE_UPLOADED', 'Project image uploaded successfully.', 'SUCCESS', 200),
                              ('PROJECT_IMAGE_DELETED', 'Project image deleted successfully.', 'SUCCESS', 200),
                              ('PROJECT_IMAGES_REORDERED', 'Project images reordered successfully.', 'SUCCESS', 200),
                              ('PROJECT_NOT_FOUND', 'Project record not found.', 'ERROR', 404),
                              ('PROJECT_IMAGE_NOT_FOUND', 'Project image record not found.', 'ERROR', 404),
                              ('MAXIMUM_PROJECT_IMAGES_EXCEEDED', 'Maximum 15 images allowed per Project.', 'ERROR', 400),
                              ('INVALID_PROJECT_ID', 'Invalid Project ID specified.', 'ERROR', 400);

INSERT INTO drs_error_message_mst (
    MessageCode,
    MessageText,
    MessageType,
    HttpStatusCode,
    IsActive,
    IsDeleted,
    CreatedDate
)
SELECT m.MessageCode,
       m.MessageText,
       m.MessageType,
       m.HttpStatusCode,
       1,
       0,
       GETDATE()
FROM   @ProjectMessages AS m
WHERE  NOT EXISTS (SELECT 1
                   FROM   drs_error_message_mst AS existing
                   WHERE  existing.MessageCode = m.MessageCode
                          AND existing.IsDeleted = 0);


GO
-- Project Showcase Seed Data
IF NOT EXISTS (SELECT 1
               FROM   drs_project_mst
               WHERE  Heading = 'All that comes, comes in play'
                      AND IsDeleted = 0)
    BEGIN
        INSERT  INTO drs_project_mst (
            Heading,
            Subheading,
            ContentType,
            ProjectOrEventTitle,
            DisplayYear,
            ShortDescription,
            DetailedDescription,
            Credits,
            Location,
            ExternalUrl,
            DisplayOrder,
            LayoutType,
            IsPublished,
            customer_id,
            IsActive,
            IsDeleted,
            CreatedDate
        )
        VALUES                       ('All that comes, comes in play', 'All that flows, flows. All that goes, goes. All is One.', 'Studio Narrative', 'Khel-Akhel Studio Philosophy', '2026', 'Khel-Akhel is a creative journey where art takes many forms, shaped by curiosity, experimentation and the natural flow of making.', 'There is no fixed beginning or end, only an unfolding. A play across forms, materials and ideas that becomes a way of seeing—bringing together art, objects, spaces and experiences as they emerge.

Khel-Akhel is an invitation to play, explore and connect. All are welcome to look, interact, participate, immerse, reflect and perhaps discover something new along the way.', NULL, 'India', 'https://www.khel-akhel.com', 1, 'grid-3', 1, NULL, 1, 0, GETDATE());
    END


GO
-- Seed Cart & Wishlist Error Messages
MERGE INTO drs_error_message_mst
 AS target
USING (VALUES ('CART_NOT_FOUND', 'Customer cart was not found.', 'ERROR', 404, 1, 0, GETDATE()), ('CART_EMPTY', 'Customer cart is currently empty.', 'ERROR', 400, 1, 0, GETDATE()), ('CART_ITEM_NOT_FOUND', 'Requested item was not found in customer cart.', 'ERROR', 404, 1, 0, GETDATE()), ('CART_ITEM_ADDED', 'Item added to cart successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('CART_ITEM_UPDATED', 'Cart item quantity updated successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('CART_ITEM_REMOVED', 'Item removed from cart successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('CART_CLEARED', 'Cart cleared successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('WISHLIST_NOT_FOUND', 'Wishlist was not found.', 'ERROR', 404, 1, 0, GETDATE()), ('WISHLIST_ITEM_NOT_FOUND', 'Requested item was not found in wishlist.', 'ERROR', 404, 1, 0, GETDATE()), ('WISHLIST_ITEM_ADDED', 'Product added to wishlist successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('WISHLIST_ITEM_REMOVED', 'Product removed from wishlist successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('WISHLIST_CLEARED', 'Wishlist cleared successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('WISHLIST_MERGED', 'Guest wishlist merged into customer account successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('ITEM_MOVED_TO_CART', 'Product moved from wishlist to cart successfully.', 'SUCCESS', 200, 1, 0, GETDATE()), ('INSUFFICIENT_STOCK', 'Insufficient product inventory for the requested quantity.', 'ERROR', 400, 1, 0, GETDATE()), ('PRODUCT_INACTIVE', 'Product is currently unavailable or inactive.', 'ERROR', 400, 1, 0, GETDATE()), ('INVALID_QUANTITY', 'Requested quantity must be greater than zero.', 'ERROR', 400, 1, 0, GETDATE())) AS source(MessageCode, MessageText, MessageType, HttpStatusCode, IsActive, IsDeleted, CreatedDate) ON target.MessageCode = source.MessageCode
WHEN NOT MATCHED THEN INSERT (
    MessageCode,
    MessageText,
    MessageType,
    HttpStatusCode,
    IsActive,
    IsDeleted,
    CreatedDate
) VALUES (source.MessageCode, source.MessageText, source.MessageType, source.HttpStatusCode, source.IsActive, source.IsDeleted, source.CreatedDate);


GO
