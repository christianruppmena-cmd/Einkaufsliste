
using Einkaufsliste.Models;

namespace Einkaufsliste.Services;

public class ArtikelService
{
    private readonly DatenbankService _datenbankService;

    public ArtikelService(DatenbankService datenbankService)
    {
        _datenbankService = datenbankService;
    }

    public async Task<List<Einkaufsartikel>> AlleArtikelLadenAsync()
    {
        return await _datenbankService.Verbindung
            .Table<Einkaufsartikel>()
            .ToListAsync();
    }

    public async Task<Einkaufsartikel?> ArtikelLadenNachIdAsync(int id)
    {
        return await _datenbankService.Verbindung
            .Table<Einkaufsartikel>()
            .Where(a => a.Id == id)
            .FirstOrDefaultAsync();
    }

    public async Task ArtikelSpeichernAsync(Einkaufsartikel artikel)
    {
        if (artikel.Id == 0)
        {
            await _datenbankService.Verbindung.InsertAsync(artikel);
        }
        else
        {
            await _datenbankService.Verbindung.UpdateAsync(artikel);
        }
    }

    public async Task ArtikelLoeschenAsync(Einkaufsartikel artikel)
    {
        await _datenbankService.Verbindung.DeleteAsync(artikel);
    }
}
