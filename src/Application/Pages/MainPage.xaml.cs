using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly IHaendlerService _haendlerService;
        private readonly IArtikelService _artikelService;
        private readonly IProdukteService _produkteService;
        private readonly IPreisService _preisService;

        private readonly ObservableCollection<Artikelgruppe> artikelGruppen = new();
        private readonly System.Threading.SemaphoreSlim _artikelHinzufuegenSperre = new(1, 1);
        private const string GruppierungsEinstellung = "Gruppierungsart";
        private string aktuelleGruppierung = "Produktkategorie";
        private List<Produkt> alleProdukte = new();
        private List<Haendler> alleHaendler = new();        
        public MainPage()
        {
            InitializeComponent();

            _haendlerService = Handler!.MauiContext.Services!.GetService<IHaendlerService>();
            _artikelService = Handler!.MauiContext.Services!.GetService<IArtikelService>();           
            _produkteService = Handler!.MauiContext.Services!.GetService<IProdukteService>(); 
            _preisService = Handler!.MauiContext.Services!.GetService<IPreisService>(); 

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
            alleHaendler = await _haendlerService.AlleHaendlerLadenAsync();

            List<Einkaufsartikel> gespeicherteArtikel = await _artikelService.AlleArtikelLadenAsync();
            
            ArtikelGruppieren(gespeicherteArtikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();

           
        }        
     }
}
