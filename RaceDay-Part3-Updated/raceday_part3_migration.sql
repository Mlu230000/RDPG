/*
   RaceDay South Africa - Part 3 Database Migration
   Adds support for Azure Blob Storage Cloud Image URLs:
   - Events.banner_image_url
   - Users.profile_picture_url
*/

USE RaceDay;
GO

-- 1. Add banner_image_url to Events table
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Events') AND name = 'banner_image_url'
)
BEGIN
    ALTER TABLE dbo.Events
    ADD banner_image_url VARCHAR(500) NULL;
    PRINT 'Added banner_image_url column to dbo.Events.';
END
GO

-- 2. Add profile_picture_url to Users table
IF NOT EXISTS (
    SELECT 1 FROM sys.columns 
    WHERE object_id = OBJECT_ID('dbo.Users') AND name = 'profile_picture_url'
)
BEGIN
    ALTER TABLE dbo.Users
    ADD profile_picture_url VARCHAR(500) NULL;
    PRINT 'Added profile_picture_url column to dbo.Users.';
END
GO
