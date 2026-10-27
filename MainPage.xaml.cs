using System.Collections.ObjectModel;
using Einkaufsliste.Models;
using Einkaufsliste.Services;
using System.Linq;

namespace Einkaufsliste
{
    public partial class MainPage : ContentPage
    {
        private ObservableCollection<Artikelgruppe> artikelGruppen = new();
        private readonly EinkauflistenDatenbank datenbank = new();

        public MainPage()
        {
            InitializeComponent();

            ArtikelListe.ItemsSource = artikelGruppen;

            LadeDatenAsync();
        }

        private async void OnArtikelHinzufuegenClicked(object sender, EventArgs e)
        {
            string neuerArtikel = ArtikelEingabe.Text ?? string.Empty;


            if (string.IsNullOrWhiteSpace(neuerArtikel))
            {
                await DisplayAlertAsync("Hinweis", "Bitte geben Sie einen Artikel ein.", "OK");
                return;
            }
            string artikelName = neuerArtikel.Trim();

            Produkt? produkt = await datenbank.ProduktFindenAsync(artikelName);

            string kategorie;

            if(produkt != null)
            {
                //Bekannstes Produkt: Kategorie automatisch übernehmen
                kategorie = produkt.Kategorie;
            }
            else
            {
                // Unbekanntes Produkt: Kategorien zur Auswahl laden
                List<Kategorie> kategorien =
                    await datenbank.AlleKategorieEintraegeLadenAsync();

                string[] kategorienNamen = kategorien.Select(k => k.Name).ToArray();

                string? ausgewaehlteKategorie = await DisplayActionSheetAsync(
                    $"Welche Kategorie gehört zu {artikelName} ?",
                    "Abbrechen",
                    null,
                    kategorienNamen);

                if(string.IsNullOrWhiteSpace(ausgewaehlteKategorie) || ausgewaehlteKategorie == "Abbrechen")
                {
                    return;
                }
                kategorie = ausgewaehlteKategorie;

                // Produkt für zukünftige Einkäufe speichern
                Produkt neuesProdukt = new()
                {
                    Name = artikelName,
                    Kategorie = kategorie
                };

                await datenbank.ProduktSpeichernAsync(neuesProdukt);
                    
            }

            Einkaufsartikel neuerEintrag = new()
            {
                Name = artikelName,
                Kategorie = kategorie,
                IstErledigt = false
            };
            
            await datenbank.ArtikelSpeichernAsync(neuerEintrag);

            Artikelgruppe? gruppe = artikelGruppen.FirstOrDefault(g => g.Kategorie == kategorie);

            if(gruppe == null)
            {
                gruppe = new Artikelgruppe(kategorie);
                artikelGruppen.Add(gruppe);
            }

            gruppe.Add(neuerEintrag);
            AktualisiereFortschritt();
            ArtikelEingabe.Text = string.Empty;
        }

        private async void OnArtikelLoeschenClicked(object sender, EventArgs e)
        {
            if(sender is Button button && 
                button.CommandParameter is Einkaufsartikel artikelZumLoeschen)
            {
                await datenbank.ArtikelLoeschenAsync(artikelZumLoeschen);

                Artikelgruppe? gruppe = artikelGruppen.FirstOrDefault(g => g.Contains(artikelZumLoeschen));

                if(gruppe != null)
                {
                    gruppe.Remove(artikelZumLoeschen);

                    if (gruppe.Count == 0)
                    {
                        artikelGruppen.Remove(gruppe);
                    }
                }

                AktualisiereFortschritt();
            }
        }

        private async void OnArtikelErledigtGeaendert(
     object sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox checkbox ||
                checkbox.BindingContext is not Einkaufsartikel artikel)
            {
                return;
            }

            await datenbank.ArtikelSpeichernAsync(artikel);

            // Aktuelle Gruppe des Artikels finden
            Artikelgruppe? alteGruppe = artikelGruppen
                .FirstOrDefault(g => g.Contains(artikel));

            if (alteGruppe == null)
            {
                AktualisiereFortschritt();
                return;
            }

            // Artikel aus der bisherigen Gruppe entfernen
            alteGruppe.Remove(artikel);

            // Leere Gruppe entfernen, außer bei der Gruppe "Erledigt"
            if (alteGruppe.Count == 0 &&
                alteGruppe.Kategorie != "Erledigt")
            {
                artikelGruppen.Remove(alteGruppe);
            }

            if (artikel.IstErledigt)
            {
                // Erledigte Artikel in die Gruppe "Erledigt" verschieben
                Artikelgruppe? erledigtGruppe = artikelGruppen
                    .FirstOrDefault(g => g.Kategorie == "Erledigt");

                if (erledigtGruppe == null)
                {
                    erledigtGruppe = new Artikelgruppe("Erledigt");
                    artikelGruppen.Add(erledigtGruppe);
                }

                erledigtGruppe.Add(artikel);
            }
            else
            {
                // Artikel zurück in seine ursprüngliche Kategorie verschieben
                Artikelgruppe? zielGruppe = artikelGruppen
                    .FirstOrDefault(g => g.Kategorie == artikel.Kategorie);

                if (zielGruppe == null)
                {
                    zielGruppe = new Artikelgruppe(artikel.Kategorie);
                    artikelGruppen.Add(zielGruppe);
                }

                zielGruppe.Add(artikel);
            }

            // Leere Erledigt-Gruppe entfernen
            Artikelgruppe? erledigt = artikelGruppen
                .FirstOrDefault(g => g.Kategorie == "Erledigt");

            if (erledigt != null && erledigt.Count == 0)
            {
                artikelGruppen.Remove(erledigt);
            }

            AktualisiereFortschritt();
        }


        private async void LadeDatenAsync()
        {
            await datenbank.InitialisierenAsync();
            await datenbank.StandardkategorienAnlegenAsync();

            

            List<Einkaufsartikel> gespeicherteArtikel = await datenbank.AlleArtikelLadenAsync();
            
            ArtikelGruppieren(gespeicherteArtikel);

            AktualisiereFortschritt();

           
        }

        private async void OnKategorienOeffnenClicked(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new KategorienPage());
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

        private void ArtikelGruppieren(IEnumerable<Einkaufsartikel> alleArtikel)
        {
            artikelGruppen.Clear();
            // Unerledigte Artikel nach ihrer Kategorie gruppieren

            var offeneGruppen = alleArtikel
                .Where(a => !a.IstErledigt)
                .GroupBy(a => a.Kategorie)
                .OrderBy(g => g.Key);

            foreach(var gruppe in offeneGruppen)
            {
                Artikelgruppe neueGruppe = new(gruppe.Key);

                foreach(Einkaufsartikel einzelnerArtikel in gruppe)
                {
                    neueGruppe.Add(einzelnerArtikel);
                }

                artikelGruppen.Add(neueGruppe);
            }

            // Alle erledigten Artikel in einer eigenen Gruppe sammeln

            var erledigteArtikel = alleArtikel
                .Where(a => a.IstErledigt)
                .ToList();

            if(erledigteArtikel.Count > 0)
            {
                Artikelgruppe erledigtGruppe = new("Erledigt");

                foreach (Einkaufsartikel artikel in erledigteArtikel)
                {
                    erledigtGruppe.Add(artikel);
                }

                artikelGruppen.Add(erledigtGruppe);
            }
        
        }
        

    }
}
