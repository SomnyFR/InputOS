using System.IO;

namespace InputOS.Services;

// ============================================================================
// FONCTIONNALITÉ : JOURNAL LOCAL DES ENVOIS DE DIAGNOSTIC
// ============================================================================

public static class DiagnosticLogService
{
    private const int MaxLines = 200;

    // Une ligne par tentative : date, VID:PID, résultat. Jamais de nom de compte, de chemin de périphérique, de jeton ni d'IP.
    public static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "InputOS", "diagnostic-log.txt");

    public static void Append(ushort vendorId, ushort productId, string result)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            var lines = File.Exists(LogPath) ? File.ReadAllLines(LogPath).ToList() : [];
            lines.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} | {vendorId:X4}:{productId:X4} | {result}");
            File.WriteAllLines(LogPath, lines.Skip(Math.Max(0, lines.Count - MaxLines)));
        }
        catch (Exception) { /* le journal ne doit jamais bloquer l'envoi */ }
    }

    public static long GetSize() => File.Exists(LogPath) ? new FileInfo(LogPath).Length : 0;

    public static void Delete()
    {
        if (File.Exists(LogPath)) File.Delete(LogPath);
    }
}

// ============================================================================
// FIN FONCTIONNALITÉ : JOURNAL LOCAL DES ENVOIS DE DIAGNOSTIC
// ============================================================================
