using System.Globalization;
using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages.Produkte;

public partial class ProduktVerwaltungPage
{
    private Produkt ProduktAusFormularErstellenOderAktualisieren(string name, string marke, string variante, string kategorie, string einheit, int bevorzugterHaendlerId, decimal packungsgroesse)
    {
        Produkt produkt = produktZumBearbeiten ?? new Produkt();

        produkt.Name = name;
        produkt.Marke = marke;
        produkt.Variante = variante;
        produkt.Kategorie = kategorie;
        produkt.Basiseinheit = einheit ?? "";
        produkt.BevorzugterHaendlerId = bevorzugterHaendlerId;
        produkt.Packungsgroesse = packungsgroesse;

        if (bildEntfernen)
        {
            produkt.BildPfad = null;
        }
        else if (ausgewaehlterBildPfad != null)
        {
            produkt.BildPfad = ausgewaehlterBildPfad;
        }
        return produkt;
    }
    private void OnNeuesProduktClicked(object sender, EventArgs e)
    {
        FormularZustandZuruecksetzen();

        NameEntry.Text = "";
        MarkeEntry.Text = "";
        VarianteEntry.Text = "";
        KategoriePicker.SelectedItem = null;
        BevorzugterHaendlerPicker.SelectedIndex = 0;
        PackungsgroesseEntry.Text = "";
        BasiseinheitPicker.SelectedItem = null;

        FormularTitel.Text = "Neues Produkt";
        ProduktLoeschenButton.IsVisible = false;

        GespeicherteProdukteBereich.IsVisible = false;
        ProduktFormularBereich.IsVisible = true;
    }
    private void OnFormularAbbrechenClicked(object sender, EventArgs e)
    {
        FormularZustandZuruecksetzen();

        ProduktFormularBereich.IsVisible = false;
        GespeicherteProdukteBereich.IsVisible = true;
    }
    private void ZurProduktlisteZurueckkehren()
    {
        FormularZustandZuruecksetzen();

        ProduktFormularBereich.IsVisible = false;
        GespeicherteProdukteBereich.IsVisible = true;
    }
    private async Task LadeKategorienAsync()
    {
        List<Kategorie> kategorien =
            await _datenbank.AlleKategorieEintraegeLadenAsync();

        KategoriePicker.ItemsSource = kategorien
            .Select(k => k.Name)
            .ToList();
    }
    private async Task LadeHaendlerAsync()
    {
        alleHaendler = await _datenbank.AlleHaendlerLadenAsync();

        var haendlerNamen = new List<string>
    {
        "Kein bevorzugter Markt"
    };

        haendlerNamen.AddRange(
            alleHaendler.Select(h => h.Name));

        BevorzugterHaendlerPicker.ItemsSource = haendlerNamen;
        BevorzugterHaendlerPicker.SelectedIndex = 0;
    }
    private async Task<bool> ProduktPflichtfelderValidierenAsync(string name,string kategorie)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            await DisplayAlertAsync(
                "Eingabe fehlt",
                "Bitte gib einen Produktnamen ein.",
                "OK");
            return false;
        }

        if (string.IsNullOrWhiteSpace(kategorie))
        {
            await DisplayAlertAsync(
                "Eingabe fehlt",
                "Bitte wähle eine Kategorie aus.",
                "OK");
            return false;
        }

        return true;
    }
    private async Task<decimal?> PackungsgroesseValidierenAsync(string packungText,string einheit)
    {
        if (string.IsNullOrWhiteSpace(packungText))
            return 0m;

        if (!decimal.TryParse(
            packungText.Replace(',', '.'),
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal packung)
            || packung <= 0)
        {
            await DisplayAlertAsync(
                "Ungültige Packungsgröße",
                "Bitte gib eine gültige Zahl größer als 0 ein.",
                "OK");
            return null;
        }

        if (string.IsNullOrWhiteSpace(einheit))
        {
            await DisplayAlertAsync(
                "Einheit fehlt",
                "Bitte wähle eine Basiseinheit aus.",
                "OK");
            return null;
        }

        return packung;
    }
    private async Task<bool> VorhandenesProduktZusammenfuehrenAsync(Produkt zuSpeicherndesProdukt,Produkt vorhandenesProdukt,string urspruenglicherName)
    {
        if (zuSpeicherndesProdukt.Id == 0)
        {
            await DisplayAlertAsync(
                "Produkt bereits vorhanden",
                $"„{vorhandenesProdukt.Name}“ existiert bereits.",
                "OK");
            return false;
        }

        bool bestaetigen = await DisplayAlertAsync(
            "Produkt bereits vorhanden",
            $"„{vorhandenesProdukt.Name}“ existiert bereits. " +
            "Sollen Einkaufsartikel, Preise und Preisverläufe von " +
            $"„{urspruenglicherName}“ übernommen werden? " +
            "Das vorhandene Produkt behält seine Stammdaten.",
            "Ja, übernehmen",
            "Nein");

        if (!bestaetigen)
            return false;

        await _datenbank.ProduktZusammenfuehrenAsync(
            zuSpeicherndesProdukt.Id,
            vorhandenesProdukt.Id);

        // Das vorhandene Produkt behält sein Bild.
        if (!string.IsNullOrWhiteSpace(ausgewaehlterBildPfad) &&
            File.Exists(ausgewaehlterBildPfad))
        {
            File.Delete(ausgewaehlterBildPfad);
        }

        return true;
    }
    private async void OnProduktSpeichernClicked(object sender, EventArgs e)
    {
        string name = NameEntry.Text?.Trim() ?? "";
        string marke = MarkeEntry.Text?.Trim() ?? "";
        string variante = VarianteEntry.Text?.Trim() ?? "";

        string kategorie = KategoriePicker.SelectedItem?.ToString();
        string einheit = BasiseinheitPicker.SelectedItem?.ToString();
        string ausgewaehlterHaendler = BevorzugterHaendlerPicker.SelectedItem?.ToString();
        int bevorzugterHaendlerId = alleHaendler.FirstOrDefault(h => h.Name == ausgewaehlterHaendler)?.Id ?? 0;

        if (!await ProduktPflichtfelderValidierenAsync(name, kategorie))
            return;
        
        string packungText = PackungsgroesseEntry.Text?.Trim() ?? "";
        decimal? validiertePackungsgroesse = await PackungsgroesseValidierenAsync(packungText, einheit);

        if(validiertePackungsgroesse is null)
        {
            return;
        }

        decimal packungsgroesse = validiertePackungsgroesse.Value;

        string altesBild = produktZumBearbeiten?.BildPfad;

        string urspruenglicherName = produktZumBearbeiten?.Name ?? name;

        Produkt zuSpeicherndesProdukt = ProduktAusFormularErstellenOderAktualisieren(
            name,
            marke,
            variante,
            kategorie,
            einheit,
            bevorzugterHaendlerId,
            packungsgroesse);

        try
        {
            Produkt? vorhandenesProdukt =
                        await _datenbank.ProduktMitGleichemSchluesselLadenAsync(zuSpeicherndesProdukt);

            bool wurdeZusammengefuehrt = false;

            if (vorhandenesProdukt != null)
            {
                bool zusammengefuehrt =
                    await VorhandenesProduktZusammenfuehrenAsync(zuSpeicherndesProdukt, vorhandenesProdukt, urspruenglicherName);

                if (!zusammengefuehrt)
                    return;
                wurdeZusammengefuehrt = true;
            }
            else
            {
                await _datenbank.ProduktSpeichernAsync(zuSpeicherndesProdukt);

                await _datenbank.EinkaufsartikelHaendlerAktualisierenAsync(
                    zuSpeicherndesProdukt.Id,
                    zuSpeicherndesProdukt.BevorzugterHaendlerId);
            }

            FormularZustandZuruecksetzen();

            await LadeProdukteAsync();

            await AltesProduktbildLoeschenAsync(
                altesBild,
                zuSpeicherndesProdukt.BildPfad);

            await DisplayAlertAsync(
                wurdeZusammengefuehrt
                    ? "Produkte zusammengeführt"
                    : "Gespeichert",
                wurdeZusammengefuehrt
                    ? $"Die Daten von „{urspruenglicherName}“ wurden in „{vorhandenesProdukt!.Name}“ übernommen."
                    : $"{name} wurde erfolgreich gespeichert.",
                "OK");
            // Bei direktem Aufruf über den Plus-Button zurückkehren
            if (nurNeuesProduktFormular)
            {
                await Navigation.PopAsync();
                return;
            }
            ZurProduktlisteZurueckkehren();
        }
        catch (InvalidOperationException ex)
        {
            await DisplayAlertAsync(
                "Produkt bereits vorhanden", ex.Message, "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Speicherfehler", ex.Message, "OK");
        }
    }
    private void OnProduktBearbeitenClicked(object sender, EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not Produkt produkt)
        {
            return;
        }

        produktZumBearbeiten = produkt;
        bildEntfernen = false;
        ausgewaehlterBildPfad = null;

        FormularTitel.Text = "Produkt bearbeiten";
        ProduktLoeschenButton.IsVisible = true;

        NameEntry.Text = produkt.Name;
        MarkeEntry.Text = produkt.Marke;
        VarianteEntry.Text = produkt.Variante;

        KategoriePicker.SelectedItem = produkt.Kategorie;

        if (produkt.BevorzugterHaendlerId > 0)
        {
            var bevorzugterHaendler = alleHaendler.FirstOrDefault(
                h => h.Id == produkt.BevorzugterHaendlerId);

            BevorzugterHaendlerPicker.SelectedItem =
                bevorzugterHaendler?.Name ?? "Kein bevorzugter Markt";
        }
        else
        {
            BevorzugterHaendlerPicker.SelectedIndex = 0;
        }

        PackungsgroesseEntry.Text =
            produkt.Packungsgroesse.ToString(
                CultureInfo.CurrentCulture) ?? "";

        BasiseinheitPicker.SelectedItem = produkt.Basiseinheit;

        GespeicherteProdukteBereich.IsVisible = false;
        ProduktFormularBereich.IsVisible = true;
    }
    private void FormularZustandZuruecksetzen()
    {
        produktZumBearbeiten = null;
        ausgewaehlterBildPfad = null;
        bildEntfernen = false;
    }
}
