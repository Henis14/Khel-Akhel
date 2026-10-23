
USE CRM_Kehl_Akhel;
GO

SET ANSI_NULLS ON;
GO

SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER VIEW drs_hero_banner_mst
AS
    SELECT Id,Title,MediaUrl,IsImage,IsVideo,DisplayOrder,IsActive,IsDeleted,CreatedAt,UpdatedAt FROM HeroBanners;
GO
