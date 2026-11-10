using Einkaufsliste.Services;
using System.Globalization;
using Einkaufsliste.Models;

namespace Einkaufsliste.Application.Pages.Produkte;

public partial class ProduktVerwaltungPage : ContentPage
{
    private readonly EinkauflistenDatenbank _datenbank;
    private List<Produkt> alleProdukte = new();
    private List<Haendler> alleHaendler = new();
    private string ausgewaehlterBildPfad;
    private Produkt produktZumBearbeiten;
    private bool bildEntfernen;
    private readonly bool nurNeuesProduktFormular;
    public ProduktVerwaltungPage(EinkauflistenDatenbank datenbank, bool nurNeuesProduktFormular = false)
    {
        InitializeComponent();

        _datenbank = datenbank;
        this.nurNeuesProduktFormular = nurNeuesProduktFormular;

        if(nurNeuesProduktFormular)
        {
            GespeicherteProdukteBereich.IsVisible = false;
            ProduktFormularBereich.IsVisible = true;
            FormularTitel.Text = "Neues Produkt";
        }
        else
        {
            GespeicherteProdukteBereich.IsVisible = true;
            ProduktFormularBereich.IsVisible = false;
        }
    }
   
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LadeKategorienAsync();
        await LadeProdukteAsync();
        await LadeHaendlerAsync();
    }
    
    private async Task LadeProdukteAsync()
    {
        alleProdukte = await _datenbank.AlleProdukteLadenAsync();

        ProduktListe.ItemsSource = alleProdukte.Where(p => p.IstAktiv).OrderBy(p => p.Name).ToList();
    }
    
    private async void OnProduktLoeschenClicked(object sender, EventArgs e)
    {
        if (produktZumBearbeiten == null)
            return;

        bool bestaetigen = await DisplayAlertAsync(
            "Produkt löschen",
            $"Möchtest du „{produktZumBearbeiten.Name}“ wirklich aus der Produktverwaltung entfernen?\n\nBereits vorhandene Einkaufsartikel bleiben erhalten.",
            "Löschen",
            "Abbrechen");

        if (!bestaetigen)
            return;

        try
        {
            produktZumBearbeiten.IstAktiv = false;

            await _datenbank.ProduktSpeichernAsync(produktZumBearbeiten);
            await LadeProdukteAsync();

            ZurProduktlisteZurueckkehren();

            await DisplayAlertAsync(
                "Produkt entfernt",
                "Das Produkt wurde aus der Produktverwaltung entfernt. Vorhandene Einkaufsartikel bleiben erhalten.",
                "OK");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync(
                "Fehler",
                $"Das Produkt konnte nicht entfernt werden:\n{ex.Message}",
                "OK");
        }
    }
    
    private async void OnPreiseVerwaltenClicked( object sender,EventArgs e)
    {
        if (sender is not Button button ||
            button.BindingContext is not Produkt produkt)
        {
            return;
        }

        await Navigation.PushAsync(
            new ProduktpreisePage(_datenbank, produkt));
    }

}