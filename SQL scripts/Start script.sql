

USE VectorRagDemo;
GO

DROP TABLE Bron
DROP TABLE Chunk
DROP TABLE Project

CREATE TABLE Chunk (
    ID INT PRIMARY KEY IDENTITY(1,1),
    BronID INT NOT NULL,
    Tekst NVARCHAR(MAX) NOT NULL,
    TekstVector VECTOR(768) NOT NULL,  
    GemaaktOp DATETIME2 NOT NULL,
    GewijzigdOp DATETIME2 NULL,
    Status INT NOT NULL 
);
GO

CREATE TABLE Bron (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Title VARCHAR(200) NOT NULL,
    Project INT NOT NULL,
    GemaaktOp DATETIME2 NOT NULL,
    GewijzigdOp DATETIME2 NULL,
    Status INT NOT NULL 
)

CREATE TABLE Project (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Naam VARCHAR(200) NOT NULL,
    BaseUrl VARCHAR(1000) NULL,
    ProductLinkSelector VARCHAR(200) NULL,
    GemaaktOp DATETIME2 NOT NULL,
    GewijzigdOp DATETIME2 NULL,
    Status INT NOT NULL 
)

INSERT INTO [VectorRagDemo].[dbo].[Project] (Naam, BaseUrl, ProductLinkSelector, GemaaktOp, Status) 
VALUES ('De Lijsten Fabriek', 'https://www.delijstenfabriek.nl', 'a.product-link', GETDATE(), 1);

  CREATE TABLE ScrapingElement (
    ID INT PRIMARY KEY IDENTITY(1,1),
    Project INT NOT NULL,
    ElementName NVARCHAR(50) NOT NULL,
    Selector NVARCHAR(500) NOT NULL,
    AttributeName NVARCHAR(50) NULL,
    IsRequired BIT DEFAULT 0 NOT NULL,
    DefaultValue NVARCHAR(100) NULL,
    SortOrder INT NOT NULL,
    GemaaktOp DATETIME2 NOT NULL,
    GewijzigdOp DATETIME2 NULL,
    Status INT NOT NULL
);

INSERT INTO ScrapingElement (Project, ElementName, Selector, AttributeName, IsRequired, DefaultValue, SortOrder, GemaaktOp, Status) 
VALUES 
(1, 'ProductInfo_Title', '.description-read-more h3', 'text', 0, '', 1, GETDATE(), 1),
(1, 'ProductInfo_Description', '.description-read-more p', 'text', 1, '', 2, GETDATE(), 1);


