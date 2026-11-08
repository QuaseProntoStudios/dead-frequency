// Scripts/Jobs/FrentistaJob.cs
// Frentista: um carro entra no posto mais perto de você e para na bomba. Mire na tampa do tanque,
// aperte E e fique por perto até encher (4 s). R$ 45 por tanque.
using Godot;

namespace DeadFrequency;

public partial class FrentistaJob : JobBase
{
    private static readonly Color Green = new(0.3f, 0.9f, 0.4f);
    private const float FillSeconds = 4f;

    private Node3D _car;
    private Node3D _marker;
    private Label3D _label;
    private string _brand = "";
    private Vector3 _spot;
    private bool _arrived;
    private bool _filling;
    private float _fill;
    private double _next = 2.0;

    public override void _ExitTree()
    {
        JobMarkers.Free(ref _marker);
        if (_car != null && IsInstanceValid(_car))
            _car.QueueFree();
    }

    private static string BrandName(string id) => id switch
    {
        "petrobraz" => "Petrobraz",
        "ipiranguinha" => "Ipiranguinha",
        "shello" => "Shello",
        _ => id,
    };

    private void SpawnCar()
    {
        if (City.GasSpots.Count == 0)
            return;
        // Posto mais perto de você.
        int best = 0;
        for (int i = 1; i < City.GasSpots.Count; i++)
            if (PlayerPos.DistanceTo(City.GasSpots[i].Position) < PlayerPos.DistanceTo(City.GasSpots[best].Position))
                best = i;
        (_spot, _brand) = City.GasSpots[best];

        _car = new StaticBody3D { Name = "CarroPosto", CollisionLayer = 1, CollisionMask = 0 };
        AddChild(_car);
        var type = (CarType)Rng.RandiRange(0, 3);
        Vector3 size = CarModels.Build(_car, type, Color.FromHsv(Rng.Randf(), 0.55f, 0.6f));
        _car.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = size }, Position = new Vector3(0f, size.Y * 0.5f, 0f) });
        // Frente do carro = -Z: entra vindo da avenida (+Z) e para na bomba.
        _car.GlobalPosition = _spot + new Vector3(0f, 0f, 24f);
        _arrived = false;
        _filling = false;
        _fill = 0f;
        Tween tw = _car.CreateTween();
        tw.TweenProperty(_car, "global_position", _spot, 3.5).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        tw.TweenCallback(Callable.From(OnArrived));

        _label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            FontSize = 48,
            PixelSize = 0.007f,
            Modulate = Green,
            OutlineSize = 8,
            NoDepthTest = true,
            Position = new Vector3(0f, size.Y + 0.9f, 0f),
            Text = "",
        };
        _car.AddChild(_label);

        // Tampa do tanque (lado direito, atrás).
        ActionArea.Create(_car, new Vector3(size.X * 0.5f + 0.15f, 0.8f, size.Z * 0.25f), new Vector3(0.6f, 0.8f, 1.2f),
            _ => _filling ? $"Abastecendo... {_fill / FillSeconds * 100f:0}%" : "[E] Abastecer o carro",
            _ => StartFill(),
            p => _arrived && !_filling && p.CurrentVehicle == null);
        _car.AddChild(new MeshInstance3D { Mesh = new BoxMesh { Size = new Vector3(0.02f, 0.14f, 0.14f) }, MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.1f, 0.1f, 0.1f) }, Position = new Vector3(size.X * 0.5f + 0.01f, 0.8f, size.Z * 0.25f) });
    }

    private void OnArrived()
    {
        _arrived = true;
        if (_label != null)
            _label.Text = "ABASTECER (E)";
        JobMarkers.Free(ref _marker);
        _marker = JobMarkers.Create(this, _spot + new Vector3(3f, 0f, 0f), Green, "", 30f, 0.6f);
    }

    private void StartFill()
    {
        _filling = true;
        GameHud.Toast(this, "Abastecendo... fique perto da bomba.", Green, 2.0);
    }

    public override void _Process(double delta)
    {
        if (_car == null || !IsInstanceValid(_car))
        {
            _next -= delta;
            if (_next <= 0)
                SpawnCar();
            return;
        }
        if (!_filling)
            return;
        if (PlayerPos.DistanceTo(_car.GlobalPosition) > 5f)
        {
            _filling = false;
            if (_label != null)
                _label.Text = "VOLTE PARA A BOMBA (E)";
            return;
        }
        _fill += (float)delta;
        if (_label != null)
            _label.Text = $"ABASTECENDO {_fill / FillSeconds * 100f:0}%";
        if (_fill >= FillSeconds)
            Finish();
    }

    private void Finish()
    {
        _filling = false;
        _arrived = false;
        JobMarkers.Free(ref _marker);
        M.Earn(1, $"Tanque cheio no posto {BrandName(_brand)}");
        if (_label != null)
            _label.Text = "VALEU, CHEFE!";
        Node3D car = _car;
        _car = null;
        Tween tw = car.CreateTween();
        tw.TweenInterval(0.8);
        tw.TweenProperty(car, "global_position", _spot + new Vector3(0f, 0f, 30f), 3.0).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.In);
        tw.TweenCallback(Callable.From(car.QueueFree));
        _next = Rng.RandfRange(5f, 10f);
    }

    public override string PanelText()
    {
        if (City.GasSpots.Count == 0)
            return "Sem postos nesta fase.";
        if (_car == null || !IsInstanceValid(_car))
            return $"· Esperando o próximo carro... ({Mathf.Max(0, _next):0} s)";
        float d = PlayerPos.DistanceTo(_car.GlobalPosition);
        if (!_arrived && !_filling)
            return $"· Carro chegando no posto {BrandName(_brand)} ({d:0} m)";
        return _filling ? $"► Abastecendo: {_fill / FillSeconds * 100f:0}%" : $"► Carro esperando no posto {BrandName(_brand)} ({d:0} m)\n  Mire na tampa do tanque e aperte E";
    }
}
