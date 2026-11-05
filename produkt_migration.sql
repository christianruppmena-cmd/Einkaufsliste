
BEGIN IMMEDIATE;

CREATE TABLE Produkt_Neu (
    Id INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL,
    Name TEXT NOT NULL,
    Kategorie TEXT NOT NULL DEFAULT 'Sonstiges',
    AldiSku TEXT,
    Marke TEXT,
    Variante TEXT,
    Packungsgroesse REAL,
    Basiseinheit TEXT,
    BildPfad TEXT,
    IstAktiv INTEGER NOT NULL DEFAULT 1
);

INSERT INTO Produkt_Neu (
    Id, Name, Kategorie, AldiSku, Marke,
    Variante, Packungsgroesse, Basiseinheit,
    BildPfad, IstAktiv
)
SELECT
    Id, Name, Kategorie, AldiSku, Marke,
    Variante, Packungsgroesse, Basiseinheit,
    BildPfad, IstAktiv
FROM Produkt;

DROP TABLE Produkt;

ALTER TABLE Produkt_Neu RENAME TO Produkt;

COMMIT;

PRAGMA integrity_check;
SELECT COUNT(*) AS AnzahlProdukte FROM Produkt;
