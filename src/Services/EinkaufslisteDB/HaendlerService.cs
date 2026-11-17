using Einkaufsliste.Interfaces;
using Einkaufsliste.Models;

namespace Einkaufsliste.Services
{
    public class HaendlerService : DBConnection, IHaendlerService
    {
        private readonly IDBBackupService _backupService;

        public HaendlerService(IDBBackupService backupService)
        {
            _backupService = backupService;
        }

        public async Task HaendlerSpeichernAsync(Haendler haendler)
        {
            if (haendler == null)
                return;

            haendler.Name = haendler.Name.Trim();

            if (string.IsNullOrWhiteSpace(haendler.Name))
                return;

            if (haendler.Id == 0)
                await Datenbank.InsertAsync(haendler);
            else
                await Datenbank.UpdateAsync(haendler);

            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task<List<Haendler>> AlleHaendlerVerwaltenLadenAsync()
        {
            return await Datenbank.QueryAsync<Haendler>(
                "SELECT * FROM Haendler ORDER BY name COLLATE NOCASE ASC");
        }
        public async Task StandardHaendlerAnlegenAsync()
        {
            string[] haendlerNamen =
            {
                "ALDI SÜD",
                "Lidl",
                "REWE",
                "EDEKA",
                "Penny",
                "Netto",
                "dm",
                "Rossmann",
                "Kaufland"
            };

            for (int i = 0; i < haendlerNamen.Length; i++)
            {
                string name = haendlerNamen[i];

                var vorhanden = await Datenbank
                    .Table<Haendler>()
                    .Where(h => h.Name == name)
                    .FirstOrDefaultAsync();

                if (vorhanden == null)
                {
                    await Datenbank.InsertAsync(new Haendler
                    {
                        Name = name,
                        IstAktiv = true,
                        Sortierung = i + 1
                    });
                }
            }
        }
        public async Task<List<Haendler>> AlleHaendlerLadenAsync()
        {
            return await Datenbank
                .Table<Haendler>()
                .Where(h => h.IstAktiv)
                .OrderBy(h => h.Sortierung)
                .ToListAsync();
        }
    }
}
