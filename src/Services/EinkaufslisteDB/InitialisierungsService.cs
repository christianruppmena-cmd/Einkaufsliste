using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services;

public class InitialisierungsService : DBConnection, IInitialisierungsService
{
    
    public async Task InitialisierenAsync()
    {
        // Einkaufslisten
        await Datenbank.CreateTableAsync<Einkaufsartikel>();
        await Datenbank.CreateTableAsync<Kategorie>();

        // Produktverwaltung
        await Datenbank.CreateTableAsync<Produkt>();
        // Migration: Bevorzugten Händler beim Produkt ergänzen
        var produktSpalten =
            await Datenbank.GetTableInfoAsync("Produkt");

        if (!produktSpalten.Any(s => s.Name == "BevorzugterHaendlerId"))
        {
            await Datenbank.ExecuteAsync(
                "ALTER TABLE Produkt ADD COLUMN BevorzugterHaendlerId INTEGER NULL");
        }

        // Migration: Ausgewählten Händler beim Einkaufsartikel ergänzen
        var artikelSpalten =
            await Datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!artikelSpalten.Any(s => s.Name == "AusgewaehlterHaendlerId"))
        {
            await Datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN AusgewaehlterHaendlerId INTEGER NULL");
        }

        // Preisvergleich
        await Datenbank.CreateTableAsync<Haendler>();
        await Datenbank.CreateTableAsync<Produktpreis>();
        await Datenbank.CreateTableAsync<PreisHistorie>();

        // Verknüpfung zwischen Einkaufsartikel und Produkt
        var spalten = await Datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spalten.Any(s => s.Name == "ProduktId"))
        {
            await Datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
        }
        // Migration PreisHistorie: Versionsnummer ergänzen
        var historieSpalten =
            await Datenbank.GetTableInfoAsync("PreisHistorie");

        if (!historieSpalten.Any(s => s.Name == "Versionsnummer"))
        {
            await Datenbank.ExecuteAsync(
                "ALTER TABLE PreisHistorie ADD COLUMN Versionsnummer INTEGER NOT NULL DEFAULT 0");
        }
    }
   
}
