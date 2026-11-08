
using Einkaufsliste.Services;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Application
{
    public partial class App : Application
    {
        private readonly EinkauflistenDatenbank _datenbank;
        private readonly IDBBackupService _backupService;
        public App(EinkauflistenDatenbank datenbank, IDBBackupService backupService)
        {
            InitializeComponent();
            _datenbank = datenbank;
            _backupService = backupService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var shell = new AppShell(_datenbank, _backupService);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    await _datenbank.InitialisierenAsync();
                    await _datenbank.StandardHaendlerAnlegenAsync();
                    await _datenbank.StandardkategorienAnlegenAsync();

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
