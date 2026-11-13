using Einkaufsliste.Models;

namespace Einkaufsliste.Services
{
    public partial class EinkauflistenDatenbank
    {
        public async Task<Produktpreis?> AktuellenProduktpreisLadenAsync(int produktId, int haendlerId, string preisart)
        {
            await InitialisierenAsync();

            string heute = DateTime.Now.ToString("yyyy-MM-dd");

            List<Produktpreis> preise = await _datenbank
                .Table<Produktpreis>()
                .Where(p =>
                    p.ProduktId == produktId &&
                    p.HaendlerId == haendlerId &&
                    p.Preisart == preisart &&
                    p.IstAktiv)
                .ToListAsync();

            return preise
                .Where(p =>
                    string.CompareOrdinal(p.GueltigVon, heute) <= 0 &&
                    (string.IsNullOrEmpty(p.GueltigBis) ||
                     string.CompareOrdinal(p.GueltigBis, heute) >= 0))
                .OrderByDescending(p => p.GueltigVon)
                .FirstOrDefault();
        }
        public async Task<Produktpreis?> GuenstigstenAktuellenPreisLadenAsync(int produktId, int haendlerId)
        {
            Produktpreis? angebot = await AktuellenProduktpreisLadenAsync(
                produktId,
                haendlerId,
                "Angebot");

            if (angebot != null)
                return angebot;

            return await AktuellenProduktpreisLadenAsync(
                produktId,
                haendlerId,
                "Normalpreis");
        }
        private static void PreisInHistorieUebernehmen(SQLiteConnection conn, Produktpreis preis)
        {
            int naechsteVersion = conn.Table<PreisHistorie>()
                .Where(h => h.ProduktpreisId == preis.Id)
                .ToList()
                .Select(h => h.Versionsnummer)
                .DefaultIfEmpty(0)
                .Max() + 1;

            conn.Insert(new PreisHistorie
            {
                ProduktpreisId = preis.Id,
                Versionsnummer = naechsteVersion,
                ProduktId = preis.ProduktId,
                HaendlerId = preis.HaendlerId,
                PreisInCent = preis.PreisInCent,
                Preisart = preis.Preisart,
                ErfasstAm = preis.ErfasstAm,
                GueltigVon = preis.GueltigVon,
                GueltigBis = preis.GueltigBis
            });
        }
        public async Task ProduktpreisSpeichernAsync(Produktpreis produktpreis)
        {
            await InitialisierenAsync();

            if (produktpreis.ProduktId <= 0 ||
                produktpreis.HaendlerId <= 0 ||
                produktpreis.PreisInCent <= 0)
            {
                throw new ArgumentException(
                    "Bitte gültige Preisangaben machen.");
            }

            if (produktpreis.Preisart != "Normalpreis" &&
                produktpreis.Preisart != "Angebot")
            {
                throw new ArgumentException(
                    "Die Preisart muss Normalpreis oder Angebot sein.");
            }

            if (!DateTime.TryParseExact(
                produktpreis.GueltigVon,
                "yyyy-MM-dd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime neuerStart))
            {
                throw new ArgumentException(
                    "Das Gültig-ab-Datum ist ungültig.");
            }

            if (!string.IsNullOrWhiteSpace(produktpreis.GueltigBis))
            {
                if (!DateTime.TryParseExact(
                    produktpreis.GueltigBis,
                    "yyyy-MM-dd",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime neuesEnde) ||
                    neuesEnde < neuerStart)
                {
                    throw new ArgumentException(
                        "Das Gültig-bis-Datum ist ungültig.");
                }
            }

            produktpreis.ErfasstAm =
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            await _datenbank.RunInTransactionAsync(conn =>
            {
                // Alten Stand beim Bearbeiten sichern
                if (produktpreis.Id > 0)
                {
                    Produktpreis? alterPreis = conn.Find<Produktpreis>(
                        produktpreis.Id);

                    if (alterPreis != null)
                    {
                        PreisInHistorieUebernehmen(conn, alterPreis);
                    }
                }

                // Bisherige Preise derselben Art abschließen
                var vorhandenePreise = conn.Table<Produktpreis>()
                    .Where(p =>
                        p.ProduktId == produktpreis.ProduktId &&
                        p.HaendlerId == produktpreis.HaendlerId &&
                        p.Preisart == produktpreis.Preisart &&
                        p.IstAktiv &&
                        p.Id != produktpreis.Id)
                    .ToList();

                foreach (var vorhanden in vorhandenePreise)
                {
                    DateTime alterStart = DateTime.ParseExact(
                        vorhanden.GueltigVon,
                        "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture);

                    if (alterStart < neuerStart &&
                        (string.IsNullOrEmpty(vorhanden.GueltigBis) ||
                         vorhanden.GueltigBis.CompareTo(
                             neuerStart.ToString("yyyy-MM-dd")) >= 0))
                    {
                        vorhanden.GueltigBis =
                            neuerStart.AddDays(-1).ToString("yyyy-MM-dd");

                        conn.Update(vorhanden);

                        PreisInHistorieUebernehmen(conn, vorhanden);
                    }
                }

                // Neuen oder bearbeiteten Preis speichern
                if (produktpreis.Id == 0)
                {
                    conn.Insert(produktpreis);
                }
                else
                {
                    conn.Update(produktpreis);
                }

                // Neuen Preisstand historisch sichern
                PreisInHistorieUebernehmen(conn, produktpreis);
            });
            await _backupService.LokaleSicherungErstellenAsync();
        }
        public async Task ProduktpreisDeaktivierenAsync(int produktpreisId)
        {
            await InitialisierenAsync();

            if (produktpreisId <= 0)
                return;

            await _datenbank.RunInTransactionAsync(conn =>
            {
                Produktpreis? preis = conn.Find<Produktpreis>(produktpreisId);

                if (preis == null || !preis.IstAktiv)
                    return;

                // Bisherigen Preisstand in der Historie sichern
                PreisInHistorieUebernehmen(conn, preis);

                // Angebot deaktivieren
                preis.IstAktiv = false;
                preis.ErfasstAm = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                conn.Update(preis);
            });

            await _backupService.LokaleSicherungErstellenAsync();
        }
    }
}
