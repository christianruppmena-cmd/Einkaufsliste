using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application.Pages.Produkte;

public class ProduktVerwaltungPageFactory : IProduktVerwaltungPageFactory
{
    private readonly IProdukteService _produkteService;
    private readonly IKategorienService _kategorienService;
    private readonly IHaendlerService _haendlerService;
    private readonly IArtikelService _artikelService;
    private readonly IPreisService _preisService;

    public ProduktVerwaltungPageFactory(
        IProdukteService produkteService, IKategorienService kategorienService, IHaendlerService haendlerService, IArtikelService artikelService, IPreisService preisService )
    {
        _produkteService = produkteService;
        _kategorienService = kategorienService;
        _haendlerService = haendlerService;
        _artikelService = artikelService;
        _preisService = preisService;
    }

    public ProduktVerwaltungPage Erstellen(bool nurNeuesProduktFormular)
    {
        return new ProduktVerwaltungPage(
            _produkteService,
            _kategorienService,
            _haendlerService,
            _artikelService,
            _preisService,
            nurNeuesProduktFormular);
    }
}
