-- ---------------------------------------------------------------------------
-- Blazor_Portfolio — login database schema (MySQL 8.0+)
--
-- How to run:
--   1. Open MySQL Workbench and connect to your local server.
--   2. Open this file (File > Open SQL Script) and execute it with the lightning
--      bolt button (Ctrl+Shift+Enter).
--   3. Point the "Default" connection string in appsettings.json at the same
--      server, database, user and password created here.
--
-- Safe to re-run: it drops and recreates only its own objects.
-- ---------------------------------------------------------------------------

CREATE DATABASE IF NOT EXISTS `blazor_portfolio`
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE `blazor_portfolio`;

-- Application login account. One row per user who can sign in.
CREATE TABLE IF NOT EXISTS `Users` (
    `Id`            INT             NOT NULL AUTO_INCREMENT,
    `Email`         VARCHAR(256)    NOT NULL,
    `DisplayName`   VARCHAR(128)    NOT NULL,
    `PasswordHash`  VARCHAR(512)    NOT NULL,
    `IsActive`      TINYINT(1)      NOT NULL DEFAULT 1,
    `CreatedUtc`    DATETIME(6)     NOT NULL,
    `LastLoginUtc`  DATETIME(6)     NULL,
    PRIMARY KEY (`Id`),
    UNIQUE KEY `IX_Users_Email` (`Email`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Messages submitted through the portfolio contact form.
-- Triage with:
--   SELECT * FROM ContactMessages WHERE IsHandled = 0 ORDER BY CreatedUtc DESC;
-- ---------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS `ContactMessages` (
    `Id`          INT             NOT NULL AUTO_INCREMENT,
    `Name`        VARCHAR(120)    NOT NULL,
    `Email`       VARCHAR(256)    NOT NULL,
    `Company`     VARCHAR(160)    NULL,
    `Message`     VARCHAR(4000)   NOT NULL,
    `CreatedUtc`  DATETIME(6)     NOT NULL,
    `IsHandled`   TINYINT(1)      NOT NULL DEFAULT 0,
    `IpAddress`   VARCHAR(45)     NULL,
    PRIMARY KEY (`Id`),
    KEY `IX_ContactMessages_IsHandled_CreatedUtc` (`IsHandled`, `CreatedUtc`)
) ENGINE = InnoDB
  DEFAULT CHARSET = utf8mb4
  COLLATE = utf8mb4_unicode_ci;

-- ---------------------------------------------------------------------------
-- Demo account:  admin@example.com  /  Passw0rd!
--
-- The hash below is PBKDF2-HMAC-SHA256, formatted as
--   iterations . base64(salt) . base64(key)
-- which is what Blazor_Portfolio.Security.PasswordHasher produces.
--
-- Generate your own with:
--   dotnet run -- --hash-password "YourNewPassword"
-- (see the notes in Database/README.md) or simply insert via the app's own
-- hashing utility. Do not ship the demo account to production.
-- ---------------------------------------------------------------------------
INSERT INTO `Users` (`Email`, `DisplayName`, `PasswordHash`, `IsActive`, `CreatedUtc`)
SELECT 'admin@example.com',
       'Demo Admin',
       '100000.tzksDJlyUvhzrGeqvjshpA==.971djknHiI8z/b9tRBUyR9k38pqHqlP9D9nyM1PP7pg=',
       1,
       UTC_TIMESTAMP(6)
WHERE NOT EXISTS (
    SELECT 1 FROM `Users` WHERE `Email` = 'admin@example.com'
);

-- ---------------------------------------------------------------------------
-- Application user. Replace the password before using this anywhere real.
-- ---------------------------------------------------------------------------
CREATE USER IF NOT EXISTS 'blazorapp'@'localhost' IDENTIFIED BY 'CHANGE_ME';
GRANT SELECT, INSERT, UPDATE, DELETE ON `blazor_portfolio`.* TO 'blazorapp'@'localhost';
FLUSH PRIVILEGES;

SELECT `Id`, `Email`, `DisplayName`, `IsActive`, `CreatedUtc`, `LastLoginUtc`
FROM `Users`;