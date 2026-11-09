using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public partial class EinkauflistenDatenbank
{
    private SQLiteAsyncConnection _datenbank;
    private readonly string datenbankPfad =
        Path.Combine(FileSystem.AppDataDirectory, "einkaufliste.db3");
    private readonly IDBBackupService _backupService;
    public EinkauflistenDatenbank(IDBBackupService backupService)
    {
        _datenbank = new SQLiteAsyncConnection(datenbankPfad);
        _backupService = backupService;
    }
    public async Task InitialisierenAsync()
    {
        // Einkaufslisten
        await _datenbank.CreateTableAsync<Einkaufsartikel>();
        await _datenbank.CreateTableAsync<Kategorie>();

        // Produktverwaltung
        await _datenbank.CreateTableAsync<Produkt>();
        // Migration: Bevorzugten Händler beim Produkt ergänzen
        var produktSpalten =
            await _datenbank.GetTableInfoAsync("Produkt");

        if (!produktSpalten.Any(s => s.Name == "BevorzugterHaendlerId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Produkt ADD COLUMN BevorzugterHaendlerId INTEGER NULL");
        }

        // Migration: Ausgewählten Händler beim Einkaufsartikel ergänzen
        var artikelSpalten =
            await _datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!artikelSpalten.Any(s => s.Name == "AusgewaehlterHaendlerId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN AusgewaehlterHaendlerId INTEGER NULL");
        }

        // Preisvergleich
        await _datenbank.CreateTableAsync<Haendler>();
        await _datenbank.CreateTableAsync<Produktpreis>();
        await _datenbank.CreateTableAsync<PreisHistorie>();

        // Verknüpfung zwischen Einkaufsartikel und Produkt
        var spalten = await _datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spalten.Any(s => s.Name == "ProduktId"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
        }
        // Migration PreisHistorie: Versionsnummer ergänzen
        var historieSpalten =
            await _datenbank.GetTableInfoAsync("PreisHistorie");

        if (!historieSpalten.Any(s => s.Name == "Versionsnummer"))
        {
            await _datenbank.ExecuteAsync(
                "ALTER TABLE PreisHistorie ADD COLUMN Versionsnummer INTEGER NOT NULL DEFAULT 0");
        }
    }
    
    
    

}
