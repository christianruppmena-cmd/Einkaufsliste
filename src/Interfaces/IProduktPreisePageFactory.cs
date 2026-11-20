using Einkaufsliste.Application.Pages.Produkte;
using Einkaufsliste.Models;

namespace Einkaufsliste.Interfaces
{
    public interface IProduktPreisePageFactory
    {
        // Es wird beim Erzeugen übergeben 
        ProduktpreisePage Erstellen(Produkt produkt);
    }
}
