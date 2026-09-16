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
    IsActive        BOOLEAN      NOT NULL DEFAULT TRUE,
    CreatedAt       TIMESTAMPTZ  NOT NULL DEFAULT now(),
    UpdatedAt       TIMESTAMPTZ  NULL
);

CREATE TABLE UserRoles (
    UserId          INT NOT NULL,
    RoleId          INT NOT NULL,
    AssignedAt      TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (UserId, RoleId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (RoleId) REFERENCES Roles(RoleId) ON DELETE CASCADE
);

CREATE TABLE Sessions (
    SessionId       UUID NOT NULL DEFAULT gen_random_uuid() PRIMARY KEY,
    UserId          INT NOT NULL,
    Token           VARCHAR(500) NOT NULL UNIQUE,
    CreatedAt       TIMESTAMPTZ NOT NULL DEFAULT now(),
    ExpiresAt       TIMESTAMPTZ NOT NULL,
    RevokedAt       TIMESTAMPTZ NULL,
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE
);

CREATE TABLE Projects (
    ProjectId       INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    Name            VARCHAR(150) NOT NULL,
    Description     VARCHAR(1000) NULL,
    IsActive        BOOLEAN       NOT NULL DEFAULT TRUE,
    CreatedAt       TIMESTAMPTZ   NOT NULL DEFAULT now(),
    UpdatedAt       TIMESTAMPTZ   NULL,
    CreatedByUserId INT           NOT NULL,
    FOREIGN KEY (CreatedByUserId) REFERENCES Users(UserId)
);

CREATE TABLE ProjectUsers (
    ProjectId       INT NOT NULL,
    UserId          INT NOT NULL,
    GrantedAt       TIMESTAMPTZ NOT NULL DEFAULT now(),
    PRIMARY KEY (ProjectId, UserId),
    FOREIGN KEY (ProjectId) REFERENCES Projects(ProjectId) ON DELETE CASCADE,
    FOREIGN KEY (UserId)    REFERENCES Users(UserId)       ON DELETE CASCADE
);

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
    CreatedByUserId INT           NOT NULL,
    AssignedToUserId INT          NULL,
    CreatedAt       TIMESTAMPTZ   NOT NULL DEFAULT now(),
    UpdatedAt       TIMESTAMPTZ   NULL,
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
    ItemHistoryId   INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    ItemId          INT NOT NULL,
    ChangedByUserId INT NOT NULL,
    FieldChanged    VARCHAR(50) NOT NULL,
    OldValue        TEXT NULL,
    NewValue        TEXT NULL,
    ChangedAt       TIMESTAMPTZ NOT NULL DEFAULT now(),
    FOREIGN KEY (ItemId)          REFERENCES Items(ItemId) ON DELETE CASCADE,
    FOREIGN KEY (ChangedByUserId) REFERENCES Users(UserId)
);
