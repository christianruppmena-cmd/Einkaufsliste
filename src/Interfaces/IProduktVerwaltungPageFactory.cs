using Einkaufsliste.Application.Pages.Produkte;

namespace Einkaufsliste.Interfaces
{
    public interface IProduktVerwaltungPageFactory
    {
        // Übergibt ob die Seite direkt das Formular für ein neues Produkt zeigt.
        ProduktVerwaltungPage Erstellen(bool nurNeuesProduktFormular);
    }
}
