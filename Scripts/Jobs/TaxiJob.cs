// Scripts/Jobs/TaxiJob.cs
// Taxista: um passageiro espera na calçada (feixe amarelo). Pare a van perto dele, ele embarca e diz
// o destino; pare perto do destino para receber a corrida (R$ 30 + pela distância, até R$ 400).
using Godot;

namespace DeadFrequency;

public partial class TaxiJob : JobBase
{
    private static readonly Color Yellow = new(1f, 0.85f, 0.2f);
    private static readonly string[] Lines =
    {
        "Me leva na {0}, por favor. E abaixa esse rádio... ou não, pode deixar.",
        "{0}. Rapidinho que eu tô atrasado pro plantão.",
        "Toca pra {0}! Essa van é segura mesmo?",
        "Boa noite. {0}. Não passa pela Rua Estática que lá tem coisa estranha.",
        "Vai pra {0}? Eu pago em dinheiro... trocado, tá?",
    };

    private int _pickup = -1;
    private int _dest = -1;
    private bool _riding;
    private CharacterModel _passenger;
    private Node3D _marker;
    private double _wait;
    private float _anim;

    public override void _Ready() => NewPassenger();

    public override void _ExitTree()
    {
        JobMarkers.Free(ref _marker);
        if (_passenger != null && IsInstanceValid(_passenger))
            _passenger.QueueFree();
    }

    private void NewPassenger()
    {
        _riding = false;
        _pickup = RandomPoint(PlayerPos, 60f, 320f);
        if (_pickup < 0)
            return;
        DeliveryPoint p = City.DeliveryPoints[_pickup];
        var rng = new RandomNumberGenerator();
        rng.Randomize();
        _passenger = CharacterModel.Create(
            Color.FromHsv(rng.Randf(), 0.5f, 0.7f),
            Color.FromHsv(rng.Randf(), 0.3f, 0.3f),
            new Color(0.55f + rng.Randf() * 0.3f, 0.4f + rng.Randf() * 0.2f, 0.3f + rng.Randf() * 0.15f));
        AddChild(_passenger);
        _passenger.GlobalPosition = p.Position;
        JobMarkers.Free(ref _marker);
        _marker = JobMarkers.Create(this, p.Position, Yellow, "PASSAGEIRO");
    }

    public override void _Process(double delta)
    {
        _anim += (float)delta;
        if (_wait > 0)
        {
            _wait -= delta;
            if (_wait <= 0)
                NewPassenger();
            return;
        }
        if (_pickup < 0 || City == null)
            return;

        VanController van = Player?.CurrentVehicle;
        if (!_riding && _passenger != null && IsInstanceValid(_passenger))
        {
            // Passageiro acena e olha para a van.
            _passenger.Animate((float)delta, 0f);
            _passenger.SetHeadPitch(Mathf.Sin(_anim * 2f) * 0.2f);
            if (van != null)
            {
                Vector3 to = van.GlobalPosition - _passenger.GlobalPosition;
                to.Y = 0f;
                if (to.LengthSquared() > 0.01f)
                    _passenger.Rotation = new Vector3(0f, Mathf.Atan2(-to.X, -to.Z), 0f);
            }
            if (van != null && van.GlobalPosition.DistanceTo(City.DeliveryPoints[_pickup].Position) < 9f && van.SpeedKmh < 8f)
                Board();
        }
        else if (_riding && van != null && van.GlobalPosition.DistanceTo(City.DeliveryPoints[_dest].Position) < 9f && van.SpeedKmh < 8f)
        {
            Arrive(van);
        }
    }

    private void Board()
    {
        _riding = true;
        _passenger.Visible = false;
        _dest = RandomPoint(City.DeliveryPoints[_pickup].Position, 140f, 600f, _pickup);
        DeliveryPoint d = City.DeliveryPoints[_dest];
        JobMarkers.Free(ref _marker);
        _marker = JobMarkers.Create(this, d.Position, Yellow, "DESTINO");
        string line = string.Format(Lines[Rng.RandiRange(0, Lines.Length - 1)], d.Address);
        GameHud.Toast(this, "Passageiro: \"" + line + "\"", Colors.White, 6.0);
    }

    private int Fare() => Mathf.Clamp(30 + Mathf.RoundToInt(City.DeliveryPoints[_pickup].Position.DistanceTo(City.DeliveryPoints[_dest].Position) * 0.5f), 30, 400);

    private void Arrive(VanController van)
    {
        int fare = Fare();
        M.Earn(1, "Corrida concluída", fare);
        JobMarkers.Free(ref _marker);
        if (_passenger != null && IsInstanceValid(_passenger))
        {
            // Desce ao lado da van e vai embora a pé.
            _passenger.Visible = true;
            _passenger.GlobalPosition = City.DeliveryPoints[_dest].Position;
            CharacterModel p = _passenger;
            _passenger = null;
            Tween tw = p.CreateTween();
            Vector3 away = p.GlobalPosition + new Vector3(Rng.RandfRange(-6f, 6f), 0f, Rng.RandfRange(-6f, 6f));
            tw.TweenProperty(p, "global_position", away, 4.0);
            tw.TweenCallback(Callable.From(p.QueueFree));
        }
        _riding = false;
        _pickup = -1;
        _wait = 3.0;
    }

    public override bool TryGetTarget(out Vector3 position, out string label)
    {
        position = Vector3.Zero;
        label = "";
        if (City == null)
            return false;
        if (Player?.CurrentVehicle == null && M.Van != null && IsInstanceValid(M.Van))
        {
            position = M.Van.GlobalPosition;
            label = "VAN";
            return true;
        }
        if (_riding && _dest >= 0)
        {
            position = City.DeliveryPoints[_dest].Position;
            label = "DESTINO";
            return true;
        }
        if (_pickup >= 0)
        {
            position = City.DeliveryPoints[_pickup].Position;
            label = "PASSAGEIRO";
            return true;
        }
        return false;
    }

    public override string PanelText()
    {
        if (_pickup < 0 || City == null)
            return "Procurando passageiro...";
        bool inVan = Player?.CurrentVehicle != null;
        string van = inVan ? "" : "\n(Entre na van para fazer as corridas.)";
        if (!_riding)
        {
            DeliveryPoint p = City.DeliveryPoints[_pickup];
            return $"· Passageiro esperando em {p.Address} ({PlayerPos.DistanceTo(p.Position):0} m)\n  Pare a van do lado dele.{van}";
        }
        DeliveryPoint d = City.DeliveryPoints[_dest];
        return $"► Levando passageiro para {d.Address} ({PlayerPos.DistanceTo(d.Position):0} m)\n  Corrida: $ {Fare()}{van}";
    }
}
