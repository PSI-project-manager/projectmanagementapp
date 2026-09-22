CREATE TABLE Roles (
    RoleId          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name            VARCHAR(50)  NOT NULL UNIQUE,
    Description     VARCHAR(200) NULL
);

CREATE TABLE Users (
    UserId          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Email           VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash    VARCHAR(255) NOT NULL,
    FullName        VARCHAR(150) NOT NULL,
    IsActive        BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE UserRoles (
    UserId          INT NOT NULL,
    RoleId          INT NOT NULL,
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
);

CREATE TABLE Projects (
    ProjectId       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name            VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000) NULL,
    IsActive        BOOLEAN       NOT NULL DEFAULT TRUE,
    CreatedByUserId INT           NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE ProjectUsers (
    ProjectId       INT NOT NULL,
    UserId          INT NOT NULL,
    PRIMARY KEY (ProjectId, UserId),
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE INDEX IX_ProjectUsers_UserId ON ProjectUsers(UserId);

CREATE TABLE ItemTypes (
    ItemTypeId      INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name            VARCHAR(100) NOT NULL UNIQUE,
    IsActive        BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE Statuses (
    StatusId        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name            VARCHAR(100) NOT NULL UNIQUE,
    SortOrder       INT          NOT NULL DEFAULT 0,
    IsActive        BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE Items (
    ItemId          INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ProjectId       INT           NOT NULL,
    Title           VARCHAR(200)  NOT NULL,
    Description     TEXT          NULL,
    ItemTypeId      INT           NOT NULL,
    StatusId        INT           NOT NULL,
    AssignedToUserId INT          NULL,
    FOREIGN KEY (ProjectId)        REFERENCES Projects(ProjectId),
    FOREIGN KEY (ItemTypeId)       REFERENCES ItemTypes(ItemTypeId),
    FOREIGN KEY (StatusId)         REFERENCES Statuses(StatusId),
    FOREIGN KEY (AssignedToUserId) REFERENCES Users(UserId)
);

CREATE INDEX IX_Items_ProjectId ON Items(ProjectId);
CREATE INDEX IX_Items_StatusId  ON Items(StatusId);
CREATE INDEX IX_Items_ItemTypeId ON Items(ItemTypeId);
CREATE INDEX IX_Items_Title     ON Items(Title);