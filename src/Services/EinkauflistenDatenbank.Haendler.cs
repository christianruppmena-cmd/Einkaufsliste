using Einkaufsliste.Models;
using Einkaufsliste.Interfaces;

namespace Einkaufsliste.Services
{
    public partial class EinkauflistenDatenbank
    {
        public async Task HaendlerSpeichernAsync(Haendler haendler)
        {
            await InitialisierenAsync();

            if (haendler == null)
                return;

            haendler.Name = haendler.Name.Trim();

            if (string.IsNullOrWhiteSpace(haendler.Name))
                return;

            if (haendler.Id == 0)
                await _datenbank.InsertAsync(haendler);
            else
                await _datenbank.UpdateAsync(haendler);

            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task<List<Haendler>> AlleHaendlerVerwaltenLadenAsync()
        {
            await InitialisierenAsync();

            return await _datenbank.QueryAsync<Haendler>(
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

                var vorhanden = await _datenbank
                    .Table<Haendler>()
                    .Where(h => h.Name == name)
                    .FirstOrDefaultAsync();

                if (vorhanden == null)
                {
                    await _datenbank.InsertAsync(new Haendler
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
            await InitialisierenAsync();

            return await _datenbank
                .Table<Haendler>()
                .Where(h => h.IstAktiv)
                .OrderBy(h => h.Sortierung)
                .ToListAsync();
        }

    }
}
