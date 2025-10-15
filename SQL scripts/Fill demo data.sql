USE VectorRagDemo;
GO

-- Insert a sample project
INSERT INTO Project (Naam, GemaaktOp, Status)
VALUES ('Test Webshop', GETDATE(), 1);

DECLARE @ProjectID INT = SCOPE_IDENTITY();

-- Insert a sample bron (source document)
INSERT INTO Bron (Title, Project, GemaaktOp, Status)
VALUES ('Product Catalog 2025', @ProjectID, GETDATE(), 1);

DECLARE @BronID INT = SCOPE_IDENTITY();

-- Insert some sample chunks with dummy vectors (we'll add real vectors later)
-- For now, just create random vectors to test
DECLARE @i INT = 1;
WHILE @i <= 5
BEGIN
    -- Generate a simple dummy vector (all zeros for now)
    DECLARE @dummyVector VARCHAR(MAX) = '[' + REPLICATE('0,', 767) + '0]';
    
    INSERT INTO Chunk (BronID, Tekst, TekstVector, GemaaktOp, Status)
    VALUES (
        @BronID,
        'Sample product text ' + CAST(@i AS VARCHAR),
        CAST(@dummyVector AS VECTOR(768)),
        GETDATE(),
        1
    );
    
    SET @i = @i + 1;
END

SELECT * FROM Project;
SELECT * FROM Bron;
SELECT * FROM Chunk;