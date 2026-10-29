using SQLite;
using Einkaufsliste.Models;

namespace Einkaufsliste.Services;

public class EinkauflistenDatenbank
{
    private readonly SQLiteAsyncConnection datenbank;

    public EinkauflistenDatenbank()
    {
        string dateiPfad = Path.Combine(
            FileSystem.AppDataDirectory, "einkaufliste.db3");

        datenbank = new SQLiteAsyncConnection(dateiPfad);
    }
    public async Task InitialisierenAsync()
    {
        await datenbank.CreateTableAsync<Einkaufsartikel>();
        await datenbank.CreateTableAsync<Produkt>();
        await datenbank.CreateTableAsync<Kategorie>();

        var spalten = await datenbank.GetTableInfoAsync("Einkaufsartikel");

        bool kategorieExistiert = spalten.Any(spalte => spalte.Name == "Kategorie");

        if (!kategorieExistiert)
        {
            await datenbank.ExecuteAsync
                ("ALTER TABLE Einkaufsartikel ADD COLUMN Kategorie TEXT NOT NULL DEFAULT 'Sonstiges'");

        }

        var spaltenAktuell =
            await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spaltenAktuell.Any(s => s.Name == "Menge"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN Menge TEXT NOT NULL DEFAULT '1'");
        }

        spaltenAktuell =
            await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spaltenAktuell.Any(s => s.Name == "Einheit"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN Einheit TEXT NOT NULL DEFAULT 'Stück'");
        }

        spaltenAktuell =
            await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spaltenAktuell.Any(s => s.Name == "Hinweis"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN Hinweis TEXT NOT NULL DEFAULT ''");
        }
    }

    public async Task StandardkategorienAnlegenAsync()
    {
        await InitialisierenAsync();

        List<Produkt> produkte = await datenbank.Table<Produkt>().ToListAsync();

        List<string> kategorien = produkte
            .Select(p => p.Kategorie.Trim())
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (string kategorienName in kategorien)
        {
            Kategorie? vorhanden = await datenbank
                .Table<Kategorie>()
                .Where(k => k.Name == kategorienName)
                .FirstOrDefaultAsync();
            if (vorhanden == null)
            {
                await datenbank.InsertAsync(new Kategorie
                {
                    Name = kategorienName
                });
            }
        }
    }

    public async Task<List<Kategorie>> AlleKategorieEintraegeLadenAsync()
    {
        await InitialisierenAsync();

        return await datenbank
            .Table<Kategorie>()
            .OrderBy(k => k.Name)
            .ToListAsync();
    }

    public async Task KategorieSpeichernAsync(Kategorie kategorie)
    {
        await InitialisierenAsync();

        if (kategorie.Id == 0)
        {
            await datenbank.InsertAsync(kategorie);

        }
        else
        {
            await datenbank.UpdateAsync(kategorie);
        }
    }
    
    public async Task<bool> KategorieUmbenennenAsync(
        Kategorie kategorie,
        string neuerName)
    {
        await InitialisierenAsync();

        string alterName = kategorie.Name.Trim();
        neuerName = neuerName.Trim();

        if (string.IsNullOrWhiteSpace(neuerName))
            return false;

        // Prüfen ob der neue Name bereits vergene ist

        List<Kategorie> kategorien =
            await datenbank.Table<Kategorie>().ToListAsync();

        bool existiertBereits = kategorien.Any(k =>
        k.Id != kategorie.Id &&
        string.Equals(
            k.Name.Trim(),
            neuerName,
            StringComparison.OrdinalIgnoreCase));

        if (existiertBereits)
            return false;

        // Alle betroffenen Artikel aktualisieren

        List<Einkaufsartikel> artikel =
            await datenbank.Table<Einkaufsartikel>().ToListAsync();

        List<Produkt> produkte =
            await datenbank.Table<Produkt>().ToListAsync();

        await datenbank.RunInTransactionAsync(conn =>
        {
            foreach (Einkaufsartikel eintrag in artikel.Where(a =>
            string.Equals(
                a.Kategorie?.Trim(),
                alterName,
                StringComparison.OrdinalIgnoreCase)))
            {
                eintrag.Kategorie = neuerName;
                conn.Update(eintrag);
            }
            foreach (Produkt produkt in produkte.Where(p =>
            string.Equals(
                p.Kategorie?.Trim(),
                alterName,
                StringComparison.OrdinalIgnoreCase)))
            {
                produkt.Kategorie = neuerName;
                conn.Update(produkt);
            }

            kategorie.Name = neuerName;
            conn.Update(kategorie);
        });
        return true;
    }
    
    public async Task<List<Einkaufsartikel>> AlleArtikelLadenAsync()
    {
        return await datenbank.Table<Einkaufsartikel>().ToListAsync();
    }

    public async Task ArtikelSpeichernAsync(Einkaufsartikel artikel)
    {
        int ergebnis;

        if(artikel.Id == 0)
        {
            ergebnis = await datenbank.InsertAsync(artikel);
        }
        else
        {
            ergebnis = await datenbank.UpdateAsync(artikel);
        }

        System.Diagnostics.Debug.WriteLine(
        $"SQLite-Speichern: ID={artikel.Id}, " +
        $"Menge={artikel.Menge}, Einheit={artikel.Einheit}, " +
        $"Ergebnis={ergebnis}");
    }

    public async Task ArtikelLoeschenAsync(Einkaufsartikel artikel)
    {
        await datenbank.DeleteAsync(artikel);
    }

    public async Task<List<Produkt>> AlleProdukteLadenAsync()
    {
        return await datenbank.Table<Produkt>().ToListAsync();

    }

    public async Task<List<string>> AlleKategorienLadenAsync()
    {
        await InitialisierenAsync();

        List<Produkt> produkte = await datenbank.Table<Produkt>().ToListAsync();

        return produkte
            .Select(p => p.Kategorie)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct()
            .OrderBy(k => k)
            .ToList();
    }

     public async Task<Produkt?> ProduktFindenAsync(string name)
    {
        
        static string Normalisieren(string text)
        {
            return string.Concat(
                text.Where(c => !char.IsWhiteSpace(c))).ToLowerInvariant();
        }
        
        
        string suchname = Normalisieren(name);

        List<Produkt> produkte = 
            await datenbank.Table<Produkt>().ToListAsync();

        // Den vollständigen Namen vergleichen

        Produkt? direktErgebnis = produkte.FirstOrDefault(
            p => Normalisieren(p.Name) == suchname);

        if (direktErgebnis != null)
            return direktErgebnis;

        // Einfache Wortendungen brücksichtigen

        string grundform = suchname;

        if (grundform.EndsWith("en"))
            grundform = grundform[..^2];
        else if (grundform.EndsWith("e"))
            grundform = grundform[..^1];
        else if (grundform.EndsWith("n"))
            grundform = grundform[..^1];

        return produkte.FirstOrDefault(
            p => Normalisieren(p.Name) == grundform);
    }

    public async Task ProduktSpeichernAsync(Produkt produkt)
    {
        await datenbank.InsertOrReplaceAsync(produkt);
    }

    public async Task StandardprodukteAnlegenAsync()
    {
        List<Produkt> standardprodukte = new()
        {
            new Produkt {Name = "Apfel", Kategorie="Obst"},
            new Produkt {Name = "Banane", Kategorie="Obst"},
            new Produkt {Name = "Birne", Kategorie="Obst"},
            new Produkt {Name = "Orange", Kategorie="Obst"},

            new Produkt { Name = "Tomate", Kategorie = "Gemüse" },
            new Produkt { Name = "Gurke", Kategorie = "Gemüse" },
            new Produkt { Name = "Karotte", Kategorie = "Gemüse" },
            new Produkt { Name = "Kartoffel", Kategorie = "Gemüse" },

            new Produkt { Name = "Milch", Kategorie = "Milchprodukte" },
            new Produkt { Name = "Käse", Kategorie = "Milchprodukte" },
            new Produkt { Name = "Joghurt", Kategorie = "Milchprodukte" },
            new Produkt { Name = "Butter", Kategorie = "Milchprodukte" },

            new Produkt { Name = "Brot", Kategorie = "Backwaren" },
            new Produkt { Name = "Brötchen", Kategorie = "Backwaren" },
            new Produkt { Name = "Toast", Kategorie = "Backwaren" },

            new Produkt { Name = "Wasser", Kategorie = "Getränke" },
            new Produkt { Name = "Orangensaft", Kategorie = "Getränke" },
            new Produkt { Name = "Kaffee", Kategorie = "Getränke" },

            new Produkt { Name = "Hähnchen", Kategorie = "Fleisch & Fisch" },
            new Produkt { Name = "Hackfleisch", Kategorie = "Fleisch & Fisch" },
            new Produkt { Name = "Lachs", Kategorie = "Fleisch & Fisch" },

            new Produkt { Name = "Pizza", Kategorie = "Tiefkühlprodukte" },
            new Produkt { Name = "Pommes", Kategorie = "Tiefkühlprodukte" },
            new Produkt { Name = "Eis", Kategorie = "Tiefkühlprodukte" },

            new Produkt { Name = "Schokolade", Kategorie = "Süßigkeiten & Snacks" },
            new Produkt { Name = "Chips", Kategorie = "Süßigkeiten & Snacks" },

            new Produkt { Name = "Spülmittel", Kategorie = "Haushalt" },
            new Produkt { Name = "Waschmittel", Kategorie = "Haushalt" }

        };

        foreach (Produkt produkt in standardprodukte)
        {
            List<Produkt> produkte =
                await datenbank.Table<Produkt>().ToListAsync();

            bool vorhanden = produkte.Any(
                p => string.Equals(
                    p.Name.Trim(),
                    produkt.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (!vorhanden)
            {
                await datenbank.InsertAsync(produkt);

                System.Diagnostics.Debug.WriteLine(
                    $"Produkt gespeichert: {produkt.Name} - {produkt.Kategorie}");
            }
        }
    }

    public async Task<Einkaufsartikel?> ArtikelLadenNachIdAsync(int id)
    {
        return await datenbank
            .Table<Einkaufsartikel>()
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
    }
}
