using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;
using Velopack;
using Velopack.Logging;
using Velopack.Sources;

namespace InputOS.Services;

/// <summary>
/// Source Velopack qui lit la dernière release GitHub stable sans index : le paquet complet
/// <c>InputOS-X.Y.Z-full.nupkg</c> suffit (taille et SHA-256 viennent de l'API GitHub).
/// Les releases qui contiennent encore tous les fichiers historiques restent compatibles.
/// shortcut: toujours le paquet complet (pas de delta, ~85 Mo) ; ajouter les deltas si la taille devient un problème.
/// </summary>
public sealed class GithubReleaseUpdateSource : IUpdateSource
{
    private static readonly HttpClient Http = CreateClient();
    private readonly string latestReleaseUrl;
    private readonly Dictionary<string, string> urls = new(StringComparer.Ordinal);

    public GithubReleaseUpdateSource(string latestReleaseUrl = "https://api.github.com/repos/SomnyFR/InputOS/releases/latest")
        => this.latestReleaseUrl = latestReleaseUrl;

    private static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("InputOS-Updater", "1"));
        client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
        return client;
    }

    public async Task<VelopackAssetFeed> GetReleaseFeed(IVelopackLogger logger, string? appId, string channel, Guid? stagingId, VelopackAsset? latestLocalFullRelease)
    {
        // L'API « latest » ignore déjà brouillons et préreleases.
        using var json = JsonDocument.Parse(await Http.GetStringAsync(latestReleaseUrl));
        var found = new List<VelopackAsset>();
        appId ??= "InputOS";
        var fullPackage = new Regex($@"^{Regex.Escape(appId)}-(?<v>\d+\.\d+\.\d+(?:-[0-9A-Za-z.]+)?)-full\.nupkg$");
        urls.Clear();
        foreach (var item in json.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = item.GetProperty("name").GetString() ?? "";
            var match = fullPackage.Match(name);
            if (!match.Success) continue;
            var digest = item.TryGetProperty("digest", out var d) ? d.GetString() ?? "" : "";
            if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) continue; // sans empreinte, pas de mise à jour
            urls[name] = item.GetProperty("browser_download_url").GetString()!;
            found.Add(new VelopackAsset
            {
                PackageId = appId,
                Version = SemanticVersion.Parse(match.Groups["v"].Value),
                Type = VelopackAssetType.Full,
                FileName = name,
                SHA256 = digest[7..].ToUpperInvariant(),
                Size = item.GetProperty("size").GetInt64(),
            });
        }
        var best = found.OrderByDescending(a => a.Version).FirstOrDefault();
        return new VelopackAssetFeed { Assets = best == null ? [] : [best] };
    }

    public async Task DownloadReleaseEntry(IVelopackLogger logger, VelopackAsset releaseEntry, string localFile, Action<int> progress, CancellationToken cancelToken)
    {
        if (!urls.TryGetValue(releaseEntry.FileName, out var url)) throw new InvalidOperationException("Paquet inconnu : " + releaseEntry.FileName);
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancelToken);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength ?? releaseEntry.Size;
        await using (var input = await response.Content.ReadAsStreamAsync(cancelToken))
        await using (var output = File.Create(localFile))
        {
            var buffer = new byte[81920];
            long done = 0; var last = -1; int read;
            while ((read = await input.ReadAsync(buffer, cancelToken)) > 0)
            {
                await output.WriteAsync(buffer.AsMemory(0, read), cancelToken);
                done += read;
                var pct = total > 0 ? (int)(done * 100 / total) : 0;
                if (pct != last) { last = pct; progress(pct); }
            }
        }
        // Contrôle fait ici : on ne dépend pas de la vérification interne de Velopack pour une source personnalisée.
        await using var check = File.OpenRead(localFile);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(check, cancelToken));
        if (!hash.Equals(releaseEntry.SHA256, StringComparison.OrdinalIgnoreCase))
        {
            check.Close();
            File.Delete(localFile);
            throw new InvalidDataException("Empreinte du paquet de mise à jour incorrecte.");
        }
    }
}
