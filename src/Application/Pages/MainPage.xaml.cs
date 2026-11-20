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
        private readonly IKategorienService _kategorienService;
        private readonly IProduktVerwaltungPageFactory _produktVerwaltungPageFactory;

        private readonly ObservableCollection<Artikelgruppe> artikelGruppen = new();
        private readonly System.Threading.SemaphoreSlim _artikelHinzufuegenSperre = new(1, 1);
        private const string GruppierungsEinstellung = "Gruppierungsart";
        private string aktuelleGruppierung = "Produktkategorie";
        private List<Produkt> alleProdukte = new();
        private List<Haendler> alleHaendler = new();        
        public MainPage(IHaendlerService haendlerService, IArtikelService artikelService, IProdukteService produkteService, IPreisService preisService, IKategorienService kategorienService, IProduktVerwaltungPageFactory produktVerwaltungPageFactory)
        {
            InitializeComponent();

            _haendlerService = haendlerService;
            _artikelService = artikelService;           
            _produkteService = produkteService; 
            _preisService = preisService;
            _kategorienService = kategorienService;
            _produktVerwaltungPageFactory = produktVerwaltungPageFactory;
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
