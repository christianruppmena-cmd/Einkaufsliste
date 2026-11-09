using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services
{
    public partial class EinkauflistenDatenbank
    {
        public async Task<List<Produkt>> AlleProdukteLadenAsync()
        {
            return await _datenbank.Table<Produkt>().ToListAsync();

        }
        public async Task<List<string>> AlleKategorienLadenAsync()
        {
            await InitialisierenAsync();

            List<Produkt> produkte = await _datenbank.Table<Produkt>().ToListAsync();

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
                await _datenbank.Table<Produkt>().ToListAsync();

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

            produkt.ProduktSchluessel = ProduktSchluesselErstellen(produkt);

            var vorhandenesProdukt = await _datenbank
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
                await _datenbank.InsertAsync(produkt);
            }
            else
            {
                await _datenbank.UpdateAsync(produkt);
            }
            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task ProduktZusammenfuehrenAsync(int quellProduktId, int zielProduktId)
        {
            if (quellProduktId == zielProduktId)
            {
                throw new ArgumentException(
                    "Quell- und Zielprodukt müssen verschieden sein.");
            }

            await InitialisierenAsync();

            await _datenbank.RunInTransactionAsync(verbindung =>
            {
                Produkt quellProdukt =
                    verbindung.Find<Produkt>(quellProduktId);

                Produkt zielProdukt =
                    verbindung.Find<Produkt>(zielProduktId);

                if (quellProdukt == null || zielProdukt == null)
                {
                    throw new InvalidOperationException(
                        "Quell- oder Zielprodukt wurde nicht gefunden.");
                }

                foreach (Einkaufsartikel artikel in verbindung
                    .Table<Einkaufsartikel>()
                    .Where(a => a.ProduktId == quellProduktId)
                    .ToList())
                {
                    artikel.ProduktId = zielProduktId;
                    artikel.Name = zielProdukt.Name;
                    artikel.Kategorie = zielProdukt.Kategorie;
                    verbindung.Update(artikel);
                }

                foreach (Produktpreis preis in verbindung
                    .Table<Produktpreis>()
                    .Where(p => p.ProduktId == quellProduktId)
                    .ToList())
                {
                    preis.ProduktId = zielProduktId;
                    verbindung.Update(preis);
                }

                foreach (PreisHistorie historie in verbindung
                    .Table<PreisHistorie>()
                    .Where(h => h.ProduktId == quellProduktId)
                    .ToList())
                {
                    historie.ProduktId = zielProduktId;
                    verbindung.Update(historie);
                }

                zielProdukt.IstAktiv = true;
                verbindung.Update(zielProdukt);

                quellProdukt.IstAktiv = false;
                verbindung.Update(quellProdukt);
            });

            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task<Produkt?> ProduktMitGleichemSchluesselLadenAsync(Produkt produkt)
        {
            string schluessel = ProduktSchluesselErstellen(produkt);

            return await _datenbank
                .Table<Produkt>()
                .Where(p =>
                    p.Id != produkt.Id &&
                    p.ProduktSchluessel == schluessel)
                .FirstOrDefaultAsync();
        }
        private static string ProduktSchluesselErstellen(Produkt produkt)
        {
            static string Normalisieren(string? wert)
            {
                return string.IsNullOrWhiteSpace(wert)
                    ? string.Empty
                    : wert.Trim().ToLowerInvariant();
            }

            string name = Normalisieren(produkt.Name);
            string marke = Normalisieren(produkt.Marke);
            string variante = Normalisieren(produkt.Variante);
            string packung = produkt.Packungsgroesse.ToString(
                "0.################",
                System.Globalization.CultureInfo.InvariantCulture);
            string einheit = Normalisieren(produkt.Basiseinheit);

            return string.Join("|", name, marke, variante, packung, einheit);
        }
    }
}
