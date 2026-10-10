using Velopack;
using Velopack.Sources;
using System.Windows.Threading;

namespace InputOS.Services;

public sealed class ApplicationUpdateService
{
    // ============================================================================
    // FONCTIONNALITÉ : MISES À JOUR AUTOMATIQUES GITHUB RELEASES
    // ============================================================================
    private readonly UpdateManager manager = new(new GithubSource("https://github.com/SomnyFR/InputOS", null, false));
    private readonly CancellationTokenSource cancellation = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromHours(4) };
    private bool busy;
    public string Status { get; private set; } = "Mises à jour automatiques au démarrage.";
    public bool IsBusy => busy;
    public bool CanRestart => !busy && manager.UpdatePendingRestart != null;
    public event EventHandler? Changed;

    public void Start()
    {
        timer.Tick += Timer_Tick;
        timer.Start();
        _ = CheckAsync();
    }
    private async void Timer_Tick(object? sender, EventArgs e) => await CheckAsync();

    public async Task CheckAsync()
    {
        if (busy) return;
        if (!manager.IsInstalled)
        {
            Status = "Cette copie utilise l’ancien installateur ou le mode développement. Installez la version avec mises à jour automatiques pour activer cette fonction.";
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }
        busy = true;
        SetStatus("Recherche d’une mise à jour…");
        try
        {
            if (manager.UpdatePendingRestart != null)
            {
                SetStatus("Mise à jour prête. Elle sera installée au prochain lancement d’InputOS.");
                return;
            }
            var update = await manager.CheckForUpdatesAsync();
            cancellation.Token.ThrowIfCancellationRequested();
            if (update == null) { SetStatus("InputOS est à jour."); return; }
            SetStatus("Téléchargement de la mise à jour…");
            await manager.DownloadUpdatesAsync(update, progress =>
                System.Windows.Application.Current.Dispatcher.BeginInvoke(() => SetStatus($"Téléchargement de la mise à jour : {progress} %")), cancellation.Token);
            SetStatus("Mise à jour prête. Elle sera installée au prochain lancement d’InputOS.");
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            SetStatus("Mise à jour indisponible pour le moment. InputOS continue de fonctionner ; une nouvelle vérification sera faite plus tard.");
        }
        finally
        {
            busy = false;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Restart()
    {
        if (!CanRestart) return;
        manager.ApplyUpdatesAndRestart(manager.UpdatePendingRestart!);
    }
    public void Stop()
    {
        timer.Stop();
        timer.Tick -= Timer_Tick;
        cancellation.Cancel();
    }
    private void SetStatus(string status)
    {
        Status = status;
        Changed?.Invoke(this, EventArgs.Empty);
    }
    // ============================================================================
    // FIN FONCTIONNALITÉ : MISES À JOUR AUTOMATIQUES GITHUB RELEASES
    // ============================================================================
}
