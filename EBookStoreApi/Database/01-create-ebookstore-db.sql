-- 1. สร้างฐานข้อมูล EBookStoreDb
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'EBookStoreDb')
BEGIN
    CREATE DATABASE EBookStoreDb;
END
GO

USE EBookStoreDb;
GO

-- 2. สร้างตาราง Authors (ผู้เขียน)
IF OBJECT_ID('dbo.Books', 'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID('dbo.Authors', 'U') IS NOT NULL DROP TABLE dbo.Authors;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
GO

CREATE TABLE dbo.Authors (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Authors PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Email NVARCHAR(255) NULL,
    Bio NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Authors_CreatedAt DEFAULT GETUTCDATE()
);
GO

-- 3. สร้างตาราง Categories (หมวดหมู่)
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(50) NOT NULL CONSTRAINT UQ_Categories_Name UNIQUE,
    Description NVARCHAR(255) NULL
);
GO

-- 4. สร้างตาราง Books (หนังสืออิเล็กทรอนิกส์)
CREATE TABLE dbo.Books (
    Id INT IDENTITY(1,1) CONSTRAINT PK_Books PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    ISBN VARCHAR(20) NOT NULL CONSTRAINT UQ_Books_ISBN UNIQUE,
    Price DECIMAL(10,2) NOT NULL CONSTRAINT CK_Books_Price CHECK (Price >= 0),
    PublishedDate DATE NOT NULL,
    FileUrl NVARCHAR(500) NULL,
    AuthorId INT NOT NULL,
    CategoryId INT NOT NULL,
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Books_CreatedAt DEFAULT GETUTCDATE(),
    CONSTRAINT FK_Books_Authors FOREIGN KEY (AuthorId) REFERENCES dbo.Authors(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Books_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id) ON DELETE NO ACTION
);
GO

-- 5. ทำการ Seed Data ข้อมูลจำลองเริ่มต้น
INSERT INTO dbo.Authors (Name, Email, Bio) VALUES
(N'Robert C. Martin', N'unclebob@cleancoder.com', N'Author of Clean Code and Clean Architecture'),
(N'Andrew Troelsen', N'andrew@apress.com', N'Author of Pro C# and .NET'),
(N'Martin Fowler', N'fowler@thoughtworks.com', N'Chief Scientist at ThoughtWorks, author of Refactoring');

INSERT INTO dbo.Categories (Name, Description) VALUES
(N'Software Engineering', N'Best practices, software design and architecture'),
(N'.NET & C#', N'C#, ASP.NET Core, EF Core and .NET runtime'),
(N'Cloud & DevOps', N'Docker, Kubernetes, Azure and CI/CD');

INSERT INTO dbo.Books (Title, ISBN, Price, PublishedDate, FileUrl, AuthorId, CategoryId) VALUES
(N'Clean Code: A Handbook of Agile Software Craftsmanship', '978-0132350884', 650.00, '2008-08-01', N'https://storage.ebooks.com/files/clean-code.pdf', 1, 1),
(N'Pro C# 10 with .NET 6', '978-1484278680', 1250.00, '2022-04-15', N'https://storage.ebooks.com/files/pro-csharp-10.pdf', 2, 2),
(N'Refactoring: Improving the Design of Existing Code', '978-0134757599', 890.00, '2018-11-20', N'https://storage.ebooks.com/files/refactoring.pdf', 3, 1);
GO
