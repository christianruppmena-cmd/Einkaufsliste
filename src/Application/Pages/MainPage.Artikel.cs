using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages
{
    public partial class MainPage
    {
        private Einkaufsartikel? FindeGleichenOffenenArtikel(Einkaufsartikel artikelZumPruefen)
        {
            return artikelGruppen.SelectMany(gruppe => gruppe).FirstOrDefault(artikel =>
            {
                bool gleichesProdukt =
                artikel.ProduktId > 0 && artikelZumPruefen.ProduktId > 0
                ? artikel.ProduktId == artikelZumPruefen.ProduktId
                : string.Equals(
                    artikel.Name?.Trim(),
                    artikelZumPruefen.Name?.Trim(),
                    StringComparison.OrdinalIgnoreCase);

                bool gleicheEinheit = string.Equals(
                    artikel.Einheit?.Trim(),
                    artikelZumPruefen.Einheit?.Trim(),
                    StringComparison.OrdinalIgnoreCase);
                return gleichesProdukt && gleicheEinheit && !artikel.IstErledigt;
            });
        }
        private async Task ArtikelHinzufuegenOderZusammenfuehrenAsync(Einkaufsartikel neuerEintrag)
        {
            await _artikelHinzufuegenSperre.WaitAsync();

            try
            {
                List<Einkaufsartikel> aktuelleArtikel =
                    await _datenbank.AlleArtikelLadenAsync();

                ArtikelGruppieren(aktuelleArtikel);

                if (await VorhandenenArtikelBehandelnAsync(neuerEintrag))
                    return;

                await _datenbank.ArtikelSpeichernAsync(neuerEintrag);

                aktuelleArtikel = await _datenbank.AlleArtikelLadenAsync();
                ArtikelGruppieren(aktuelleArtikel);

                AktualisiereFortschritt();
                await GesamtsummeAktualisierenAsync();
            }
            finally
            {
                _artikelHinzufuegenSperre.Release();
            }
        }
        private async void OnArtikelLoeschenClicked(object sender, EventArgs e)
        {
            if (sender is Button button &&
                button.CommandParameter is Einkaufsartikel artikelZumLoeschen)
            {
                await _datenbank.ArtikelLoeschenAsync(artikelZumLoeschen);

                Artikelgruppe gruppe = artikelGruppen.FirstOrDefault(g => g.Contains(artikelZumLoeschen));

                if (gruppe != null)
                {
                    gruppe.Remove(artikelZumLoeschen);

                    if (gruppe.Count == 0)
                    {
                        artikelGruppen.Remove(gruppe);
                    }
                }

                AktualisiereFortschritt();
                await GesamtsummeAktualisierenAsync();
            }
        }
        private async void OnArtikelErledigtGeaendert(object sender, CheckedChangedEventArgs e)
        {
            if (sender is not CheckBox checkbox ||
                checkbox.BindingContext is not Einkaufsartikel artikel)
            {
                return;
            }

            await _datenbank.ArtikelSpeichernAsync(artikel);

            Artikelgruppe alteGruppe = artikelGruppen
                .FirstOrDefault(g => g.Contains(artikel));

            if (alteGruppe == null)
                return;

            alteGruppe.Remove(artikel);

            if (alteGruppe.Count == 0)
            {
                artikelGruppen.Remove(alteGruppe);
            }

            string zielGruppenName;

            if (artikel.IstErledigt)
            {
                zielGruppenName = "Erledigt";
            }
            else if (aktuelleGruppierung == "Kaufort")
            {
                zielGruppenName = alleHaendler
                    .FirstOrDefault(h => h.Id == artikel.AusgewaehlterHaendlerId)
                    ?.Name ?? "Ohne festgelegten Kaufort";
            }
            else
            {
                zielGruppenName = artikel.Kategorie;
            }

            Artikelgruppe zielGruppe = artikelGruppen
                .FirstOrDefault(g => g.Kategorie == zielGruppenName);

            if (zielGruppe == null)
            {
                zielGruppe = new Artikelgruppe(zielGruppenName);
                artikelGruppen.Add(zielGruppe);
            }

            zielGruppe.Add(artikel);

            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();
        }
        private async Task ArtikeldetailsBearbeitenAsync(Einkaufsartikel artikel)
        {
            string menge = await DisplayPromptAsync(
                "Menge Bearbeiten",
                $"Menge für {artikel.Name} :",
                initialValue: artikel.Menge,
                keyboard: Keyboard.Default);

            if (menge == null)
                return;

            menge = menge.Trim();

            if (string.IsNullOrWhiteSpace(menge))
            {
                await DisplayAlertAsync(
                    "Ungültige Menge",
                    "Bitte gib eine Menge ein.",
                    "OK");
                return;
            }

            string[] einheiten =
            {
                "Stück","Packung", "Flasche", "Kg", "g", "Liter", "ml"
            };

            string einheit = await DisplayActionSheetAsync(
                "Einheit auswählen",
                "Abbrechen",
                null,
                einheiten);

            if (string.IsNullOrWhiteSpace(einheit) || einheit == "Abbrechen")
            {
                return;
            }
            string hinweis = await DisplayPromptAsync(
                "Hinweis bearbeiten",
                "Zusätzliche Angaben, z.B. TK oder Vollkorn: ",
                initialValue: artikel.Hinweis);

            if (hinweis == null)
                return;

            artikel.Menge = menge;
            artikel.Einheit = einheit;
            artikel.Hinweis = hinweis.Trim();
        }
        private async void OnArtikelDetailsAnzeigenClicked(object sender, TappedEventArgs e)
        {
            if (sender is Label label &&
                label.BindingContext is Einkaufsartikel artikel)
            {
                await ArtikeldetailsBearbeitenAsync(artikel);

                // Liste aktualisieren , damit die Details sichtbar werden
                ArtikelListe.ItemsSource = null;
                ArtikelListe.ItemsSource = artikelGruppen;
            }
        }
        private async void OnMengeErhoehenClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not Einkaufsartikel artikel)
                return;

            if (!decimal.TryParse(
                artikel.Menge,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
            {
                menge = 1;
            }

            menge++;

            artikel.Menge = menge.ToString(
                System.Globalization.CultureInfo.CurrentCulture);

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnMengeVerringernClicked(object sender, EventArgs e)
        {
            if (sender is not Button button ||
                button.CommandParameter is not Einkaufsartikel artikel)
                return;

            if (!decimal.TryParse(
                artikel.Menge,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
            {
                menge = 1;
            }

            if (menge <= 1)
                return;

            menge--;

            artikel.Menge = menge.ToString(
                System.Globalization.CultureInfo.CurrentCulture);

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnMengeGeaendert(object sender, TextChangedEventArgs e)
        {
            if (sender is not Entry entry ||
                entry.BindingContext is not Einkaufsartikel artikel)
                return;

            string eingabe = e.NewTextValue?.Trim() ?? string.Empty;

            // Während der Eingabe leere oder ungültige Werte ignorieren
            if (!decimal.TryParse(
                eingabe,
                System.Globalization.NumberStyles.Number,
                System.Globalization.CultureInfo.CurrentCulture,
                out decimal menge))
                return;

            if (menge <= 0)
                return;

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private async void OnEinheitGeaendert(object sender, EventArgs e)
        {
            if (sender is not Picker picker ||
                picker.BindingContext is not Einkaufsartikel artikel)
                return;

            string neueEinheit = picker.SelectedItem?.ToString();

            if (string.IsNullOrWhiteSpace(neueEinheit))
                return;

            artikel.Einheit = neueEinheit;

            await _datenbank.ArtikelSpeichernAsync(artikel);

            await GesamtsummeAktualisierenAsync();
        }
        private static bool MengenUndHinweiseZusammenfassen(Einkaufsartikel vorhandenerArtikel, Einkaufsartikel neuerEintrag)
        {
            if (!decimal.TryParse(
                    vorhandenerArtikel.Menge,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out decimal alteMenge)
                || !decimal.TryParse(
                    neuerEintrag.Menge,
                    System.Globalization.NumberStyles.Number,
                    System.Globalization.CultureInfo.CurrentCulture,
                    out decimal neueMenge))
            {
                return false;
            }

            vorhandenerArtikel.Menge =
                (alteMenge + neueMenge).ToString(
                    System.Globalization.CultureInfo.CurrentCulture);

            if (string.IsNullOrWhiteSpace(vorhandenerArtikel.Hinweis))
            {
                vorhandenerArtikel.Hinweis = neuerEintrag.Hinweis;
            }
            else if (!string.IsNullOrWhiteSpace(neuerEintrag.Hinweis)
                     && !string.Equals(
                         vorhandenerArtikel.Hinweis,
                         neuerEintrag.Hinweis,
                         StringComparison.OrdinalIgnoreCase))
            {
                vorhandenerArtikel.Hinweis += " / " + neuerEintrag.Hinweis;
            }

            return true;
        }
        private async Task<bool> VorhandenenArtikelBehandelnAsync(Einkaufsartikel neuerEintrag)
        {
            Einkaufsartikel? vorhandenerArtikel =
                FindeGleichenOffenenArtikel(neuerEintrag);

            if (vorhandenerArtikel == null)
                return false;

            bool zusammenfassen = await DisplayAlertAsync(
                "Artikel bereits vorhanden",
                $"{vorhandenerArtikel.Name} ist bereits in deiner Einkaufsliste.\n\n" +
                $"Vorhanden: {vorhandenerArtikel.Menge} {vorhandenerArtikel.Einheit}\n" +
                $"Neu: {neuerEintrag.Menge} {neuerEintrag.Einheit}\n\n" +
                "Möchtest du die Mengen zusammenfassen?",
                "Zusammenfassen",
                "Separat hinzufügen");

            if (!zusammenfassen)
                return false;

            if (!MengenUndHinweiseZusammenfassen(
                    vorhandenerArtikel,
                    neuerEintrag))
            {
                await DisplayAlertAsync(
                    "Menge ungültig",
                    "Die Mengen konnten nicht addiert werden. Bitte prüfe die Angaben.",
                    "OK");
                return true;
            }

            await _datenbank.ArtikelSpeichernAsync(vorhandenerArtikel);
            AktualisiereFortschritt();
            await GesamtsummeAktualisierenAsync();

            return true;
        }
    }  
}
