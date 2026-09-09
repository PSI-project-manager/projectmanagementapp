CREATE TABLE Roles (
    RoleId          INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(50)  NOT NULL UNIQUE,
    Description     NVARCHAR(200) NULL
);

CREATE TABLE Users (
    UserId          INT IDENTITY(1,1) PRIMARY KEY,
    Email           NVARCHAR(255) NOT NULL UNIQUE,
    PasswordHash    NVARCHAR(255) NOT NULL,
    FullName        NVARCHAR(150) NOT NULL,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2     NULL
);

CREATE TABLE UserRoles (
    UserId          INT NOT NULL,
    RoleId          INT NOT NULL,
    AssignedAt      DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
);

CREATE TABLE Sessions (
    SessionId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
    UserId          INT NOT NULL,
    Token           NVARCHAR(500) NOT NULL UNIQUE,
    CreatedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    ExpiresAt       DATETIME2 NOT NULL,
    RevokedAt       DATETIME2 NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE Projects (
    ProjectId       INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(150) NOT NULL,
    Description     NVARCHAR(1000) NULL,
    IsActive        BIT           NOT NULL DEFAULT 1,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2     NULL,
    CreatedByUserId INT           NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE ProjectUsers (
    ProjectId       INT NOT NULL,
    UserId          INT NOT NULL,
    GrantedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PRIMARY KEY (ProjectId, UserId),
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    FOREIGN KEY (UserId)    REFERENCES Users(UserId)       ON DELETE CASCADE
);

CREATE TABLE ItemTypes (
    ItemTypeId      INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL UNIQUE,
    IsActive        BIT           NOT NULL DEFAULT 1
);

CREATE TABLE Statuses (
    StatusId        INT IDENTITY(1,1) PRIMARY KEY,
    Name            NVARCHAR(100) NOT NULL UNIQUE,
    SortOrder       INT           NOT NULL DEFAULT 0,
    IsActive        BIT           NOT NULL DEFAULT 1
);

CREATE TABLE Items (
    ItemId          INT IDENTITY(1,1) PRIMARY KEY,
    ProjectId       INT           NOT NULL,
    Title           NVARCHAR(200) NOT NULL,
    Description     NVARCHAR(MAX) NULL,
    ItemTypeId      INT           NOT NULL,
    StatusId        INT           NOT NULL,
    CreatedByUserId INT           NOT NULL,
    AssignedToUserId INT          NULL,
    CreatedAt       DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
    UpdatedAt       DATETIME2     NULL,
    FOREIGN KEY (ProjectId)        REFERENCES Projects(ProjectId),
    FOREIGN KEY (ItemTypeId)       REFERENCES ItemTypes(ItemTypeId),
    FOREIGN KEY (StatusId)         REFERENCES Statuses(StatusId),
    FOREIGN KEY (CreatedByUserId)  REFERENCES Users(UserId),
    FOREIGN KEY (AssignedToUserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_Items_ProjectId ON Items(ProjectId);
CREATE INDEX IX_Items_StatusId  ON Items(StatusId);
CREATE INDEX IX_Items_ItemTypeId ON Items(ItemTypeId);
CREATE INDEX IX_Items_Title     ON Items(Title);

CREATE TABLE ItemHistory (
    ItemHistoryId   INT IDENTITY(1,1) PRIMARY KEY,
    ItemId          INT NOT NULL,
    ChangedByUserId INT NOT NULL,
    FieldChanged    NVARCHAR(50) NOT NULL,
    OldValue        NVARCHAR(MAX) NULL,
    NewValue        NVARCHAR(MAX) NULL,
    ChangedAt       DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    FOREIGN KEY (ItemId)          REFERENCES Items(ItemId) ON DELETE CASCADE,
    FOREIGN KEY (ChangedByUserId) REFERENCES Users(UserId)
);
