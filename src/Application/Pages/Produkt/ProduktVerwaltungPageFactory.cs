using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application.Pages.Produkte;

public class ProduktVerwaltungPageFactory : IProduktVerwaltungPageFactory
{
    private readonly IProdukteService _produkteService;
    private readonly IKategorienService _kategorienService;
    private readonly IHaendlerService _haendlerService;
    private readonly IArtikelService _artikelService;
    private readonly IProduktPreisePageFactory _produktPreisePageFactory;

    public ProduktVerwaltungPageFactory(
        IProdukteService produkteService, IKategorienService kategorienService, IHaendlerService haendlerService, IArtikelService artikelService, IProduktPreisePageFactory produktPreisePageFactory )
    {
        _produkteService = produkteService;
        _kategorienService = kategorienService;
        _haendlerService = haendlerService;
        _artikelService = artikelService;
        _produktPreisePageFactory = produktPreisePageFactory;
    }

    public ProduktVerwaltungPage Erstellen(bool nurNeuesProduktFormular)
    {
        return new ProduktVerwaltungPage(
            _produkteService,
            _kategorienService,
            _haendlerService,
            _artikelService,
            _produktPreisePageFactory,
            nurNeuesProduktFormular);
    }
}
