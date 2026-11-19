
using Microsoft.Extensions.Logging;
using Einkaufsliste.Services;
using Einkaufsliste.Interfaces;
using Einkaufsliste.Application;
using Einkaufsliste.Application.Pages;
using Einkaufsliste.Application.Pages.Produkte;
using Einkaufsliste.Application.Pages.Kategorien;
using Einkaufsliste.Application.Pages.Datenbank;


namespace Einkaufsliste
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Interfaces
            builder.Services.AddTransient<IDBBackupService, DBBackupService>();
            builder.Services.AddTransient<IArtikelService, ArtikelService>();
            builder.Services.AddTransient<IPreisService, PreiseService>();
            builder.Services.AddTransient<IHaendlerService, HaendlerService>();
            builder.Services.AddTransient<IKategorienService, KategorienService>();
            builder.Services.AddTransient<IProdukteService, ProdukteService>();
            builder.Services.AddTransient<IInitialisierungsService, InitialisierungsService>();
           

            // Pages werden erstellt mit den Passenden DI-Abhängigkeiten
            builder.Services.AddTransient<MainPage>();
            builder.Services.AddTransient<ProduktVerwaltungPage>();
            builder.Services.AddTransient<KategorienPage>();
            builder.Services.AddTransient<DatenbanksicherungPage>();
            builder.Services.AddTransient<HaendlerPage>();
            builder.Services.AddTransient<IProduktVerwaltungPageFactory, ProduktVerwaltungPageFactory>();

            builder.Services.AddSingleton<AppShell>();
            

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
