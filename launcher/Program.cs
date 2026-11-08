// Launcher do Dead Frequency.
// 1. Pergunta ao GitHub qual é o último release (github.com/dieguesmosken/dead-frequency/releases).
// 2. Se a versão instalada (pasta "game" ao lado do launcher) for mais antiga, baixa o zip do sistema
//    (windows ou linux), extrai e guarda a versão em game/version.txt.
// 3. Abre o jogo. Sem internet, abre a versão que já estiver instalada.
// Opções: --check (só verifica), --no-launch (atualiza e não abre), --dir <pasta> (onde instalar).
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text.Json;

const string Repo = "dieguesmosken/dead-frequency";
const string Title = "DEAD FREQUENCY · launcher";

bool checkOnly = args.Contains("--check");
bool noLaunch = args.Contains("--no-launch");
string baseDir = AppContext.BaseDirectory;
int dirArg = Array.IndexOf(args, "--dir");
if (dirArg >= 0 && dirArg + 1 < args.Length)
    baseDir = Path.GetFullPath(args[dirArg + 1]);
string gameDir = Path.Combine(baseDir, "game");
string versionFile = Path.Combine(gameDir, "version.txt");
bool windows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
string platform = windows ? "windows" : "linux";
string exeName = windows ? "DeadFrequency.exe" : "DeadFrequency.x86_64";

Console.OutputEncoding = System.Text.Encoding.UTF8;
try { Console.Title = Title; } catch { /* terminal sem título */ }
Say("", ConsoleColor.Green);
Say("  ██ DEAD FREQUENCY ██   sintonize a 96.6", ConsoleColor.Green);
Say("  launcher · atualização automática pelo GitHub\n", ConsoleColor.DarkGreen);

string installed = File.Exists(versionFile) ? File.ReadAllText(versionFile).Trim() : "";
bool hasGame = File.Exists(Path.Combine(gameDir, exeName));
Say(hasGame ? $"Instalado: {(installed.Length > 0 ? installed : "versão desconhecida")}" : "Jogo ainda não instalado.", ConsoleColor.Gray);

using var http = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("DeadFrequencyLauncher", "1.0"));
http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

string? latestTag = null;
string? assetUrl = null;
string? assetName = null;
long assetSize = 0;
try
{
    Say("Procurando atualizações...", ConsoleColor.Gray);
    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
    string json = await http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest", cts.Token);
    using JsonDocument doc = JsonDocument.Parse(json);
    latestTag = doc.RootElement.GetProperty("tag_name").GetString();
    foreach (JsonElement asset in doc.RootElement.GetProperty("assets").EnumerateArray())
    {
        string name = asset.GetProperty("name").GetString() ?? "";
        if (name.StartsWith("DeadFrequency-", StringComparison.OrdinalIgnoreCase) && name.Contains(platform, StringComparison.OrdinalIgnoreCase) && name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            assetUrl = asset.GetProperty("browser_download_url").GetString();
            assetName = name;
            assetSize = asset.GetProperty("size").GetInt64();
            break;
        }
    }
    Say($"Última versão: {latestTag}", ConsoleColor.Gray);
}
catch (Exception e)
{
    Say($"Não deu para falar com o GitHub ({e.GetType().Name}).", ConsoleColor.Yellow);
}

bool needsUpdate = latestTag != null && assetUrl != null && (!hasGame || CompareVersions(latestTag, installed) > 0);
if (checkOnly)
{
    Say(needsUpdate ? $"Atualização disponível: {latestTag}" : "Você já está na última versão.", ConsoleColor.Green);
    return 0;
}

if (needsUpdate)
{
    try
    {
        Say($"\nBaixando {assetName} ({assetSize / 1048576.0:0.0} MB)...", ConsoleColor.Green);
        string tmp = Path.Combine(Path.GetTempPath(), assetName!);
        await Download(http, assetUrl!, tmp, assetSize);
        Say("Instalando...", ConsoleColor.Green);
        string staging = gameDir + ".new";
        if (Directory.Exists(staging))
            Directory.Delete(staging, true);
        ZipFile.ExtractToDirectory(tmp, staging);
        // Alguns zips vêm com uma pasta dentro: usa a pasta que tem o executável.
        string root = Directory.GetFiles(staging, exeName, SearchOption.AllDirectories).Select(Path.GetDirectoryName).FirstOrDefault() ?? staging;
        if (Directory.Exists(gameDir))
            Directory.Delete(gameDir, true);
        Directory.Move(root!, gameDir);
        if (Directory.Exists(staging))
            Directory.Delete(staging, true);
        File.WriteAllText(versionFile, latestTag);
        File.Delete(tmp);
        if (!windows)
            File.SetUnixFileMode(Path.Combine(gameDir, exeName), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupExecute | UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        Say($"Pronto! Dead Frequency {latestTag} instalado.", ConsoleColor.Green);
        hasGame = true;
    }
    catch (Exception e)
    {
        Say($"Falha ao atualizar: {e.Message}", ConsoleColor.Red);
        if (!hasGame)
        {
            Say("Baixe manualmente em https://github.com/" + Repo + "/releases", ConsoleColor.Yellow);
            Pause();
            return 1;
        }
        Say("Abrindo a versão que já estava instalada.", ConsoleColor.Yellow);
    }
}
else if (hasGame)
{
    Say("Tudo atualizado.", ConsoleColor.Green);
}

if (!hasGame)
{
    Say("Não há jogo instalado e não foi possível baixar. Confira a internet e tente de novo.", ConsoleColor.Red);
    Pause();
    return 1;
}
if (noLaunch)
    return 0;

Say("\nAbrindo o jogo... boa entrega!", ConsoleColor.Green);
Process.Start(new ProcessStartInfo(Path.Combine(gameDir, exeName)) { WorkingDirectory = gameDir, UseShellExecute = false });
await Task.Delay(1200);
return 0;

static async Task Download(HttpClient http, string url, string path, long expected)
{
    using HttpResponseMessage res = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
    res.EnsureSuccessStatusCode();
    long total = res.Content.Headers.ContentLength ?? expected;
    await using Stream input = await res.Content.ReadAsStreamAsync();
    await using FileStream output = File.Create(path);
    var buffer = new byte[1 << 16];
    long done = 0;
    int read;
    var watch = Stopwatch.StartNew();
    long lastDraw = -1;
    while ((read = await input.ReadAsync(buffer)) > 0)
    {
        await output.WriteAsync(buffer.AsMemory(0, read));
        done += read;
        if (watch.ElapsedMilliseconds - lastDraw > 150 || done == total)
        {
            lastDraw = watch.ElapsedMilliseconds;
            double pct = total > 0 ? (double)done / total : 0;
            int bars = (int)(pct * 30);
            double speed = done / 1048576.0 / Math.Max(0.1, watch.Elapsed.TotalSeconds);
            Console.Write($"\r  [{new string('█', bars)}{new string('·', 30 - bars)}] {pct * 100,5:0.0}%  {speed:0.0} MB/s   ");
        }
    }
    Console.WriteLine();
}

static int CompareVersions(string a, string b)
{
    static int[] Parts(string v) => v.TrimStart('v', 'V').Split('.').Select(p => int.TryParse(new string(p.TakeWhile(char.IsDigit).ToArray()), out int n) ? n : 0).Concat(new[] { 0, 0, 0 }).Take(3).ToArray();
    int[] x = Parts(a), y = Parts(b);
    for (int i = 0; i < 3; i++)
        if (x[i] != y[i])
            return x[i].CompareTo(y[i]);
    return 0;
}

static void Say(string text, ConsoleColor color)
{
    ConsoleColor old = Console.ForegroundColor;
    Console.ForegroundColor = color;
    Console.WriteLine(text);
    Console.ForegroundColor = old;
}

static void Pause()
{
    if (Console.IsInputRedirected)
        return;
    Console.WriteLine("Aperte Enter para fechar.");
    Console.ReadLine();
}
