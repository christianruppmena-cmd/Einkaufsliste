using Einkaufsliste.Models;
using Einkaufsliste.Services;
using System.Globalization;

namespace Einkaufsliste;

public partial class PreisvergleichPage : ContentPage
{
	private readonly EinkauflistenDatenbank datenbank;
	private readonly PreisvergleichService preisvergleichService;
	public PreisvergleichPage()
	{
		InitializeComponent();

		datenbank = new EinkauflistenDatenbank();
		preisvergleichService = new PreisvergleichService(datenbank);
	}
	private async void OnVergleichenClicked(object sender, EventArgs e)
	{
		VergleichenButton.IsEnabled = false;
		VergleichErgebnisText.Text = "Preise werden verglichen ...";

		try
		{
			await datenbank.InitialisierenAsync();
			await datenbank.StandardfililialenAnlegenAsync();

			List<Einkaufsartikel> artikel =
				await datenbank.AlleArtikelLadenAsync();

			List<Filiale> filialen =
				await datenbank.AlleAktivenFilialenLadenAsync();

			Filiale? edeka = filialen.FirstOrDefault(f =>
			f.Name.Contains("EDEKA Karch", StringComparison.OrdinalIgnoreCase));

			Filiale? aldi = filialen.FirstOrDefault(f =>
			f.Name.Contains("ALDI SÜD", StringComparison.OrdinalIgnoreCase) && f.Name.Contains("Stromberg", StringComparison.OrdinalIgnoreCase));

			if (edeka == null || aldi == null)
			{
				VergleichErgebnisText.Text =
					"Eine oder beide Filialen wurden nicht gefunden.";

				return;
			}
			PreisvergleichErgebnis edekaErgebnis =
				await preisvergleichService.VergleichenAsync(artikel, edeka.Id);
			PreisvergleichErgebnis aldiErgebnis =
				await preisvergleichService.VergleichenAsync(artikel, aldi.Id);

			EdekaSummeText.Text =
				$"Bekannte Zwischensumme: {edekaErgebnis.Gesamtsumme.ToString("N2", CultureInfo.CurrentCulture)} €";
			EdekaFehlendePreiseText.Text =
				$"Artikel ohne Preis: {edekaErgebnis.ArtikelOhnePreis.Count}";
			EdekaStatusText.Text =
				edekaErgebnis.ArtikelOhnePreis.Count == 0 ? "Alle offenen Artikel haben einen Preis." : "Für einige Artikel fehlen noch Preise.";

			AldiSummeText.Text =
				$"Bekannte Zwischensumme: {aldiErgebnis.Gesamtsumme.ToString("N2", CultureInfo.CurrentCulture)} €";
			AldiFehlendePreiseText.Text =
				$"Artikel ohne Preis: {aldiErgebnis.ArtikelOhnePreis.Count}";
			AldiStatusText.Text =
				aldiErgebnis.ArtikelOhnePreis.Count == 0 ? "Alle offenen Artikel haben einen Preis." : "Für einige Artikel fehlen noch Preise.";

			if (edekaErgebnis.ArtikelOhnePreis.Count == 0 && aldiErgebnis.ArtikelOhnePreis.Count == 0)
			{
				decimal differenz = Math.Abs(
					edekaErgebnis.Gesamtsumme -
					aldiErgebnis.Gesamtsumme);

				if (differenz == 0)
				{
					VergleichErgebnisText.Text = "Beide Märkte haben dieselbe Gesamtsumme.";
				}
				else if(edekaErgebnis.Gesamtsumme < aldiErgebnis.Gesamtsumme)
				{
					VergleichErgebnisText.Text = $"EDEKA ist {differenz.ToString("N2", CultureInfo.CurrentCulture)} € günstiger.";
				}
				else
				{
					VergleichErgebnisText.Text = $"ALDI ist {differenz.ToString("N2", CultureInfo.CurrentCulture)} € günstiger.";
				}
			}
			else
			{
				VergleichErgebnisText.Text =
					"Ein vollständiger Vergleich ist noch nicht möglich. Es fehlen Preise.";
			}

		}
		catch (Exception ex)
		{
			await DisplayAlertAsync(
				"Fehler",
				$"Der Preisvergleich konnte nicht durchgeführt werden: \n{ex.Message}", "OK");
		}
		finally
		{
            VergleichenButton.IsEnabled = true;
		}
	}
}