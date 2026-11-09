using Einkaufsliste.Services;
using Einkaufsliste.Models;
using Einkaufsliste.Utilities;
using Einkaufsliste.Application.Pages.Artikel;
using Einkaufsliste.Application.Pages.Kategorien;
using Einkaufsliste.Application.Pages.Produkte;


namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly ObservableCollection<Artikelgruppe> artikelGruppen = new();
        private readonly EinkauflistenDatenbank _datenbank ;
        private const string GruppierungsEinstellung = "Gruppierungsart";
        private string aktuelleGruppierung = "Produktkategorie";
        private List<Produkt> alleProdukte = new();
        private List<Haendler> alleHaendler = new();

        public MainPage(EinkauflistenDatenbank datenbank)
        {
            InitializeComponent();

            _datenbank = datenbank;

            ArtikelListe.ItemsSource = artikelGruppen;

            aktuelleGruppierung = Preferences.Get(
                GruppierungsEinstellung,
                "Produktkategorie");

            GruppierungsPicker.SelectedItem = aktuelleGruppierung;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LadeDatenAsync();
            await LadeProdukteFuerSucheAsync();

        }
        

        private async Task LadeProdukteFuerSucheAsync()
        {
            alleProdukte = await _datenbank.AlleProdukteLadenAsync();
        }
        

        private async void OnProduktNeuAnlegenClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new ProduktVerwaltungPage(_datenbank, true));
        }
        

        
        private async Task LadeDatenAsync()
        {
            alleHaendler = await _datenbank.AlleHaendlerLadenAsync();

            List<Einkaufsartikel> gespeicherteArtikel = await _datenbank.AlleArtikelLadenAsync();
            
            ArtikelGruppieren(gespeicherteArtikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();

           
        }
        private async void OnKategorienOeffnenClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new KategorienPage(_datenbank));
        }
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
        private async void OnProduktVorschlagAusgewaehlt(object sender,SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is not Produkt produkt)
                return;

            if(produkt == null)
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

            if (await VorhandenenArtikelBehandelnAsync(neuerArtikel))
                return;
            await _datenbank.ArtikelSpeichernAsync(neuerArtikel);

            // Passende Gruppe ermitteln
            string gruppenName;

            if (aktuelleGruppierung == "Kaufort")
            {
                gruppenName = alleHaendler
                    .FirstOrDefault(h => h.Id == neuerArtikel.AusgewaehlterHaendlerId)
                    ?.Name ?? "Ohne festgelegten Kaufort";
            }
            else
            {
                gruppenName = neuerArtikel.Kategorie;
            }

            Artikelgruppe gruppe = artikelGruppen
                .FirstOrDefault(g => g.Kategorie == gruppenName);

            if (gruppe == null)
            {
                gruppe = new Artikelgruppe(gruppenName);
                artikelGruppen.Add(gruppe);
            }

            gruppe.Add(neuerArtikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();
        }
        private async Task<string?> KategorieFuerNeuesProduktAuswaehlenAsync(string produktName)
        {
            List<Kategorie> kategorien =
                await _datenbank.AlleKategorieEintraegeLadenAsync();

            string[] kategorienNamen =
                kategorien.Select(k => k.Name).ToArray();

            const string neueKategorieOption = "+ Neue Kategorie";

            string? ausgewaehlteKategorie = await DisplayActionSheetAsync(
                $"Welche Kategorie gehört zu {produktName}?",
                "Abbrechen",
                null,
                kategorienNamen.Append(neueKategorieOption).ToArray());

            if (string.IsNullOrWhiteSpace(ausgewaehlteKategorie) ||
                ausgewaehlteKategorie == "Abbrechen")
            {
                return null;
            }

            if (ausgewaehlteKategorie != neueKategorieOption)
                return ausgewaehlteKategorie;

            string? neueKategorie = await DisplayPromptAsync(
                "Neue Kategorie",
                "Wie soll die Kategorie heißen?");

            if (string.IsNullOrWhiteSpace(neueKategorie))
                return null;

            neueKategorie = neueKategorie.Trim();

            bool existiertBereits = kategorien.Any(k =>
                string.Equals(
                    k.Name.Trim(),
                    neueKategorie,
                    StringComparison.OrdinalIgnoreCase));

            if (existiertBereits)
            {
                await DisplayAlertAsync(
                    "Hinweis",
                    "Diese Kategorie existiert bereits.",
                    "OK");

                return null;
            }

            await _datenbank.KategorieSpeichernAsync(
                new Kategorie { Name = neueKategorie });

            return neueKategorie;
        }
        
        
    }
}
