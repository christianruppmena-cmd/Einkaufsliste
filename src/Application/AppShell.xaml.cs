using Einkaufsliste.Services;
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
        private readonly EinkauflistenDatenbank _datenbank;
        private readonly IDBBackupService _backupService;

        public AppShell(EinkauflistenDatenbank datenbank, IDBBackupService backupService)
        {
            InitializeComponent();

            _datenbank = datenbank;
            _backupService = backupService;

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
                Content = new ProduktVerwaltungPage(datenbank)
            };

            // Kategorienverwaltung
            var kategorien = new ShellContent
            {
                Title = "🏷️ Kategorien",
                Route = "KategorienPage",
                Content = new KategorienPage(datenbank)
            };

            // Datensicherung
            var datensicherung = new ShellContent
            {
                Title = "💾 Datensicherung",
                Route = "DatenbanksicherungPage",
                Content = new DatenbanksicherungPage(backupService)
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
                Title = "🏪 Händlerverwaltung (in Arbeit)",
                IsEnabled = false,
                Items =
                {
                    new ShellSection
                    {
                        Items =
                        {
                            new ShellContent
                            {
                                Title = "Händlerverwaltung",
                                Content = new ContentPage()
                            }
                        }
                    }
                }
            };

            var datensicherungMenue = new FlyoutItem
            {
                Title = "💾 Datensicherung",
                Items =
                {
                    new ShellSection
                    {
                        Items = { datensicherung }
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
            Items.Add(datensicherungMenue);

            startInhalt = einkaufsliste;
        }

        public void StartseiteAnzeigen()
        {
            startInhalt.Content = new MainPage(_datenbank);
        }
    }
}