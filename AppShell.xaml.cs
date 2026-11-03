using Einkaufsliste.Services;

namespace Einkaufsliste
{
    public partial class AppShell : Shell
    {
        private readonly ShellContent startInhalt;
        private readonly EinkauflistenDatenbank datenbank;

        public AppShell(EinkauflistenDatenbank datenbank)
        {
            InitializeComponent();

            this.datenbank = datenbank;

            var einkaufsliste = new ShellContent
            {
                Title = "Einkaufsliste",
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

            var produktverwaltung = new ShellContent
            {
                Title = "Produktverwaltung",
                Route = "ProduktverwaltungPage",
                Content = new ProduktverwaltungPage(datenbank)
            };

            var datensicherung = new ShellContent
            {
                Title = "Datensicherung",
                Route = "DatenbanksicherungPage",
                Content = new DatenbanksicherungPage(datenbank)
            };

            var bereich = new ShellSection
            {
                Items =
                {
                    einkaufsliste,
                    produktverwaltung,
                    datensicherung
                }
            };

            var menuepunkt = new FlyoutItem
            {
                Title = "Verwaltung",
                Items = { bereich }
            };

            Items.Add(menuepunkt);

            startInhalt = einkaufsliste;
        }

        public void StartseiteAnzeigen()
        {
            startInhalt.Content = new MainPage(datenbank);
        }
    }
}