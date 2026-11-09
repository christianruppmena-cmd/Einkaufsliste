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
        private Einkaufsartikel? FindeGleichenOffenenArtikel(Einkaufsartikel artikelZumPruefen)
        {
            return artikelGruppen
                .SelectMany(gruppe => gruppe)
                .FirstOrDefault(artikel =>
                    string.Equals(
                        artikel.Name.Trim(),
                        artikelZumPruefen.Name.Trim(),
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        artikel.Einheit,
                        artikelZumPruefen.Einheit,
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        artikel.Kategorie,
                        artikelZumPruefen.Kategorie,
                        StringComparison.OrdinalIgnoreCase)
                    && !artikel.IstErledigt);
        }

        private async Task LadeProdukteFuerSucheAsync()
        {
            alleProdukte = await _datenbank.AlleProdukteLadenAsync();
        }
        private async void OnArtikelHinzufuegenClicked(object sender, EventArgs e)
        {
            var seite = new ArtikelEingabePage(string.Empty);

            await Navigation.PushModalAsync(new NavigationPage(seite));

            Einkaufsartikel neuerEintrag = await seite.ErgebnisTask;

            if (neuerEintrag == null)
                return;

            neuerEintrag.Name = TextHelfer.Normalisieren(neuerEintrag.Name);

            // Zuerst die Kategorie des Produkts ermitteln
            Produkt produkt = await _datenbank.ProduktFindenAsync(neuerEintrag.Name);

            if (produkt != null)
            {
                neuerEintrag.ProduktId = produkt.Id;
                neuerEintrag.Name = produkt.Name;
                neuerEintrag.Kategorie = produkt.Kategorie;
            }
            else
            {
                List<Kategorie> kategorien =
                    await _datenbank.AlleKategorieEintraegeLadenAsync();

                string[] kategorienNamen = kategorien.Select(k => k.Name).ToArray();

                string neueKategorieOption = "+ Neue Kategorie";

                string ausgewaehlteKategorie = await DisplayActionSheetAsync(
                    $"Welche Kategorie gehört zu {neuerEintrag.Name}?",
                    "Abbrechen",
                    null,
                    kategorienNamen.Append(neueKategorieOption).ToArray());

                if (string.IsNullOrWhiteSpace(ausgewaehlteKategorie) ||
                    ausgewaehlteKategorie == "Abbrechen")
                {
                    return;
                }

                if (ausgewaehlteKategorie == neueKategorieOption)
                {
                    string neueKategorie = await DisplayPromptAsync(
                        "Neue Kategorie",
                        "Wie soll die Kategorie heißen? ");

                    if (string.IsNullOrWhiteSpace(neueKategorie))
                        return;

                    neueKategorie = neueKategorie.Trim();

                    bool existiert = kategorien.Any(k =>
                    string.Equals(
                        k.Name.Trim(),
                        neueKategorie,
                        StringComparison.OrdinalIgnoreCase));

                    if (existiert)
                    {
                        await DisplayAlertAsync(
                            "Hinweis",
                            "Diese Kategorie existiert bereits.",
                            "OK");
                        return;
                    }

                    await _datenbank.KategorieSpeichernAsync(
                        new Kategorie { Name = neueKategorie });
                    neuerEintrag.Kategorie = neueKategorie;
                }
                else
                {
                    neuerEintrag.Kategorie = ausgewaehlteKategorie;
                }

                Produkt neuesProdukt = new Produkt
                {
                    Name = TextHelfer.Normalisieren(neuerEintrag.Name),
                    Kategorie = TextHelfer.Normalisieren(neuerEintrag.Kategorie)
                };

                await _datenbank.ProduktSpeichernAsync(neuesProdukt);
                neuerEintrag.ProduktId = neuesProdukt.Id;
            }

            // Erst jetzt nach einem doppelten Artikel suchen
            Einkaufsartikel? vorhandenerArtikel = FindeGleichenOffenenArtikel(neuerEintrag);

            if (vorhandenerArtikel != null)
            {
                bool zusammenfassen = await DisplayAlertAsync(
                    "Artikel bereits vorhanden",
                    $"{vorhandenerArtikel.Name} ist bereits in deiner Einkaufsliste.\n\n" +
                    $"Vorhanden: {vorhandenerArtikel.Menge} {vorhandenerArtikel.Einheit}\n" +
                    $"Neu: {neuerEintrag.Menge} {neuerEintrag.Einheit}\n\n" +
                    "Möchtest du die Mengen zusammenfassen?",
                    "Zusammenfassen",
                    "Separat hinzufügen");

                if (zusammenfassen)
                {
                    if (decimal.TryParse(
                            vorhandenerArtikel.Menge,
                            System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.CurrentCulture,
                            out decimal alteMenge)
                        && decimal.TryParse(
                            neuerEintrag.Menge,
                            System.Globalization.NumberStyles.Number,
                            System.Globalization.CultureInfo.CurrentCulture,
                            out decimal neueMenge))
                    {
                        vorhandenerArtikel.Menge =
                            (alteMenge + neueMenge).ToString(
                                System.Globalization.CultureInfo.CurrentCulture);

                        if (string.IsNullOrWhiteSpace(vorhandenerArtikel.Hinweis))
                        {
                            vorhandenerArtikel.Hinweis = neuerEintrag.Hinweis;
                        }
                        else if (!string.IsNullOrWhiteSpace(neuerEintrag.Hinweis)
                            && !string.Equals(
                                vorhandenerArtikel.Hinweis,
                                neuerEintrag.Hinweis,
                                StringComparison.OrdinalIgnoreCase))
                        {
                            vorhandenerArtikel.Hinweis += " / " + neuerEintrag.Hinweis;
                        }

                        await _datenbank.ArtikelSpeichernAsync(vorhandenerArtikel);
                        AktualisiereFortschritt();
                        return;
                    }

                    await DisplayAlertAsync(
                        "Menge ungültig",
                        "Die Mengen konnten nicht addiert werden. Bitte prüfe die Angaben.",
                        "OK");
                    return;
                }
            }

            await _datenbank.ArtikelSpeichernAsync(neuerEintrag);

            // Einkaufsliste entsprechend der gewählten Gruppierung aktualisieren
            List<Einkaufsartikel> alleArtikel = await _datenbank.AlleArtikelLadenAsync();
                
            ArtikelGruppieren(alleArtikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();
        }

        private async void OnProduktNeuAnlegenClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(
                new ProduktVerwaltungPage(_datenbank, true));
        }
        private async void OnArtikelLoeschenClicked(object sender, EventArgs e)
        {
            if(sender is Button button && 
                button.CommandParameter is Einkaufsartikel artikelZumLoeschen)
            {
                await _datenbank.ArtikelLoeschenAsync(artikelZumLoeschen);

                Artikelgruppe gruppe = artikelGruppen.FirstOrDefault(g => g.Contains(artikelZumLoeschen));

                if(gruppe != null)
                {
                    gruppe.Remove(artikelZumLoeschen);

                    if (gruppe.Count == 0)
                    {
                        artikelGruppen.Remove(gruppe);
                    }
                }

                AktualisiereFortschritt();
                await GesamtsummeAktualisierenAsync();
            }
        }

        private async void OnArtikelErledigtGeaendert(object sender,CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox checkbox ||
                checkbox.BindingContext is not Einkaufsartikel artikel)
            {
                return;
            }

            await _datenbank.ArtikelSpeichernAsync(artikel);

            Artikelgruppe alteGruppe = artikelGruppen
                .FirstOrDefault(g => g.Contains(artikel));

            if (alteGruppe == null)
                return;

            alteGruppe.Remove(artikel);

            if (alteGruppe.Count == 0 )
            {
                artikelGruppen.Remove(alteGruppe);
            }

            string zielGruppenName;

            if (artikel.IstErledigt)
            {
                zielGruppenName = "Erledigt";
            }
            else if (aktuelleGruppierung == "Kaufort")
            {
                zielGruppenName = alleHaendler
                    .FirstOrDefault(h => h.Id == artikel.AusgewaehlterHaendlerId)
                    ?.Name ?? "Ohne festgelegten Kaufort";
            }
            else
            {
                zielGruppenName = artikel.Kategorie;
            }

            Artikelgruppe zielGruppe = artikelGruppen
                .FirstOrDefault(g => g.Kategorie == zielGruppenName);

            if (zielGruppe == null)
            {
                zielGruppe = new Artikelgruppe(zielGruppenName);
                artikelGruppen.Add(zielGruppe);
            }

            zielGruppe.Add(artikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();
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
        private async Task ArtikeldetailsBearbeitenAsync(Einkaufsartikel artikel)
        {
            string menge = await DisplayPromptAsync(
                "Menge Bearbeiten",
                $"Menge für {artikel.Name} :",
                initialValue: artikel.Menge,
                keyboard: Keyboard.Default);

            if (menge == null)
                return;

            menge = menge.Trim();

            if (string.IsNullOrWhiteSpace(menge))
                {
                    await DisplayAlertAsync(
                        "Ungültige Menge",
                        "Bitte gib eine Menge ein.",
                        "OK");
                    return;
                }

            string[] einheiten =
            {
                "Stück","Packung", "Flasche", "Kg", "g", "Liter", "ml"
            };

            string einheit = await DisplayActionSheetAsync(
                "Einheit auswählen",
                "Abbrechen",
                null,
                einheiten);

            if (string.IsNullOrWhiteSpace(einheit) || einheit == "Abbrechen")
            { 
                return;
            }
            string hinweis = await DisplayPromptAsync(
                "Hinweis bearbeiten",
                "Zusätzliche Angaben, z.B. TK oder Vollkorn: ",
                initialValue: artikel.Hinweis);

            if (hinweis == null)
                return;

            artikel.Menge = menge;
            artikel.Einheit = einheit;
            artikel.Hinweis = hinweis.Trim();
        }
        private async void OnArtikelDetailsAnzeigenClicked(object sender, TappedEventArgs e)
        {
            if (sender is Label label &&
                label.BindingContext is Einkaufsartikel artikel)
            {
                await ArtikeldetailsBearbeitenAsync(artikel);

                // Liste aktualisieren , damit die Details sichtbar werden
                ArtikelListe.ItemsSource = null;
                ArtikelListe.ItemsSource = artikelGruppen;
            }
        }
        private async void OnArtikelBearbeitenClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.BindingContext is not Einkaufsartikel artikel)
            {
                return;
            }

            var seite = new ArtikeldetailsPage(artikel);

            await Navigation.PushModalAsync(new NavigationPage(seite));

            bool gespeichert = await seite.ErgebnisTask;

            if (!gespeichert)
                return;

            await _datenbank.ArtikelSpeichernAsync(artikel);

            // Aktuelle Gruppe des Artikels finden
            Artikelgruppe alteGruppe = artikelGruppen
                .FirstOrDefault(g => g.Contains(artikel));

            if (alteGruppe != null)
            {
                string neueGruppenBezeichnung;

                if (artikel.IstErledigt)
                {
                    neueGruppenBezeichnung = "Erledigt";
                }
                else if (aktuelleGruppierung == "Kaufort")
                {
                    neueGruppenBezeichnung = alleHaendler
                        .FirstOrDefault(h => h.Id == artikel.AusgewaehlterHaendlerId)
                        ?.Name ?? "Ohne festgelegten Kaufort";
                }
                else
                {
                    neueGruppenBezeichnung = artikel.Kategorie;
                }

                // Nur verschieben, wenn sich die Gruppe geändert hat
                if (alteGruppe.Kategorie != neueGruppenBezeichnung)
                {
                    alteGruppe.Remove(artikel);

                    if (alteGruppe.Count == 0 &&
                        alteGruppe.Kategorie != "Erledigt")
                    {
                        artikelGruppen.Remove(alteGruppe);
                    }

                    Artikelgruppe neueGruppe = artikelGruppen
                        .FirstOrDefault(g => g.Kategorie == neueGruppenBezeichnung);

                    if (neueGruppe == null)
                    {
                        neueGruppe = new Artikelgruppe(neueGruppenBezeichnung);
                        artikelGruppen.Add(neueGruppe);
                    }

                    neueGruppe.Add(artikel);
                }
            }

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();
        }
        private async void OnMengeErhoehenClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not Einkaufsartikel artikel)
                return;

            if (!decimal.TryParse(
                artikel.Menge,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
            {
                menge = 1;
            }

            menge++;

            artikel.Menge = menge.ToString(
                System.Globalization.CultureInfo.CurrentCulture);

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnMengeVerringernClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not Einkaufsartikel artikel)
                return;

            if (!decimal.TryParse(
                artikel.Menge,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
            {
                menge = 1;
            }

            if (menge <= 1)
                return;

            menge--;

            artikel.Menge = menge.ToString(
                System.Globalization.CultureInfo.CurrentCulture);

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnMengeGeaendert(object sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry ||
                entry.BindingContext is not Einkaufsartikel artikel)
                return;

            string eingabe = e.NewTextValue?.Trim() ?? string.Empty;

            // Während der Eingabe leere oder ungültige Werte ignorieren
            if (!decimal.TryParse(
                eingabe,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
                return;

            if (menge <= 0)
                return;

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnEinheitGeaendert(object sender, EventArgs e)
        {
            if (sender is not Picker picker ||
                picker.BindingContext is not Einkaufsartikel artikel)
                return;

            string neueEinheit = picker.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(neueEinheit))
                return;

            artikel.Einheit = neueEinheit;

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
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
    }
}
