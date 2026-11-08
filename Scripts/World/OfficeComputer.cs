// Scripts/World/OfficeComputer.cs
// PC com monitor CRT no escritório do depósito. A tela (Label3D) mostra o caixa, o turno,
// as entregas e o histórico recente, lidos do DeliveryManager. Mire e aperte E para trocar de página.
// O CityGenerator monta o gabinete e posiciona este nó com a tela virada para +Z local.
using System.Collections.Generic;
using Godot;

namespace DeadFrequency;

public partial class OfficeComputer : Area3D, IInteractable
{
    private const int HistorySize = 6;

    /// <summary>Histórico da sessão (sobrevive à troca de página, zera ao sair para o menu).</summary>
    private static readonly List<(string Name, int Payment, string Hour)> History = new();
    private static int _sessionEarnings;
    private static int _sessionLost;
    private static DeliveryManager _watched;

    private Label3D _screen;
    private double _refresh;
    private double _blink;
    private AudioStreamPlayer3D _beep;

    public override void _Ready()
    {
        CollisionLayer = 4; // Interactables
        CollisionMask = 0;
        Monitoring = false;
        AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = new Vector3(0.55f, 0.5f, 0.5f) }, Position = new Vector3(0f, 0.22f, -0.1f) });

        var beige = new StandardMaterial3D { AlbedoColor = new Color(0.78f, 0.74f, 0.64f), Roughness = 0.8f };
        var dark = new StandardMaterial3D { AlbedoColor = new Color(0.08f, 0.08f, 0.09f), Roughness = 0.9f };
        var glow = new StandardMaterial3D
        {
            AlbedoColor = new Color(0.01f, 0.05f, 0.02f),
            EmissionEnabled = true,
            Emission = new Color(0.02f, 0.18f, 0.06f),
            EmissionEnergyMultiplier = 1f,
        };

        // Monitor CRT: caixa frontal + "traseira" afunilada.
        Add(new Vector3(0f, 0.22f, -0.05f), new Vector3(0.5f, 0.44f, 0.3f), beige);
        Add(new Vector3(0f, 0.22f, -0.32f), new Vector3(0.36f, 0.32f, 0.26f), beige);
        Add(new Vector3(0f, 0.02f, -0.12f), new Vector3(0.28f, 0.04f, 0.24f), beige); // base
        Add(new Vector3(0f, 0.235f, 0.105f), new Vector3(0.42f, 0.34f, 0.01f), dark); // moldura da tela
        Add(new Vector3(0f, 0.235f, 0.112f), new Vector3(0.38f, 0.3f, 0.005f), glow); // tela acesa
        Add(new Vector3(0.18f, 0.04f, 0.102f), new Vector3(0.03f, 0.02f, 0.01f), new StandardMaterial3D
        {
            AlbedoColor = new Color(0.2f, 1f, 0.3f), EmissionEnabled = true, Emission = new Color(0.2f, 1f, 0.3f),
        }); // LED

        // Gabinete deitado embaixo do monitor? Fica ao lado, em pé, como nos anos 90.
        Add(new Vector3(0.5f, 0.2f, -0.15f), new Vector3(0.2f, 0.42f, 0.45f), beige);
        Add(new Vector3(0.5f, 0.32f, 0.08f), new Vector3(0.14f, 0.03f, 0.01f), dark); // drive de disquete
        // Teclado e mouse.
        Add(new Vector3(0f, -0.01f, 0.32f), new Vector3(0.46f, 0.03f, 0.16f), beige);
        Add(new Vector3(0.33f, -0.01f, 0.33f), new Vector3(0.06f, 0.025f, 0.1f), beige);

        _screen = new Label3D
        {
            Position = new Vector3(0f, 0.235f, 0.117f),
            PixelSize = 0.00045f,
            FontSize = 64,
            OutlineSize = 0,
            Modulate = new Color(0.45f, 1f, 0.55f),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Width = 800f,
            AutowrapMode = TextServer.AutowrapMode.WordSmart, // a largura só vale com quebra ligada (senão sai deslocado)
            Shaded = false,
            DoubleSided = false,
            FixedSize = false,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Linear,
            // Fonte monoespaçada do sistema para as colunas do "relatório" alinharem.
            Font = new SystemFont { FontNames = new[] { "Consolas", "Courier New", "Lucida Console", "monospace" } },
        };
        AddChild(_screen);

        _beep = new AudioStreamPlayer3D { Bus = AudioManager.SfxBus, UnitSize = 3f, Position = new Vector3(0f, 0.2f, 0f) };
        if (ResourceLoader.Exists("res://Audio/SFX/pc_beep.wav"))
            _beep.Stream = GD.Load<AudioStream>("res://Audio/SFX/pc_beep.wav");
        AddChild(_beep);
        Refresh();
    }

    private void Add(Vector3 pos, Vector3 size, Material mat) =>
        AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = size }, Position = pos, MaterialOverride = mat });

    public override void _Process(double delta)
    {
        _blink += delta;
        _refresh -= delta;
        if (_refresh > 0)
            return;
        _refresh = 0.5;
        Refresh();
    }

    private static DeliveryManager Manager(Node from)
    {
        var delivery = from.GetTree().GetFirstNodeInGroup("delivery_manager") as DeliveryManager;
        if (delivery != null && delivery != _watched)
        {
            // Fase nova: zera o histórico e passa a ouvir este gerente.
            _watched = delivery;
            History.Clear();
            _sessionEarnings = 0;
            _sessionLost = 0;
            delivery.DeliveryCompleted += (name, payment) =>
            {
                _sessionEarnings += payment;
                History.Insert(0, (name, payment, DayNightController.ClockText()[..5]));
                if (History.Count > HistorySize)
                    History.RemoveAt(History.Count - 1);
            };
            delivery.CargoLost += name =>
            {
                _sessionLost++;
                History.Insert(0, (name, -1, DayNightController.ClockText()[..5]));
                if (History.Count > HistorySize)
                    History.RemoveAt(History.Count - 1);
            };
        }
        return delivery;
    }

    private void Refresh()
    {
        DeliveryManager d = Manager(this);
        bool cursorOn = (int)(_blink * 2) % 2 == 0;
        string clock = DayNightController.ClockText()[..5];
        if (d == null)
        {
            _screen.Text = "DFE-DOS 6.22\n\nC:\\> " + (cursorOn ? "_" : "");
            return;
        }
        // A tela no mundo mostra só o resumo em letras grandes; o relatório completo abre com E.
        _screen.Text =
            $"DFE ENTREGAS {clock}\n" +
            $"CAIXA  $ {d.Money}\n" +
            $"TURNO  {d.Shift}  ({d.ShiftDelivered}/{d.ShiftTotal})\n" +
            $"GANHO  $ {d.ShiftEarnings}\n" +
            $"[E] ABRIR {(cursorOn ? "_" : " ")}";
    }

    /// <summary>Relatório completo (usado pela tela cheia do computador).</summary>
    public static string Report(int page, bool cursorOn)
    {
        DeliveryManager d = _watched;
        string cursor = cursorOn ? "_" : " ";
        string clock = DayNightController.ClockText();
        if (d == null || !IsInstanceValid(d))
            return "DFE-DOS 6.22\n\nC:\\> " + cursor;

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"DEAD FREQUENCY ENTREGAS LTDA          {clock}");
        sb.AppendLine("==================================================");
        if (page == 0)
        {
            sb.AppendLine("C:\\FINANCAS> RELAT.EXE");
            sb.AppendLine();
            sb.AppendLine($"  CAIXA DA EMPRESA ............ $ {d.Money,8}");
            sb.AppendLine($"  GANHOS NESTA SESSAO ......... $ {_sessionEarnings,8}");
            sb.AppendLine();
            sb.AppendLine($"  TURNO ATUAL ................. {d.Shift,10}");
            sb.AppendLine($"  ENTREGAS DO TURNO ........... {d.ShiftDelivered,6} / {d.ShiftTotal}");
            sb.AppendLine($"  GANHO DO TURNO .............. $ {d.ShiftEarnings,8}");
            sb.AppendLine($"  TOTAL ENTREGUE .............. {d.TotalDelivered,10}");
            sb.AppendLine($"  CARGAS PERDIDAS ............. {_sessionLost,10}");
            sb.AppendLine();
            sb.AppendLine("  " + Bar(d.ShiftDelivered, Mathf.Max(1, d.ShiftTotal)));
        }
        else
        {
            sb.AppendLine("C:\\FINANCAS> HISTOR.EXE");
            sb.AppendLine();
            if (History.Count == 0)
                sb.AppendLine("  nenhuma entrega registrada ainda.");
            foreach ((string name, int payment, string hour) in History)
            {
                string n = name.Length > 24 ? name[..24] : name;
                sb.AppendLine(payment >= 0 ? $"  {hour}  {n,-24}  +$ {payment,6}" : $"  {hour}  {n,-24}  PERDIDA");
            }
            sb.AppendLine();
            sb.AppendLine("  PEDIDOS ABERTOS:");
            foreach (DeliveryJob job in d.Jobs)
                sb.AppendLine($"   - {job.CargoName} -> {job.Point.Address}");
        }
        sb.AppendLine("==================================================");
        sb.Append($"[TAB/E] trocar pagina   [ESC] sair   C:\\> {cursor}");
        return sb.ToString();
    }

    private static string Bar(int value, int max)
    {
        const int cells = 24;
        int filled = Mathf.Clamp(value * cells / max, 0, cells);
        return "TURNO [" + new string('#', filled) + new string('.', cells - filled) + "]";
    }

    public string GetInteractionPrompt(PlayerController player) => "[E] Usar o computador";

    public void Interact(PlayerController player)
    {
        _beep.Play();
        ComputerScreen.OpenFor(this, player);
    }
}
