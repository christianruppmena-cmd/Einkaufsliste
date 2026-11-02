
using Einkaufsliste.Services;

namespace Einkaufsliste
{
    public partial class App : Application
    {
        private readonly EinkauflistenDatenbank datenbank;

        public App(EinkauflistenDatenbank datenbank)
        {
            InitializeComponent();
            this.datenbank = datenbank;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell(datenbank);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await datenbank.InitialisierenAsync();
                    await datenbank.StandardHaendlerAnlegenAsync();
                    await datenbank.StandardkategorienAnlegenAsync();

                    shell.StartseiteAnzeigen();
                }
                catch (Exception ex)
                {
                    await shell.DisplayAlertAsync(
                            "Datenbankfehler",
                            ex.Message,
                            "OK");
                }
            });

            return new Window(shell);
        }
    }
}
