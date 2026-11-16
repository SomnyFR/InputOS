using static InputOS.Services.LocalizationService;
using Velopack;
using Velopack.Sources;
using System.Windows.Threading;

namespace InputOS.Services;

public sealed class ApplicationUpdateService
{
    // ============================================================================
    // FONCTIONNALITÉ : MISES À JOUR AUTOMATIQUES GITHUB RELEASES
    // ============================================================================
#if UPDATE_TEST
    // Build de test isolée uniquement : source locale, jamais compilée dans une build de publication.
    private static readonly string? TestUrl = Environment.GetEnvironmentVariable("INPUTOS_UPDATE_TEST_URL");
    private readonly UpdateManager primary = new(TestUrl is { Length: > 0 } ? new GithubReleaseUpdateSource(TestUrl) : new GithubReleaseUpdateSource());
    private readonly UpdateManager legacy = new(TestUrl is { Length: > 0 } ? new GithubReleaseUpdateSource(TestUrl) : new GithubReleaseUpdateSource());
#else
    private readonly UpdateManager primary = new(new GithubReleaseUpdateSource());
    // Repli : ancienne source par index (releases.win.json), utilisée seulement si la nouvelle échoue ou ne trouve rien.
    private readonly UpdateManager legacy = new(new GithubSource("https://github.com/SomnyFR/InputOS", null, false));
#endif
    private UpdateManager manager;
    private readonly CancellationTokenSource cancellation = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromHours(4) };
    private bool busy;
    public string Status { get; private set; } = Tr("Mises à jour automatiques au démarrage.");
    public bool IsBusy => busy;
    public bool CanRestart => !busy && manager.UpdatePendingRestart != null;
    public event EventHandler? Changed;

    public ApplicationUpdateService() => manager = primary;

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
            Status = Tr("Cette copie utilise l’ancien installateur ou le mode développement. Installez la version avec mises à jour automatiques pour activer cette fonction.");
            Changed?.Invoke(this, EventArgs.Empty);
            return;
        }
        busy = true;
        SetStatus(Tr("Recherche d’une mise à jour…"));
        try
        {
            if (primary.UpdatePendingRestart != null || legacy.UpdatePendingRestart != null)
            {
                manager = primary.UpdatePendingRestart != null ? primary : legacy;
                SetStatus(Tr("Mise à jour prête. Elle sera installée au prochain lancement d’InputOS."));
                return;
            }
            // Chaque source est essayée au plus une fois, de la vérification au téléchargement.
            Exception? failure = null;
            var upToDateSeen = false;
            foreach (var candidate in new[] { primary, legacy })
            {
                try
                {
                    if (candidate == legacy) SetStatus(Tr("Recherche d’une mise à jour (source de secours)…"));
                    var update = await candidate.CheckForUpdatesAsync();
                    cancellation.Token.ThrowIfCancellationRequested();
                    if (update == null) { upToDateSeen = true; continue; }
                    SetStatus(Tr("Téléchargement de la mise à jour…"));
                    await candidate.DownloadUpdatesAsync(update, progress =>
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(() => SetStatus(Tf("Téléchargement de la mise à jour : {0} %", progress))), cancellation.Token);
                    manager = candidate;
                    SetStatus(Tr("Mise à jour prête. Elle sera installée au prochain lancement d’InputOS."));
                    return;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex) { failure = ex; }
            }
            // À jour seulement si au moins une source a répondu sans erreur ; indisponible si les deux ont échoué.
            if (failure != null && !upToDateSeen) throw failure;
            SetStatus(Tr("InputOS est à jour."));
        }
        catch (OperationCanceledException) { }
        catch (Exception)
        {
            SetStatus(Tr("Mise à jour indisponible pour le moment. InputOS continue de fonctionner ; une nouvelle vérification sera faite plus tard."));
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
