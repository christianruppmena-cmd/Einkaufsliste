using Einkaufsliste.Application.Pages;
using Einkaufsliste.Application.Pages.Produkte;
using Einkaufsliste.Application.Pages.Datenbank;
using Einkaufsliste.Application.Pages.Kategorien;

namespace Einkaufsliste.Application
{
    public partial class AppShell : Shell
    {
        private readonly ShellContent startInhalt;
        private readonly MainPage _mainPage;
        private readonly ProduktVerwaltungPage _produktVerwaltungPage;
        private readonly KategorienPage _kategorienPage;
        private readonly DatenbanksicherungPage _datenbanksicherungPage;
        private readonly HaendlerPage _haendlerPage; 
        public AppShell(MainPage mainPage, ProduktVerwaltungPage produktVerwaltungPage, KategorienPage kategorienPage, DatenbanksicherungPage datenbanksicherungPage, HaendlerPage haendlerPage)
        {
            InitializeComponent();
            _mainPage = mainPage;
            _produktVerwaltungPage = produktVerwaltungPage;
            _kategorienPage = kategorienPage;
            _datenbanksicherungPage = datenbanksicherungPage;
            _haendlerPage = haendlerPage;

            // Einkaufsliste
            var einkaufsliste = new ShellContent
            {
                Title = "🛒 Einkaufsliste",
                Route = "MainPage",
                Content = new ContentPage
                {
                    Content = new ActivityIndicator
                    {
                        IsRunning = true,
                        VerticalOptions = LayoutOptions.Center,
                        HorizontalOptions = LayoutOptions.Center
                    }
                }
            };

            // Produktverwaltung
            var produktverwaltung = new ShellContent
            {
                Title = "📦 Produktverwaltung",
                Route = "ProduktVerwaltungPage",
                Content = _produktVerwaltungPage
            };

            // Kategorienverwaltung
            var kategorien = new ShellContent
            {
                Title = "🏷️ Kategorien",
                Route = "KategorienPage",
                Content = _kategorienPage
            };

            // Datensicherung
            var datensicherung = new ShellContent
            {
                Title = "💾 Datensicherung",
                Route = "DatenbanksicherungPage",
                Content = _datenbanksicherungPage
            };

            // Eigene Menüeinträge
            var einkaufslisteMenue = new FlyoutItem
            {
                Title = "🛒 Einkaufsliste",
                Items =
                {
                    new ShellSection
                    {
                        Items = { einkaufsliste }
                    }
                }
            };

            var produktverwaltungMenue = new FlyoutItem
            {
                Title = "📦 Produktverwaltung",
                Items =
                {
                    new ShellSection
                    {
                        Items = { produktverwaltung }
                    }
                }
            };

            // Händlerverwaltung (in Arbeit)
            var haendlerMenue = new FlyoutItem
            {
                Title = "🏪 Händlerverwaltung",
                Items =
                {
                    new ShellSection
                    {
                        Items =
                        {
                            new ShellContent
                            {
                                Title = "Händlerverwaltung",
                                Route = "HaendlerPage",
                                Content = _haendlerPage
                            }
                        }
                    }
                }
            };

            // Kategorien
            var kategorienMenue = new FlyoutItem
            {
                Title = "🏷️ Kategorien",
                Items =
                {
                    new ShellSection
                    {
                        Items = { kategorien }
                    }
                }
            };

            Items.Add(einkaufslisteMenue);
            Items.Add(produktverwaltungMenue);
            Items.Add(kategorienMenue);
            Items.Add(haendlerMenue);
            Items.Add(new FlyoutItem{
                Title = "💾 Datensicherung",
                Items =
                {
                    new ShellSection
                    {
                        Items = { datensicherung }
                    }
                }
            });

            startInhalt = einkaufsliste;
        }
        public void StartseiteAnzeigen()
        {
            startInhalt.Content = _mainPage;
        }
    }
}