// Scripts/Core/UpdateChecker.cs
// Pergunta ao GitHub qual é a última versão publicada (releases/latest) e avisa o menu se for mais nova.
// Uma vez por execução; se não houver internet, fica quieto.
using System;
using System.Text.Json;
using Godot;

namespace DeadFrequency;

public static class UpdateChecker
{
    public const string Repo = "dieguesmosken/dead-frequency";
    public const string ReleasesPage = "https://github.com/" + Repo + "/releases/latest";
    private static string _latestTag;
    private static string _latestUrl;
    private static bool _checked;

    /// <summary>Chama onNewer(tag, url) se existir versão mais nova que GameVersion.Current.</summary>
    public static void Check(Node host, Action<string, string> onNewer)
    {
        if (_checked)
        {
            if (_latestTag != null && GameVersion.Compare(_latestTag.TrimStart('v'), GameVersion.Current) > 0)
                onNewer(_latestTag, _latestUrl);
            return;
        }
        _checked = true;
        var http = new HttpRequest { Timeout = 8 };
        host.AddChild(http);
        http.RequestCompleted += (result, code, headers, body) =>
        {
            http.QueueFree();
            if (result != (long)HttpRequest.Result.Success || code != 200)
                return;
            try
            {
                using JsonDocument doc = JsonDocument.Parse(body);
                _latestTag = doc.RootElement.GetProperty("tag_name").GetString();
                _latestUrl = doc.RootElement.TryGetProperty("html_url", out JsonElement u) ? u.GetString() : ReleasesPage;
                if (_latestTag != null && GameVersion.Compare(_latestTag.TrimStart('v'), GameVersion.Current) > 0)
                    onNewer(_latestTag, _latestUrl);
            }
            catch (Exception)
            {
                // resposta inesperada: ignora
            }
        };
        http.Request($"https://api.github.com/repos/{Repo}/releases/latest", new[] { "User-Agent: DeadFrequency-Game", "Accept: application/vnd.github+json" });
    }
}
