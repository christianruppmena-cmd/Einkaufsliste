using Einkaufsliste.Models;

namespace Einkaufsliste.Services;

public class PreisvergleichService
{
    private readonly EinkauflistenDatenbank datenbank;
    public PreisvergleichService(EinkauflistenDatenbank datenbank)
    {
        this.datenbank = datenbank;
    }
    public async Task<PreisvergleichErgebnis> VergleichenAsync(List<Einkaufsartikel> artikel, int filialeId)
    {
        List<Produkt> produkte =
            await datenbank.AlleProdukteLadenAsync();

        List<Produktpreis> preise =
            await datenbank.ProduktpreiseFuerFilialeLadenAsync(filialeId);

        PreisvergleichErgebnis ergebnis = new();

        foreach (Einkaufsartikel eintrag in artikel)
        {
            if (eintrag.IstErledigt)
                continue;

            Produkt? produkt = produkte.FirstOrDefault(p =>
                string.Equals(
                    p.Name.Trim(),
                    eintrag.Name.Trim(),
                    StringComparison.OrdinalIgnoreCase));

            if (produkt == null)
            {
                ergebnis.ArtikelOhnePreis.Add(eintrag.Name);
                continue;
            }

            DateTime heute = DateTime.Today;

            Produktpreis? preis = preise
                .Where(p => p.ProduktId == produkt.Id)
                .Where(p =>
                    p.GueltigVon == null || p.GueltigVon.Value.Date <= heute)
                .Where(p =>
                    p.GueltigBis == null || p.GueltigBis.Value.Date >= heute)
                .Where(p => p.ZuordnunBestaetigt)
                .OrderBy(p =>
                    p.Preisart == "Wochenangebot" ? 0 : 1)
                .ThenBy(p => p.Preis)
                .ThenByDescending(p => p.ErfasstAm)
                .FirstOrDefault();

            if (preis == null)
            {
                ergebnis.ArtikelOhnePreis.Add(eintrag.Name);
                continue;
            }
            decimal menge = 1;

            if (!decimal.TryParse(
                eintrag.Menge.Trim().Replace(',','.'),
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.InvariantCulture,
                out menge))
            {
                menge = 1;
            }

            decimal artikelkosten = menge * preis.Preis;

            ergebnis.Gesamtsumme += artikelkosten;
            ergebnis.ArtikelMitPreis++;
        }
        return ergebnis;
    }
    

}

public class PreisvergleichErgebnis
{
    public decimal Gesamtsumme { get; set; }
    public int ArtikelMitPreis { get; set; }
    public List<string> ArtikelOhnePreis { get; set; } = new();
}