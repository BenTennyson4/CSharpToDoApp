-- The following sql commands are in Transact SQL (T-SQL)
CREATE TABLE AppUser (
    UserID        INT IDENTITY(1,1) PRIMARY KEY,
    UserPassword  VARCHAR(255) NOT NULL,
    Username      VARCHAR(255) UNIQUE NOT NULL,
    UserCreatedAt DATETIME DEFAULT GETDATE()
);

CREATE TABLE List (
    ListID           INT PRIMARY KEY,
    UserID           INT NOT NULL,
    ListCompleted    BIT DEFAULT 0,
    ListCreatedAt    DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (UserID) REFERENCES AppUser(UserID)
);

CREATE TABLE Task (
    TaskID              INT IDENTITY(1,1) PRIMARY KEY,
    Completed           BIT DEFAULT 0,
    Priority            INT DEFAULT 0,
    TaskText            VARCHAR(360) NOT NULL,
    TaskName            VARCHAR(255),
    ListID              INT NOT NULL,
    ListPosition        INT NOT NULL,
    TaskCreatedAt       DATETIME DEFAULT GETDATE(),
    FOREIGN KEY (ListID) REFERENCES List(ListID)
);