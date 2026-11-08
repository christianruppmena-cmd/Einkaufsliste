
using Einkaufsliste.Models;
using SQLite;

namespace Einkaufsliste.Services;

public class DatenbankService
{
    public SQLiteAsyncConnection Verbindung { get; private set; }

    public string DatenbankPfad { get; }

    private readonly SemaphoreSlim _initialisierungsSperre = new(1, 1);
    private bool _istInitialisiert;
    public DatenbankService()
    {
        DatenbankPfad = Path.Combine(
            FileSystem.AppDataDirectory,
            "einkaufliste.db3");

        Verbindung = new SQLiteAsyncConnection(DatenbankPfad);
    }

    public void VerbindungNeuErstellen()
    {
        Verbindung = new SQLiteAsyncConnection(DatenbankPfad);

        _istInitialisiert = false;
    }

    public async Task InitialisierenAsync()
    {
        await _initialisierungsSperre.WaitAsync();

        try
        {
            if (_istInitialisiert)
                return;


            var datenbank = Verbindung;

            // Einkaufslisten
            await datenbank.CreateTableAsync<Einkaufsartikel>();
            await datenbank.CreateTableAsync<Kategorie>();

            // Produktverwaltung
            await datenbank.CreateTableAsync<Produkt>();
            // Migration: Bevorzugten Händler beim Produkt ergänzen
            var produktSpalten =
                await datenbank.GetTableInfoAsync("Produkt");

            if (!produktSpalten.Any(s => s.Name == "BevorzugterHaendlerId"))
            {
                await datenbank.ExecuteAsync(
                    "ALTER TABLE Produkt ADD COLUMN BevorzugterHaendlerId INTEGER NULL");
            }

            // Migration: Ausgewählten Händler beim Einkaufsartikel ergänzen
            var artikelSpalten =
                await datenbank.GetTableInfoAsync("Einkaufsartikel");

            if (!artikelSpalten.Any(s => s.Name == "AusgewaehlterHaendlerId"))
            {
                await datenbank.ExecuteAsync(
                    "ALTER TABLE Einkaufsartikel ADD COLUMN AusgewaehlterHaendlerId INTEGER NULL");
            }

            // Preisvergleich
            await datenbank.CreateTableAsync<Haendler>();
            await datenbank.CreateTableAsync<Produktpreis>();
            await datenbank.CreateTableAsync<PreisHistorie>();

            // Migration: Logo-Pfad beim Händler ergänzen
            var haendlerSpalten =
                await datenbank.GetTableInfoAsync("Haendler");

            if (!haendlerSpalten.Any(s => s.Name == "LogoPfad"))
            {
                await datenbank.ExecuteAsync(
                    "ALTER TABLE Haendler ADD COLUMN LogoPfad TEXT NULL");
            }

            // Verknüpfung zwischen Einkaufsartikel und Produkt
            var spalten = await datenbank.GetTableInfoAsync("Einkaufsartikel");

            if (!spalten.Any(s => s.Name == "ProduktId"))
            {
                await datenbank.ExecuteAsync(
                    "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
            }
            // Migration PreisHistorie: Versionsnummer ergänzen
            var historieSpalten =
                await datenbank.GetTableInfoAsync("PreisHistorie");

            if (!historieSpalten.Any(s => s.Name == "Versionsnummer"))
            {
                await datenbank.ExecuteAsync(
                    "ALTER TABLE PreisHistorie ADD COLUMN Versionsnummer INTEGER NOT NULL DEFAULT 0");
            }

            _istInitialisiert = true;
        }
        finally
        {
            _initialisierungsSperre.Release();
        }
    }
}
