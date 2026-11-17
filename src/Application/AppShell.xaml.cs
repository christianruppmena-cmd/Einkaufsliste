using Einkaufsliste.Application.Pages;
using Einkaufsliste.Interfaces;
using Einkaufsliste.Application.Pages.Produkte;
using Einkaufsliste.Application.Pages.Datenbank;
using Einkaufsliste.Application.Pages.Kategorien;

namespace Einkaufsliste.Application
{
    public partial class AppShell : Shell
    {
        private readonly ShellContent startInhalt;
        private readonly IDBBackupService _backupService;
        private readonly IKategorienService _kategorienService;
        private readonly IProdukteService _produkteService;
        private readonly IHaendlerService _haendlerService;
        private readonly IArtikelService _artikelService;
        private readonly IPreisService _preisService;

        public AppShell()
        {
            InitializeComponent();

            
            _backupService = Handler!.MauiContext.Services!.GetService<IDBBackupService>(); 
            _kategorienService = Handler!.MauiContext.Services!.GetService<IKategorienService>(); 
            _produkteService = Handler!.MauiContext.Services!.GetService<IProdukteService>(); 
            _haendlerService = Handler!.MauiContext.Services!.GetService<IHaendlerService>(); 
            _preisService = Handler!.MauiContext.Services!.GetService<IPreisService>(); 
            _artikelService = Handler!.MauiContext.Services!.GetService<IArtikelService>(); 

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
                Content = new ProduktVerwaltungPage()
            };

            // Kategorienverwaltung
            var kategorien = new ShellContent
            {
                Title = "🏷️ Kategorien",
                Route = "KategorienPage",
                Content = new KategorienPage()
            };

            // Datensicherung
            var datensicherung = new ShellContent
            {
                Title = "💾 Datensicherung",
                Route = "DatenbanksicherungPage",
                Content = new DatenbanksicherungPage()
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
                                Content = new HaendlerPage()
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
            startInhalt.Content = new MainPage();
        }
    }
}