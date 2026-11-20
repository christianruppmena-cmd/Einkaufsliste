
using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application.Pages.Produkte;
// Erstellt Preisseiten und versorgt sie mit den benötigten Services.
public class ProduktPreisePageFatcory : IProduktPreisePageFactory
{
    private readonly IHaendlerService _haendlerService;
    private readonly IPreisService _preisService;
    private readonly IArtikelService _artikelService;
    
    // DI übergibt die Services.
    public ProduktPreisePageFatcory(IHaendlerService haendlerService, IPreisService preisService, IArtikelService artikelService)
    {
        _haendlerService = haendlerService;
        _preisService = preisService;
        _artikelService = artikelService;
    }
    // Das ausgewählte Produkt wir zur Laufzeit an die Preisseite übergeben.
    public ProduktpreisePage Erstellen(Produkt produkt)
    {
        return new ProduktpreisePage(produkt, _haendlerService,_preisService,_artikelService);
    }

}
