// Scripts/Net/TunnelService.cs
// Link público para quem hospeda no próprio PC (tipo ngrok), usando o "Quick Tunnel" gratuito da
// Cloudflare: roda `cloudflared tunnel --url http://localhost:8787` e lê o endereço
// https://xxxx.trycloudflare.com que ele imprime. Não precisa de conta nem abrir porta no roteador.
//
// O cloudflared é um programa oficial da Cloudflare. Se não estiver instalado, o botão do menu baixa
// a versão oficial do GitHub (github.com/cloudflare/cloudflared) para a pasta de dados do jogo.
using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Godot;

namespace DeadFrequency;

public partial class TunnelService : Node
{
    public enum TunnelState
    {
        Off,
        Downloading,
        Starting,
        Ready,
        Failed,
    }

    public static TunnelService Instance { get; private set; }

    public TunnelState State { get; private set; } = TunnelState.Off;
    /// <summary>https://xxxx.trycloudflare.com quando pronto.</summary>
    public string PublicUrl { get; private set; } = "";
    public string ErrorMessage { get; private set; } = "";
    public float DownloadProgress { get; private set; }

    public event Action Changed;

    private static readonly Regex UrlPattern = new(@"https://[a-z0-9-]+\.trycloudflare\.com", RegexOptions.Compiled);
    private Process _process;
    private volatile string _foundUrl;
    private volatile string _lastLog = "";
    private HttpRequest _download;
    private double _startTimeout;
    private int _pendingPort;

    public static TunnelService Ensure()
    {
        if (Instance != null && IsInstanceValid(Instance))
            return Instance;
        var tunnel = new TunnelService { Name = "TunnelService" };
        NetClient.Instance.AddChild(tunnel);
        return tunnel;
    }

    public override void _EnterTree()
    {
        Instance = this;
        ProcessMode = ProcessModeEnum.Always;
    }

    public override void _ExitTree()
    {
        Stop();
        if (Instance == this)
            Instance = null;
    }

    public override void _Notification(int what)
    {
        // Fechou a janela do jogo: não deixa o cloudflared rodando sozinho.
        if (what == NotificationWMCloseRequest || what == NotificationPredelete)
            Stop();
    }

    // ---------------- Onde está o cloudflared ----------------

    private static string DownloadedPath => ProjectSettings.GlobalizePath("user://cloudflared" + (OS.GetName() == "Windows" ? ".exe" : ""));

    public static string FindExecutable()
    {
        string exe = OS.GetName() == "Windows" ? "cloudflared.exe" : "cloudflared";
        if (File.Exists(DownloadedPath))
            return DownloadedPath;
        string[] candidates =
        {
            @"C:\Program Files (x86)\cloudflared\cloudflared.exe",
            @"C:\Program Files\cloudflared\cloudflared.exe",
            System.Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Microsoft\WinGet\Links\cloudflared.exe"),
            "/usr/local/bin/cloudflared",
            "/usr/bin/cloudflared",
            "/opt/homebrew/bin/cloudflared",
        };
        foreach (string c in candidates)
            if (File.Exists(c))
                return c;
        foreach (string dir in (System.Environment.GetEnvironmentVariable("PATH") ?? "").Split(Path.PathSeparator))
        {
            try
            {
                string full = Path.Combine(dir.Trim(), exe);
                if (File.Exists(full))
                    return full;
            }
            catch (ArgumentException)
            {
                // entrada inválida no PATH
            }
        }
        return null;
    }

    public static bool IsInstalled => FindExecutable() != null;

    // ---------------- Baixar ----------------

    /// <summary>Baixa o cloudflared oficial para user:// (só Windows e Linux x64).</summary>
    public void Download()
    {
        string asset = OS.GetName() switch
        {
            "Windows" => "cloudflared-windows-amd64.exe",
            "Linux" => "cloudflared-linux-amd64",
            _ => null,
        };
        if (asset == null)
        {
            Fail("Baixe o cloudflared em developers.cloudflare.com (no Mac: brew install cloudflared).");
            return;
        }
        _download?.QueueFree();
        _download = new HttpRequest { DownloadFile = DownloadedPath, Timeout = 300, MaxRedirects = 10, UseThreads = true };
        AddChild(_download);
        _download.RequestCompleted += (result, code, headers, body) =>
        {
            _download.QueueFree();
            _download = null;
            if (result != (long)HttpRequest.Result.Success || code != 200)
            {
                Fail($"Não consegui baixar o cloudflared ({code}).");
                return;
            }
            if (OS.GetName() != "Windows")
                OS.Execute("chmod", new[] { "+x", DownloadedPath });
            State = TunnelState.Off;
            Changed?.Invoke();
            if (_pendingPort > 0)
                Start(_pendingPort);
        };
        Godot.Error err = _download.Request($"https://github.com/cloudflare/cloudflared/releases/latest/download/{asset}");
        if (err != Godot.Error.Ok)
        {
            Fail($"Não consegui baixar o cloudflared ({err}).");
            return;
        }
        State = TunnelState.Downloading;
        DownloadProgress = 0;
        Changed?.Invoke();
    }

    // ---------------- Ligar / desligar ----------------

    public void Start(int port)
    {
        Stop();
        string exe = FindExecutable();
        if (exe == null)
        {
            _pendingPort = port;
            Download();
            return;
        }
        _pendingPort = 0;
        _foundUrl = null;
        PublicUrl = "";
        ErrorMessage = "";
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"tunnel --no-autoupdate --url http://localhost:{port}",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };
            _process = new Process { StartInfo = info, EnableRaisingEvents = true };
            _process.OutputDataReceived += (_, e) => ReadLine(e.Data);
            _process.ErrorDataReceived += (_, e) => ReadLine(e.Data);
            _process.Start();
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();
        }
        catch (Exception e)
        {
            Fail("Não consegui iniciar o cloudflared: " + e.Message);
            return;
        }
        State = TunnelState.Starting;
        _startTimeout = 40.0;
        Changed?.Invoke();
    }

    public void Stop()
    {
        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                    _process.Kill(true);
            }
            catch (Exception)
            {
                // já terminou
            }
            _process.Dispose();
            _process = null;
        }
        if (State is TunnelState.Starting or TunnelState.Ready)
        {
            State = TunnelState.Off;
            PublicUrl = "";
            Changed?.Invoke();
        }
    }

    private void ReadLine(string line)
    {
        if (string.IsNullOrEmpty(line))
            return;
        _lastLog = line;
        Match m = UrlPattern.Match(line);
        if (m.Success && !m.Value.Contains("api.trycloudflare"))
            _foundUrl = m.Value;
    }

    public override void _Process(double delta)
    {
        if (State == TunnelState.Downloading && _download != null)
        {
            int total = _download.GetBodySize();
            DownloadProgress = total > 0 ? _download.GetDownloadedBytes() / (float)total : 0f;
        }

        if (State != TunnelState.Starting)
            return;
        if (_foundUrl != null)
        {
            PublicUrl = _foundUrl;
            State = TunnelState.Ready;
            if (NetClient.Instance != null)
                NetClient.Instance.PublicServerUrl = PublicUrl;
            Changed?.Invoke();
            return;
        }
        _startTimeout -= delta;
        bool exited = _process == null || _process.HasExited;
        if (exited || _startTimeout <= 0)
        {
            string log = _lastLog;
            Stop();
            Fail("O túnel não respondeu. " + (string.IsNullOrEmpty(log) ? "" : "Último aviso: " + log));
        }
    }

    private void Fail(string message)
    {
        ErrorMessage = message;
        State = TunnelState.Failed;
        GD.PushWarning("TunnelService: " + message);
        Changed?.Invoke();
    }
}
