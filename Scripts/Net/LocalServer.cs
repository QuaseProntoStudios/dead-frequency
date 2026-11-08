// Scripts/Net/LocalServer.cs
// "Hospedar no meu PC": o próprio jogo vira o servidor da sala. É o mesmo protocolo do servidor da
// Cloudflare (server/src/index.ts), reescrito em C#: turnos, pedidos, pagamentos, dinheiro do grupo,
// donos dos objetos e anfitrião. Escuta WebSocket na porta 8787; os amigos entram pela rede local
// ou pelo link público do TunnelService (Cloudflare Tunnel, tipo ngrok).
//
// Criado sob demanda como filho do autoload NetClient, então sobrevive à troca de cena.
// Ao mudar as regras aqui, mude também em server/src/index.ts (e vice-versa).
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Godot;

namespace DeadFrequency;

public partial class LocalServer : Node
{
    public const int DefaultPort = 8787;
    private const int ProtocolVersion = NetClient.ProtocolVersion;
    private const int MaxPlayers = 8;
    private const int MaxMessageBytes = 8 * 1024;
    private const double NextShiftDelay = 4.0;
    private const double GameOverRestart = 8.0;
    private const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>Mesma ordem de DeliveryManager.CargoCatalog / CARGO_CATALOG do servidor.</summary>
    private static readonly int[] CargoPayments = { 150, 300, 120, 180, 220, 90, 160, 140, 260, 280, 200, 110, 170 };
    private static readonly string[] PlayerColors = { "#8cff9e", "#ffb347", "#6ec6ff", "#ff6b6b", "#d58cff", "#fff27a", "#7affea", "#ff9ad5" };

    public static LocalServer Instance { get; private set; }

    public bool Running => _tcp != null && _tcp.IsListening();
    public string Code { get; private set; } = "";
    public int Port { get; private set; } = DefaultPort;
    public int PlayerCount => _peers.Count(p => p.Joined);

    private sealed class Job
    {
        public string Id = "";
        public int Type;
        public int Pick;
        public int Slot;
        public string Status = "pending";
    }

    private sealed class Peer
    {
        public WebSocketPeer Ws;
        public int Id;
        public string Name = "";
        public string Color = "";
        public bool Joined;
        public double Age;
        public bool Closed;
        public Dictionary<string, object> Snapshot;
        public string Held;
        public int Seat;
        public string Prof = Professions.Entregador;
        public double LastEarn = -1e9;
    }

    /// <summary>Mesmos valores de PROFESSIONS em server/src/index.ts (pay, maxUnits, maxAmount, cooldown em s).</summary>
    private static readonly Dictionary<string, (int Pay, int MaxUnits, int MaxAmount, double Cooldown, string[] Levels)> ProfessionRules = new()
    {
        [Professions.Entregador] = (0, 0, 0, 0, new[] { "city", "farm" }),
        [Professions.Taxista] = (30, 1, 400, 8, new[] { "city", "farm" }),
        [Professions.Gari] = (25, 3, 0, 1.5, new[] { "city", "farm" }),
        [Professions.Repositor] = (30, 1, 0, 2.5, new[] { "city" }),
        [Professions.Frentista] = (45, 1, 0, 5, new[] { "city" }),
        [Professions.Locutor] = (20, 1, 0, 9, new[] { "city" }),
    };
    private static readonly string[] ProfessionOrder = { Professions.Entregador, Professions.Taxista, Professions.Gari, Professions.Repositor, Professions.Frentista, Professions.Locutor };
    private bool _autoProfession;

    private TcpServer _tcp;
    private readonly List<Peer> _peers = new();
    private readonly Dictionary<string, Dictionary<string, object>> _objects = new();
    private readonly Dictionary<string, int?> _owners = new();
    private List<Job> _jobs = new();
    private readonly RandomNumberGenerator _rng = new();
    private string _level = "city";
    private int _seed;
    private int _shift;
    private int _money;
    private int _nextPlayerId = 1;
    private int? _hostId;
    private bool _pendingShift;
    private bool _gameOver;
    private double _shiftTimer = -1;

    /// <summary>Cria (uma vez) o servidor local pendurado no NetClient.</summary>
    public static LocalServer Ensure()
    {
        if (Instance != null && IsInstanceValid(Instance))
            return Instance;
        var server = new LocalServer { Name = "LocalServer" };
        NetClient.Instance.AddChild(server);
        return server;
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

    // ---------------- Liga / desliga ----------------

    public Error Start(string level, int port = DefaultPort, bool autoProfession = false)
    {
        _autoProfession = autoProfession;
        Stop();
        _tcp = new TcpServer();
        Error err = _tcp.Listen((ushort)port, "*");
        if (err != Error.Ok)
        {
            _tcp = null;
            return err;
        }
        Port = port;
        _rng.Randomize();
        _level = level;
        _seed = (int)(_rng.Randi() % 1_000_000);
        Code = new string(Enumerable.Range(0, 5).Select(_ => CodeAlphabet[(int)(_rng.Randi() % CodeAlphabet.Length)]).ToArray());
        _shift = 0;
        _money = 0;
        _nextPlayerId = 1;
        _hostId = null;
        _owners.Clear();
        _owners["van"] = null;
        _objects.Clear();
        _gameOver = false;
        NewShift(false);
        GD.Print($"LocalServer: sala {Code} ({level}) na porta {port}");
        return Error.Ok;
    }

    public void Stop()
    {
        foreach (Peer p in _peers)
        {
            try
            {
                p.Ws.SendText(JsonSerializer.Serialize(new { t = "kick", msg = "O anfitrião fechou a sala." }));
                p.Ws.Close(4001, "host closed");
            }
            catch (Exception)
            {
                // já fechado
            }
        }
        _peers.Clear();
        _tcp?.Stop();
        _tcp = null;
        _shiftTimer = -1;
    }

    /// <summary>Endereços desta máquina na rede local (para amigos na mesma casa/Wi-Fi).</summary>
    public static List<string> LanAddresses()
    {
        var list = new List<string>();
        foreach (string ip in IP.GetLocalAddresses())
        {
            if (ip.Contains(':'))
                continue; // IPv6
            if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || (ip.StartsWith("172.") && int.TryParse(ip.Split('.')[1], out int b) && b >= 16 && b <= 31))
                list.Add(ip);
        }
        return list;
    }

    // ---------------- Loop ----------------

    public override void _Process(double delta)
    {
        if (_tcp == null)
            return;

        while (_tcp.IsConnectionAvailable())
        {
            StreamPeerTcp conn = _tcp.TakeConnection();
            if (_peers.Count >= MaxPlayers)
            {
                conn.DisconnectFromHost();
                continue;
            }
            var ws = new WebSocketPeer { InboundBufferSize = 1 << 20, OutboundBufferSize = 1 << 20 };
            if (ws.AcceptStream(conn) != Error.Ok)
                continue;
            int id = _nextPlayerId++;
            _peers.Add(new Peer { Ws = ws, Id = id, Name = $"Entregador {id}", Color = PlayerColors[(id - 1) % PlayerColors.Length] });
        }

        foreach (Peer p in _peers.ToArray())
        {
            p.Ws.Poll();
            p.Age += delta;
            WebSocketPeer.State state = p.Ws.GetReadyState();
            if (state == WebSocketPeer.State.Open)
            {
                while (p.Ws.GetAvailablePacketCount() > 0)
                {
                    byte[] packet = p.Ws.GetPacket();
                    if (packet.Length <= MaxMessageBytes)
                        HandleMessage(p, packet.GetStringFromUtf8());
                }
            }
            else if (state == WebSocketPeer.State.Closed || (state == WebSocketPeer.State.Connecting && p.Age > 10))
            {
                RemovePeer(p);
            }
        }

        if (_shiftTimer > 0)
        {
            _shiftTimer -= delta;
            if (_shiftTimer <= 0)
                OnShiftTimer();
        }
    }

    // ---------------- Mensagens ----------------

    private void HandleMessage(Peer me, string text)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(text);
        }
        catch (JsonException)
        {
            return;
        }
        using (doc)
        {
            JsonElement msg = doc.RootElement;
            if (msg.ValueKind != JsonValueKind.Object)
                return;
            switch (Str(msg, "t"))
            {
                case "hello": OnHello(me, msg); break;
                case "tick": OnTick(me, msg); break;
                case "claim": OnClaim(me, Str(msg, "id")); break;
                case "release": OnRelease(me, Str(msg, "id")); break;
                case "deliver": OnDeliver(me, Str(msg, "cargoId"), Num(msg, "integ")); break;
                case "lost": OnLost(me, Str(msg, "cargoId")); break;
                case "buy": OnBuy(me, Str(msg, "item"), (int)Num(msg, "req")); break;
                case "earn": OnEarn(me, msg); break;
                case "prof": OnProf(me, Str(msg, "prof")); break;
                case "chat":
                {
                    string chat = Str(msg, "msg");
                    chat = chat.Length > 120 ? chat[..120].Trim() : chat.Trim();
                    if (chat.Length > 0 && me.Joined)
                        Broadcast(new { t = "chat", id = me.Id, name = me.Name, msg = chat });
                    break;
                }
                case "ping":
                    Send(me, new Dictionary<string, object> { ["t"] = "pong", ["ts"] = msg.TryGetProperty("ts", out JsonElement ts) ? ts.Clone() : 0 });
                    break;
            }
        }
    }

    private void OnHello(Peer me, JsonElement msg)
    {
        if ((int)Num(msg, "version") != ProtocolVersion)
        {
            Send(me, new { t = "error", msg = "Versão do jogo diferente da do anfitrião. Atualizem o jogo." });
            me.Ws.Close(4000, "version");
            return;
        }
        if (me.Joined)
            return;
        string name = SanitizeName(Str(msg, "name"));
        if (name.Length > 0)
            me.Name = name;
        me.Joined = true;
        me.Prof = _autoProfession ? AssignProfession(me) : ValidProfession(Str(msg, "prof"));

        if (_hostId == null || !_peers.Any(p => p.Joined && p.Id == _hostId))
            _hostId = me.Id;

        var others = _peers.Where(p => p.Joined && p != me)
            .Select(p => new Dictionary<string, object> { ["id"] = p.Id, ["name"] = p.Name, ["color"] = p.Color, ["prof"] = p.Prof, ["state"] = p.Snapshot })
            .ToList();
        Send(me, new Dictionary<string, object>
        {
            ["t"] = "welcome",
            ["you"] = me.Id,
            ["name"] = me.Name,
            ["color"] = me.Color,
            ["code"] = Code,
            ["seed"] = _seed,
            ["level"] = _level,
            ["prof"] = me.Prof,
            ["autoProfession"] = _autoProfession,
            ["host"] = _hostId,
            ["players"] = others,
            ["shift"] = _shift,
            ["money"] = _money,
            ["jobs"] = JobsPayload(),
            ["owners"] = _owners,
            ["objects"] = _objects.Values.ToList(),
        });
        Broadcast(new { t = "join", player = new { id = me.Id, name = me.Name, color = me.Color, prof = me.Prof } }, me);
    }

    private void OnTick(Peer me, JsonElement msg)
    {
        if (!me.Joined)
            return;
        string held = msg.TryGetProperty("held", out JsonElement h) && h.ValueKind == JsonValueKind.String ? Clip(h.GetString(), 24) : null;
        string eq = Str(msg, "eq");
        eq = new string(eq.Where(c => (c >= 'a' && c <= 'z') || c == ',').ToArray());
        me.Held = held;
        me.Seat = (int)Num(msg, "st") == 1 ? 1 : 0;
        me.Snapshot = new Dictionary<string, object>
        {
            ["p"] = Vec(msg, "p", 3),
            ["ry"] = Num(msg, "ry"),
            ["hp"] = Num(msg, "hp"),
            ["st"] = me.Seat,
            ["held"] = held,
            ["eq"] = Clip(eq, 80),
        };

        var objs = new List<Dictionary<string, object>>();
        if (msg.TryGetProperty("objs", out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement o in arr.EnumerateArray().Take(16))
            {
                string id = Str(o, "id");
                if (id.Length == 0 || !_owners.TryGetValue(id, out int? owner) || owner != me.Id)
                    continue;
                var obj = new Dictionary<string, object> { ["id"] = id, ["p"] = Vec(o, "p", 3), ["q"] = Vec(o, "q", 4), ["v"] = Vec(o, "v", 3), ["w"] = Vec(o, "w", 3) };
                if (o.TryGetProperty("integ", out JsonElement integ) && integ.ValueKind == JsonValueKind.Number)
                    obj["integ"] = Mathf.Clamp(integ.GetDouble(), 0.0, 1.0);
                _objects[id] = obj;
                objs.Add(obj);
            }
        }

        var tick = new Dictionary<string, object>(me.Snapshot) { ["t"] = "tick", ["id"] = me.Id, ["objs"] = objs };
        Broadcast(tick, me);
    }

    private void OnClaim(Peer me, string id)
    {
        if (!IsKnownObject(id))
            return;
        int? owner = _owners.TryGetValue(id, out int? o) ? o : null;
        Peer ownerPeer = owner == null ? null : _peers.FirstOrDefault(p => p.Joined && p.Id == owner);
        bool busy = ownerPeer != null && owner != me.Id && (Vehicles.Contains(id) ? ownerPeer.Seat == 1 : ownerPeer.Held == id);
        if (busy)
        {
            Send(me, new { t = "claimFail", id, owner });
            return;
        }
        if (owner != me.Id)
        {
            _owners[id] = me.Id;
            Broadcast(new { t = "owner", id, owner = me.Id });
        }
    }

    private void OnRelease(Peer me, string id)
    {
        if (!Vehicles.Contains(id) || !_owners.TryGetValue(id, out int? o) || o != me.Id)
            return;
        _owners[id] = null;
        Broadcast(new Dictionary<string, object> { ["t"] = "owner", ["id"] = id, ["owner"] = null });
    }

    private void OnDeliver(Peer me, string cargoId, double integrity)
    {
        Job job = _jobs.FirstOrDefault(j => j.Id == cargoId);
        if (job == null || job.Status != "pending" || !_owners.TryGetValue(cargoId, out int? owner) || owner != me.Id)
            return;
        double integ = double.IsFinite(integrity) ? Math.Clamp(integrity, 0.0, 1.0) : 0.0;
        int payment = (int)Math.Round(CargoPayments[job.Type] * integ);
        job.Status = "delivered";
        _money += payment;
        _owners.Remove(cargoId);
        _objects.Remove(cargoId);
        Broadcast(new { t = "delivered", cargoId, by = me.Id, name = me.Name, payment, money = _money });
        CheckShiftDone();
    }

    private void OnLost(Peer me, string cargoId)
    {
        Job job = _jobs.FirstOrDefault(j => j.Id == cargoId);
        if (job == null || job.Status != "pending" || !_owners.TryGetValue(cargoId, out int? owner) || owner != me.Id)
            return;
        job.Status = "lost";
        Broadcast(new { t = "lost", cargoId, by = me.Id, name = me.Name });
        CheckShiftDone();
    }

    private void OnBuy(Peer me, string item, int req)
    {
        ItemDef def = ItemCatalog.Get(item);
        if (def == null || _money < def.Price)
        {
            Send(me, new { t = "buyFail", item, req });
            return;
        }
        _money -= def.Price;
        Broadcast(new { t = "money", money = _money, by = me.Id, name = me.Name, item, req });
    }

    // ---------------- Profissões ----------------

    private string ValidProfession(string prof) =>
        prof != null && ProfessionRules.TryGetValue(prof, out var r) && r.Levels.Contains(_level) ? prof : Professions.Entregador;

    private string AssignProfession(Peer me)
    {
        string best = Professions.Entregador;
        int bestCount = int.MaxValue;
        foreach (string p in ProfessionOrder)
        {
            if (!ProfessionRules[p].Levels.Contains(_level))
                continue;
            int c = _peers.Count(x => x.Joined && x != me && x.Prof == p);
            if (c < bestCount)
            {
                best = p;
                bestCount = c;
            }
        }
        return best;
    }

    private void OnProf(Peer me, string prof)
    {
        if (!me.Joined)
            return;
        if (_autoProfession)
        {
            Send(me, new { t = "prof", id = me.Id, prof = me.Prof, locked = true });
            return;
        }
        me.Prof = ValidProfession(prof);
        Broadcast(new { t = "prof", id = me.Id, prof = me.Prof });
    }

    private void OnEarn(Peer me, JsonElement msg)
    {
        string job = Str(msg, "job");
        double now = Time.GetTicksMsec() / 1000.0;
        if (!me.Joined || job == null || !ProfessionRules.TryGetValue(job, out var rule) || rule.MaxUnits == 0 || me.Prof != job || now - me.LastEarn < rule.Cooldown)
        {
            Send(me, new { t = "earnFail", job });
            return;
        }
        int payment = rule.MaxAmount > 0
            ? Math.Clamp((int)Math.Round(Num(msg, "amount")), rule.Pay, rule.MaxAmount)
            : rule.Pay * Math.Clamp((int)Num(msg, "units"), 1, rule.MaxUnits);
        me.LastEarn = now;
        _money += payment;
        Broadcast(new { t = "earned", by = me.Id, name = me.Name, job, payment, money = _money, what = Clip(Str(msg, "what") ?? "", 60) });
    }

    private int Entregadores() => _peers.Count(p => p.Joined && p.Prof == Professions.Entregador);

    private void RemovePeer(Peer me)
    {
        if (me.Closed)
            return;
        me.Closed = true;
        _peers.Remove(me);
        foreach (string id in _owners.Keys.ToList())
        {
            if (_owners[id] == me.Id)
            {
                _owners[id] = null;
                Broadcast(new Dictionary<string, object> { ["t"] = "owner", ["id"] = id, ["owner"] = null });
            }
        }
        if (_hostId == me.Id)
        {
            Peer next = _peers.FirstOrDefault(p => p.Joined);
            _hostId = next?.Id;
            if (next != null)
                Broadcast(new { t = "host", id = next.Id });
        }
        if (me.Joined)
            Broadcast(new { t = "leave", id = me.Id });
    }

    // ---------------- Turnos ----------------

    private void NewShift(bool broadcast)
    {
        _shift += 1;
        _pendingShift = false;
        int count = Math.Min(6, 2 + Math.Max(1, Entregadores()));
        foreach (Job old in _jobs)
        {
            _owners.Remove(old.Id);
            _objects.Remove(old.Id);
        }
        _jobs = Enumerable.Range(0, count).Select(i => new Job
        {
            Id = $"c{_shift}_{i}",
            Type = (int)(_rng.Randi() % (uint)CargoPayments.Length),
            Pick = (int)(_rng.Randi() % 100000),
            Slot = i,
        }).ToList();
        if (broadcast)
            Broadcast(new { t = "shift", shift = _shift, jobs = JobsPayload(), money = _money });
    }

    private void CheckShiftDone()
    {
        if (_pendingShift || _jobs.Any(j => j.Status == "pending"))
            return;
        _pendingShift = true;
        if (!_jobs.Any(j => j.Status == "delivered") && Entregadores() > 0)
        {
            _gameOver = true;
            _money = 0;
            Broadcast(new { t = "gameOver", shift = _shift, money = 0 });
            _shiftTimer = GameOverRestart;
            return;
        }
        Broadcast(new { t = "shiftDone", shift = _shift, jobs = _jobs.Count, money = _money });
        _shiftTimer = NextShiftDelay;
    }

    private void OnShiftTimer()
    {
        if (!_pendingShift)
            return;
        if (_gameOver)
        {
            _gameOver = false;
            _shift = 0;
        }
        NewShift(true);
    }

    // ---------------- Utilitários ----------------

    private List<Dictionary<string, object>> JobsPayload() =>
        _jobs.Select(j => new Dictionary<string, object> { ["id"] = j.Id, ["type"] = j.Type, ["pick"] = j.Pick, ["slot"] = j.Slot, ["status"] = j.Status }).ToList();

    /// <summary>Mesmos de VEHICLES no servidor.</summary>
    private static readonly string[] Vehicles = { "van", "moto", "truck_yellow", "truck_red" };

    private bool IsKnownObject(string id) => Vehicles.Contains(id) || _jobs.Any(j => j.Id == id && j.Status == "pending");

    private static void Send(Peer to, object payload)
    {
        if (to.Ws.GetReadyState() == WebSocketPeer.State.Open)
            to.Ws.SendText(JsonSerializer.Serialize(payload));
    }

    private void Broadcast(object payload, Peer except = null)
    {
        string text = JsonSerializer.Serialize(payload);
        foreach (Peer p in _peers)
        {
            if (p == except || !p.Joined || p.Ws.GetReadyState() != WebSocketPeer.State.Open)
                continue;
            p.Ws.SendText(text);
        }
    }

    private static string Str(JsonElement e, string key) =>
        e.TryGetProperty(key, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static double Num(JsonElement e, string key)
    {
        if (!e.TryGetProperty(key, out JsonElement v))
            return 0;
        if (v.ValueKind == JsonValueKind.Number)
            return Math.Round(v.GetDouble() * 1000) / 1000;
        if (v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double d))
            return d;
        return 0;
    }

    private static double[] Vec(JsonElement e, string key, int length)
    {
        var output = new double[length];
        if (e.TryGetProperty(key, out JsonElement arr) && arr.ValueKind == JsonValueKind.Array)
        {
            int i = 0;
            foreach (JsonElement v in arr.EnumerateArray())
            {
                if (i >= length)
                    break;
                output[i++] = v.ValueKind == JsonValueKind.Number ? Math.Round(v.GetDouble() * 1000) / 1000 : 0;
            }
        }
        return output;
    }

    private static string Clip(string s, int max) => string.IsNullOrEmpty(s) ? s : s.Length > max ? s[..max] : s;

    private static string SanitizeName(string raw)
    {
        var chars = (raw ?? "").Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '.' || c == '-').ToArray();
        return Clip(new string(chars).Trim(), 16);
    }
}
