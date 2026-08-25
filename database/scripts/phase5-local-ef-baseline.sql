/*
   One-time LOCAL transition from the draft Node/Fastify SQL migration marker
   to the official EF Core migration history.

   Safety boundary:
   - This script accepts only the two named LogicFit local databases.
   - It must not be run against TOP GYM, staging, or production.
   - A verified COPY_ONLY backup was created before this transition.
   - The legacy migrations.schema_migrations marker is removed only after
     the required schema is confirmed and EF history is recorded.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

USE [LogicFit_ControlPlane_Local];
IF DB_NAME() <> N'LogicFit_ControlPlane_Local'
    THROW 51000, 'Unexpected database. Expected LogicFit_ControlPlane_Local.', 1;

IF OBJECT_ID(N'platform.organizations', N'U') IS NULL
   OR OBJECT_ID(N'platform.gyms', N'U') IS NULL
   OR OBJECT_ID(N'platform.gym_databases', N'U') IS NULL
   OR OBJECT_ID(N'iam.users', N'U') IS NULL
   OR OBJECT_ID(N'iam.permissions', N'U') IS NULL
   OR OBJECT_ID(N'iam.roles', N'U') IS NULL
   OR OBJECT_ID(N'iam.role_permissions', N'U') IS NULL
   OR OBJECT_ID(N'iam.sessions', N'U') IS NULL
   OR OBJECT_ID(N'audit.events', N'U') IS NULL
    THROW 51001, 'Required Control Plane schema is incomplete; baseline aborted.', 1;

IF COL_LENGTH(N'iam.users', N'user_id') IS NULL
   OR COL_LENGTH(N'iam.users', N'email') IS NULL
   OR COL_LENGTH(N'iam.sessions', N'token_hash') IS NULL
   OR COL_LENGTH(N'iam.sessions', N'gym_id') IS NULL
    THROW 51002, 'Required Control Plane authentication columns are incomplete; baseline aborted.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory]
    (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825144155_InitialControlPlaneFoundation'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825144155_InitialControlPlaneFoundation', N'10.0.0');
END;

IF OBJECT_ID(N'migrations.schema_migrations', N'U') IS NOT NULL
    DROP TABLE [migrations].[schema_migrations];

COMMIT TRANSACTION;

USE [LogicFit_Gym_001_Local];
IF DB_NAME() <> N'LogicFit_Gym_001_Local'
    THROW 51010, 'Unexpected database. Expected LogicFit_Gym_001_Local.', 1;

IF OBJECT_ID(N'core.gym_context', N'U') IS NULL
   OR OBJECT_ID(N'auth.gym_users', N'U') IS NULL
   OR OBJECT_ID(N'library.exercises', N'U') IS NULL
   OR OBJECT_ID(N'library.foods', N'U') IS NULL
   OR OBJECT_ID(N'library.__seed_installations', N'U') IS NULL
   OR OBJECT_ID(N'audit.events', N'U') IS NULL
    THROW 51011, 'Required Gym schema is incomplete; baseline aborted.', 1;

IF COL_LENGTH(N'core.gym_context', N'control_plane_gym_id') IS NULL
   OR COL_LENGTH(N'auth.gym_users', N'gym_user_id') IS NULL
   OR COL_LENGTH(N'library.exercises', N'exercise_id') IS NULL
   OR COL_LENGTH(N'library.exercises', N'seed_key') IS NULL
   OR COL_LENGTH(N'library.foods', N'food_id') IS NULL
    THROW 51012, 'Required Gym authentication/library columns are incomplete; baseline aborted.', 1;

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.__EFMigrationsHistory', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory]
    (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825144011_InitialGymFoundation'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825144011_InitialGymFoundation', N'10.0.0');
END;

IF OBJECT_ID(N'migrations.schema_migrations', N'U') IS NOT NULL
    DROP TABLE [migrations].[schema_migrations];

COMMIT TRANSACTION;

PRINT 'LogicFit local EF baseline completed.';
