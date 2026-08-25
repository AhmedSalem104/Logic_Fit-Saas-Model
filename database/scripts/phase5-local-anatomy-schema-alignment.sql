/* One-time LOCAL schema alignment discovered during the EF seed smoke test. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
USE [LogicFit_Gym_001_Local];

IF DB_NAME() <> N'LogicFit_Gym_001_Local'
    THROW 51020, 'Unexpected database. Expected LogicFit_Gym_001_Local.', 1;

IF OBJECT_ID(N'library.anatomy_mappings', N'U') IS NULL
    THROW 51021, 'Anatomy mapping table is missing; alignment aborted.', 1;

BEGIN TRANSACTION;

IF COL_LENGTH(N'library.anatomy_mappings', N'name_ar') IS NULL
    ALTER TABLE [library].[anatomy_mappings] ADD [name_ar] nvarchar(200) NULL;

IF COL_LENGTH(N'library.anatomy_mappings', N'name_en') IS NULL
    ALTER TABLE [library].[anatomy_mappings] ADD [name_en] nvarchar(300) NULL;

EXEC sys.sp_executesql N'
    UPDATE [library].[anatomy_mappings]
    SET [name_ar] = COALESCE([name_ar], JSON_VALUE([payload_json], ''$.provenance.system_name_ar'')),
        [name_en] = COALESCE([name_en], JSON_VALUE([payload_json], ''$.provenance.system_name''))
    WHERE [name_en] IS NULL;';

EXEC sys.sp_executesql N'
    IF EXISTS (SELECT 1 FROM [library].[anatomy_mappings] WHERE [name_en] IS NULL)
        THROW 51022, ''Anatomy mapping name_en could not be recovered from canonical provenance.'', 1;';

EXEC sys.sp_executesql N'
    ALTER TABLE [library].[anatomy_mappings] ALTER COLUMN [name_en] nvarchar(300) NOT NULL;';

COMMIT TRANSACTION;
PRINT 'LogicFit local anatomy schema alignment completed.';
