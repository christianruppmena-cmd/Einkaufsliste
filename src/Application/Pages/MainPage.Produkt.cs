using Einkaufsliste.Models;
using Einkaufsliste.Application.Pages.Produkte;

namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage
    {
        private async Task LadeProdukteFuerSucheAsync()
        {
            alleProdukte = await _datenbank.AlleProdukteLadenAsync();
        }
        private async void OnProduktNeuAnlegenClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new ProduktVerwaltungPage(_datenbank, true));
        }
        private void OnProduktsucheTextChanged(object sender, TextChangedEventArgs e)
        {
            string suchtext = e.NewTextValue?.Trim() ?? string.Empty;

            if (suchtext.Length == 0)
            {
                ProduktVorschlaegeContainer.IsVisible = false;
                ProduktVorschlaegeListe.ItemsSource = null;
                return;
            }

            var treffer = alleProdukte
                .Where(p =>
                    p.IstAktiv &&
                    (
                        p.Name.Contains(suchtext, StringComparison.OrdinalIgnoreCase) ||
                        p.Marke.Contains(suchtext, StringComparison.OrdinalIgnoreCase) ||
                        p.Variante.Contains(suchtext, StringComparison.OrdinalIgnoreCase)
                    ))
                .OrderBy(p => p.Name)
                .Take(15)
                .ToList();

            ProduktVorschlaegeListe.ItemsSource = treffer;

            ProduktVorschlaegeContainer.IsVisible = treffer.Count > 0;
        }
        private async void OnProduktVorschlagAusgewaehlt(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not Produkt produkt)
                return;

            if (produkt == null)
                return;

            ProduktVorschlaegeListe.SelectedItem = null;
            ProduktVorschlaegeContainer.IsVisible = false;
            ProduktsucheEntry.Text = string.Empty;


            // Aktuellen Preis beim bevorzugten Händler laden
            Produktpreis? aktuellerPreis = null;

            if (produkt.BevorzugterHaendlerId > 0)
            {
                aktuellerPreis = await _datenbank!.GuenstigstenAktuellenPreisLadenAsync(
                    produkt.Id,
                    produkt.BevorzugterHaendlerId);
            }

            // Einkaufsartikel mit Produktdaten erstellen
            Einkaufsartikel neuerArtikel = new()
            {
                ProduktId = produkt.Id,
                Name = produkt.Name,
                Kategorie = produkt.Kategorie,
                Menge = "1",

                Einheit = string.IsNullOrWhiteSpace(produkt.Basiseinheit)
                    ? "Stück"
                    : produkt.Basiseinheit,

                AusgewaehlterHaendlerId = produkt.BevorzugterHaendlerId,

                HaendlerAnzeige = alleHaendler.FirstOrDefault(h => h.Id == produkt.BevorzugterHaendlerId)?.Name ?? string.Empty,

                Stueckpreis = aktuellerPreis?.PreisEuro ?? 0m,

                IstErledigt = false
            };

            await ArtikelHinzufuegenOderZusammenfuehrenAsync(neuerArtikel);

        }
    }
}
