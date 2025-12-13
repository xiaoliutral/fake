-- 创建数据库
CREATE DATABASE IF NOT EXISTS fake_simple_admin 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_0900_ai_ci;

USE fake_simple_admin;

-- 用户表
CREATE TABLE IF NOT EXISTS `user` (
    `Id` CHAR(36) NOT NULL,
    `Name` VARCHAR(32) NOT NULL,
    `Account` VARCHAR(32) NOT NULL,
    `Password` VARCHAR(100) NOT NULL,
    `Salt` VARCHAR(50) NOT NULL,
    `Email` VARCHAR(32) NULL,
    `Avatar` VARCHAR(64) NULL,
    `CreateTime` DATETIME(6) NOT NULL,
    `CreatorId` CHAR(36) NULL,
    `LastModificationTime` DATETIME(6) NULL,
    `LastModifierId` CHAR(36) NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeleterId` CHAR(36) NULL,
    `DeletionTime` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_user_Account` (`Account`),
    INDEX `IX_user_Email` (`Email`),
    INDEX `IX_user_Name` (`Name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 角色表
CREATE TABLE IF NOT EXISTS `role` (
    `Id` CHAR(36) NOT NULL,
    `Name` VARCHAR(32) NOT NULL,
    `Code` VARCHAR(32) NOT NULL,
    `CreateTime` DATETIME(6) NOT NULL,
    `CreatorId` CHAR(36) NULL,
    `LastModificationTime` DATETIME(6) NULL,
    `LastModifierId` CHAR(36) NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeleterId` CHAR(36) NULL,
    `DeletionTime` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_role_Code` (`Code`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 用户角色关联表
CREATE TABLE IF NOT EXISTS `user_role` (
    `Id` CHAR(36) NOT NULL,
    `UserId` CHAR(36) NOT NULL,
    `RoleId` CHAR(36) NOT NULL,
    `CreateTime` DATETIME(6) NOT NULL,
    `CreatorId` CHAR(36) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_user_role_UserId_RoleId` (`UserId`, `RoleId`),
    INDEX `IX_user_role_RoleId` (`RoleId`),
    CONSTRAINT `FK_user_role_user_UserId` FOREIGN KEY (`UserId`) REFERENCES `user` (`Id`) ON DELETE CASCADE,
    CONSTRAINT `FK_user_role_role_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `role` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 角色权限表
CREATE TABLE IF NOT EXISTS `role_permission` (
    `Id` CHAR(36) NOT NULL,
    `RoleId` CHAR(36) NOT NULL,
    `PermissionCode` VARCHAR(100) NOT NULL,
    `CreateTime` DATETIME(6) NOT NULL,
    `CreatorId` CHAR(36) NULL,
    PRIMARY KEY (`Id`),
    UNIQUE INDEX `IX_role_permission_RoleId_PermissionCode` (`RoleId`, `PermissionCode`),
    CONSTRAINT `FK_role_permission_role_RoleId` FOREIGN KEY (`RoleId`) REFERENCES `role` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- 菜单表
CREATE TABLE IF NOT EXISTS `menu` (
    `Id` CHAR(36) NOT NULL,
    `PId` CHAR(36) NOT NULL,
    `Name` VARCHAR(64) NOT NULL,
    `PermissionCode` VARCHAR(64) NULL,
    `Type` INT NOT NULL,
    `Icon` VARCHAR(64) NULL,
    `Route` VARCHAR(64) NULL,
    `Component` VARCHAR(64) NULL,
    `Order` INT NOT NULL,
    `IsHidden` TINYINT(1) NOT NULL,
    `IsCached` TINYINT(1) NOT NULL,
    `Description` VARCHAR(256) NULL,
    `CreateTime` DATETIME(6) NOT NULL,
    `CreatorId` CHAR(36) NULL,
    `LastModificationTime` DATETIME(6) NULL,
    `LastModifierId` CHAR(36) NULL,
    `IsDeleted` TINYINT(1) NOT NULL DEFAULT 0,
    `DeleterId` CHAR(36) NULL,
    `DeletionTime` DATETIME(6) NULL,
    PRIMARY KEY (`Id`),
    INDEX `IX_menu_Name` (`Name`),
    INDEX `IX_menu_PermissionCode` (`PermissionCode`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
