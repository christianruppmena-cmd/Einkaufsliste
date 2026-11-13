
namespace Einkaufsliste.Application.Pages.Produkte;

public class HaendlerPreisEintrag : System.ComponentModel.INotifyPropertyChanged
{
    public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

    private bool gueltigBisAktiviert;
    public int HaendlerId { get; set; }
    public string Haendler { get; set; } = "";
    public int NormalpreisId { get; set; }
    public int AngebotId { get; set; }
    public string NormalpreisText { get; set; } = "";
    public string AngebotText { get; set; } = "";

    public bool GueltigBisAktiviert
    {
        get => gueltigBisAktiviert;
        set
        {
            if (gueltigBisAktiviert == value)
                return;

            gueltigBisAktiviert = value;

            PropertyChanged?.Invoke(
                this,
                new System.ComponentModel.PropertyChangedEventArgs(
                    nameof(GueltigBisAktiviert)));
        }
    }
    public DateTime GueltigBis { get; set; } = DateTime.Today;
}