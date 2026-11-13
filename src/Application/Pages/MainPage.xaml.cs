using Einkaufsliste.Services;
using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly ObservableCollection<Artikelgruppe> artikelGruppen = new();
        private readonly EinkauflistenDatenbank _datenbank ;
        private readonly System.Threading.SemaphoreSlim _artikelHinzufuegenSperre = new(1, 1);
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
        private async Task LadeDatenAsync()
        {
            alleHaendler = await _datenbank.AlleHaendlerLadenAsync();

            List<Einkaufsartikel> gespeicherteArtikel = await _datenbank.AlleArtikelLadenAsync();
            
            ArtikelGruppieren(gespeicherteArtikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();

           
        }        
     }
}
