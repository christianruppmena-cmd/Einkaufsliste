
using Einkaufsliste.Models;
using SQLite;


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
        // Einkaufslisten
        await datenbank.CreateTableAsync<Einkaufsartikel>();
        await datenbank.CreateTableAsync<Kategorie>();

        // Produktverwaltung
        await datenbank.CreateTableAsync<Produkt>();

        // Preisvergleich
        await datenbank.CreateTableAsync<Haendler>();
        await datenbank.CreateTableAsync<Produktpreis>();
        await datenbank.CreateTableAsync<PreisHistorie>();

        // Verknüpfung zwischen Einkaufsartikel und Produkt
        var spalten = await datenbank.GetTableInfoAsync("Einkaufsartikel");

        if (!spalten.Any(s => s.Name == "ProduktId"))
        {
            await datenbank.ExecuteAsync(
                "ALTER TABLE Einkaufsartikel ADD COLUMN ProduktId INTEGER NULL");
        }
    }


    public async Task StandardHaendlerAnlegenAsync()
    {
        string[] haendlerNamen =
        {
        "ALDI SÜD",
        "Lidl",
        "REWE",
        "EDEKA",
        "Penny",
        "Netto",
        "dm",
        "Rossmann",
        "Kaufland"
    };

        for (int i = 0; i < haendlerNamen.Length; i++)
        {
            string name = haendlerNamen[i];

            var vorhanden = await datenbank
                .Table<Haendler>()
                .Where(h => h.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await datenbank.InsertAsync(new Haendler
                {
                    Name = name,
                    IstAktiv = true,
                    Sortierung = i + 1
                });
            }
        }
    }

    public async Task StandardkategorienAnlegenAsync()
    {
        string[] standardkategorien =
        {
        "Obst",
        "Gemüse",
        "Milchprodukte",
        "Fleisch & Fisch",
        "Backwaren",
        "Getränke",
        "Tiefkühlprodukte",
        "Süßigkeiten & Snacks",
        "Haushalt",
        "Drogerie",
        "Sonstiges"
    };

        foreach (string name in standardkategorien)
        {
            Kategorie? vorhanden = await datenbank
                .Table<Kategorie>()
                .Where(k => k.Name == name)
                .FirstOrDefaultAsync();

            if (vorhanden == null)
            {
                await datenbank.InsertAsync(new Kategorie
                {
                    Name = name
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
    
    public async Task<bool> KategorieUmbenennenAsync(Kategorie kategorie,string neuerName)
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
        if (string.IsNullOrWhiteSpace(produkt.Name))
        {
            throw new ArgumentException("Der Produktname darf nicht leer sein.");
        }

        string Normalisieren(string? wert)
        {
            return string.IsNullOrWhiteSpace(wert)
                ? string.Empty
                : wert.Trim().ToLowerInvariant();
        }

        string name = Normalisieren(produkt.Name);
        string marke = Normalisieren(produkt.Marke);
        string variante = Normalisieren(produkt.Variante);
        string packung = produkt.Packungsgroesse?.ToString(
            "0.################",
            System.Globalization.CultureInfo.InvariantCulture) ?? "";
        string einheit = Normalisieren(produkt.Basiseinheit);

        produkt.ProduktSchluessel =
            string.Join("|", name, marke, variante, packung, einheit);

        var vorhandenesProdukt = await datenbank
            .Table<Produkt>()
            .Where(p => p.ProduktSchluessel == produkt.ProduktSchluessel)
            .FirstOrDefaultAsync();

        if (vorhandenesProdukt != null &&
            vorhandenesProdukt.Id != produkt.Id)
        {
            throw new InvalidOperationException(
                "Dieses Produkt existiert bereits.");
        }

        if (produkt.Id == 0)
        {
            await datenbank.InsertAsync(produkt);
        }
        else
        {
            await datenbank.UpdateAsync(produkt);
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
