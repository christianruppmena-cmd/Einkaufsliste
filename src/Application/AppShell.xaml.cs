using Einkaufsliste.Application.Pages;
using Einkaufsliste.Application.Pages.Produkte;
using Einkaufsliste.Application.Pages.Datenbank;
using Einkaufsliste.Application.Pages.Kategorien;


namespace Einkaufsliste.Application
{
    public partial class AppShell : Shell
    {
        private readonly ShellContent startInhalt;
        
        //Die Einkaufsliste wird nach der Datenbankinitialisiernug angezeigt.
        private readonly MainPage _mainPage;
        
        //Ermöglicht es, Nebenseiten erst bei Bedarf über DI zu erzeugen.
        private readonly IServiceProvider _serviceProvider; 
        
        // Die Hauptseite wird für den Start übergeben.
        // Weitere Pages erzeugt die Shell erst wenn sie geöffnet werden
        public AppShell(MainPage mainPage, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _mainPage = mainPage;
            _serviceProvider = serviceProvider;

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
                
                //Die seite wird erst erzeugt wenn der Nutzer diesen Menüpunkt öffnet.
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<ProduktVerwaltungPage>())
            };

            // Kategorienverwaltung
            var kategorien = new ShellContent
            {
                Title = "🏷️ Kategorien",
                Route = "KategorienPage",
                
                //Die seite wird erst erzeugt wenn der Nutzer diesen Menüpunkt öffnet.
                ContentTemplate = new DataTemplate(() => _serviceProvider.GetRequiredService<KategorienPage>())
            };

            // Datensicherung
            var datensicherung = new ShellContent
            {
                Title = "💾 Datensicherung",
                Route = "DatenbanksicherungPage",

                //Die seite wird erst erzeugt wenn der Nutzer diesen Menüpunkt öffnet.
                ContentTemplate = new DataTemplate(()=> _serviceProvider.GetRequiredService<DatenbanksicherungPage>())
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

                                //Die seite wird erst erzeugt wenn der Nutzer diesen Menüpunkt öffnet.
                                ContentTemplate = new DataTemplate (() => _serviceProvider.GetRequiredService<HaendlerPage>())
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