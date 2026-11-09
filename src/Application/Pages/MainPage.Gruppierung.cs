using Einkaufsliste.Services;
using Einkaufsliste.Models;
using Einkaufsliste.Utilities;
using Einkaufsliste.Application.Pages.Artikel;
using Einkaufsliste.Application.Pages.Kategorien;
using Einkaufsliste.Application.Pages.Produkte;

namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage
    {
        private void AktualisiereFortschritt()
        {
            int gesamt = 0;
            int erledigt = 0;

            foreach (Artikelgruppe gruppe in artikelGruppen)
            {
                gesamt += gruppe.Count;

                erledigt += gruppe.Count(a => a.IstErledigt);
            }

            ArtikelAnzahlText.Text = $"{gesamt} Artikel insgesamt";

            FortschrittText.Text = $"{erledigt} von {gesamt} erledigt";

            FortschrittBalken.Progress = gesamt == 0
                ? 0
                : (double)erledigt / gesamt;
        }
        private async Task GesamtsummeAktualisierenAsync()
        {
            List<Einkaufsartikel> alleArtikel =
                await _datenbank.AlleArtikelLadenAsync();

            decimal gesamtsumme = alleArtikel.Sum(a => a.Gesamtpreis);

            GesamtsummeText.Text = $"Gesamtsumme : {gesamtsumme:N2} €";

        }
        private void ArtikelGruppieren(IEnumerable<Einkaufsartikel> alleArtikel)
        {
            artikelGruppen.Clear();

            // Händlernamen für die Artikelanzeige ermitteln
            foreach (Einkaufsartikel artikel in alleArtikel)
            {
                Haendler haendler = alleHaendler.FirstOrDefault(
                    h => h.Id == artikel.AusgewaehlterHaendlerId);

                artikel.HaendlerAnzeige = haendler?.Name ?? string.Empty;
            }


            // Nur offene Artikel berücksichtigen
            var offeneArtikel = alleArtikel
                .Where(a => !a.IstErledigt)
                .ToList();

            if (aktuelleGruppierung == "Kaufort")
            {
                // Artikel nach Händler gruppieren
                var offeneGruppen = offeneArtikel
                    .GroupBy(a =>
                        alleHaendler
                            .FirstOrDefault(h => h.Id == a.AusgewaehlterHaendlerId)
                            ?.Name ?? "Ohne festgelegten Kaufort")
                    .OrderBy(g =>
                        alleHaendler
                            .FirstOrDefault(h => h.Name == g.Key)
                            ?.Sortierung ?? int.MaxValue)
                    .ThenBy(g => g.Key);

                foreach (var gruppe in offeneGruppen)
                {
                    Artikelgruppe neueGruppe = new(gruppe.Key);

                    foreach (Einkaufsartikel artikel in gruppe)
                    {
                        neueGruppe.Add(artikel);
                    }

                    artikelGruppen.Add(neueGruppe);
                }
            }
            else
            {
                // Artikel nach Produktkategorie gruppieren
                var offeneGruppen = offeneArtikel
                    .GroupBy(a => a.Kategorie)
                    .OrderBy(g => g.Key);

                foreach (var gruppe in offeneGruppen)
                {
                    Artikelgruppe neueGruppe = new(gruppe.Key);

                    foreach (Einkaufsartikel artikel in gruppe)
                    {
                        neueGruppe.Add(artikel);
                    }

                    artikelGruppen.Add(neueGruppe);
                }
            }

            // Erledigte Artikel bleiben immer in einer eigenen Gruppe
            var erledigteArtikel = alleArtikel
                .Where(a => a.IstErledigt)
                .ToList();

            if (erledigteArtikel.Count > 0)
            {
                Artikelgruppe erledigtGruppe = new("Erledigt");

                foreach (Einkaufsartikel artikel in erledigteArtikel)
                {
                    erledigtGruppe.Add(artikel);
                }

                artikelGruppen.Add(erledigtGruppe);
            }
        }
        private async void OnGruppierungGeaendert(object sender, EventArgs e)
        {
            if (GruppierungsPicker.SelectedItem is not string neueGruppierung)
                return;

            if (neueGruppierung != "Produktkategorie" &&
                neueGruppierung != "Kaufort")
                return;

            aktuelleGruppierung = neueGruppierung;

            Preferences.Set(
                GruppierungsEinstellung,
                aktuelleGruppierung);

            List<Einkaufsartikel> alleArtikel =
                await _datenbank.AlleArtikelLadenAsync();

            ArtikelGruppieren(alleArtikel);

            AktualisiereFortschritt();
        }
    }
}
