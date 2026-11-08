// Scripts/Vehicle/VanController.cs
// Física de direção da van. Só lê os controles enquanto há um motorista sentado.
// A frente da van é o +Z local (convenção do VehicleBody3D).
//
// Cena: Scenes/Vehicle/van.tscn
// Van (VehicleBody3D + este script)
//  ├── *Shape (CollisionShape3D: chassi, cabine, laterais e tampa da caçamba)
//  ├── Body (malhas)          ├── Cabin/SteeringWheel (Node3D que gira com o volante)
//  ├── Wheel* (4x VehicleWheel3D)
//  ├── SeatMarker / ExitMarker (Marker3D)
//  ├── DriverDoor (Area3D + DriverSeatInteractable)
//  └── Headlights (2x SpotLight3D)
using Godot;

namespace DeadFrequency;

public partial class VanController : VehicleBody3D
{
    [ExportGroup("Motor e freio")]
    [Export] public float MaxEngineForce { get; set; } = 2600f;
    [Export] public float MaxReverseForce { get; set; } = 1300f;
    [Export] public float BrakeForce { get; set; } = 45f;
    [Export] public float HandbrakeForce { get; set; } = 90f;
    /// <summary>Freio leve quando ninguém acelera (freio-motor).</summary>
    [Export] public float CoastBrake { get; set; } = 3f;
    /// <summary>Freio quando não há motorista (van estacionada).</summary>
    [Export] public float ParkedBrake { get; set; } = 30f;
    [Export] public float MaxSpeedKmh { get; set; } = 95f;

    [ExportGroup("Direção")]
    [Export(PropertyHint.Range, "10,45,1")] public float MaxSteerDegrees { get; set; } = 32f;
    /// <summary>Velocidade de giro do volante (rad/s).</summary>
    [Export] public float SteerSpeed { get; set; } = 2.5f;
    /// <summary>Acima dessa velocidade (m/s) o esterço máximo cai pela metade.</summary>
    [Export] public float SteerFalloffSpeed { get; set; } = 25f;
    /// <summary>Quantas vezes o volante gira mais que as rodas (visual).</summary>
    [Export] public float SteeringWheelRatio { get; set; } = 5f;

    [ExportGroup("Som")]
    [Export] public AudioStream EngineSound { get; set; }
    [Export] public AudioStream DoorSound { get; set; }

    [ExportGroup("Saída")]
    /// <summary>Velocidade máxima (m/s) em que o motorista pode descer.</summary>
    [Export] public float ExitMaxSpeed { get; set; } = 2f;

    public PlayerController Driver { get; private set; }
    /// <summary>Outro jogador está dirigindo: aqui a van só segue o estado recebido.</summary>
    public bool IsRemote { get; private set; }
    /// <summary>Há um motorista remoto (para o som do motor e faróis).</summary>
    public bool HasRemoteDriver { get; set; }
    private Vector3 _remotePosition;
    private Quaternion _remoteRotation = Quaternion.Identity;
    private Vector3 _remoteVelocity;
    private bool _hasRemoteState;
    private Transform3D _lastGoodTransform;
    private bool _hasGoodTransform;
    public bool HasDriver => Driver != null;
    public float SpeedKmh => LinearVelocity.Length() * 3.6f;
    public bool CanExit => LinearVelocity.Length() <= ExitMaxSpeed;

    private Node3D _steeringWheel;
    private Node3D _headlights;
    private AudioStreamPlayer3D _engine;
    private AudioStreamPlayer3D _door;
    private float _engineLoad;
    private AudioStreamPlayer3D _crash;
    private AudioStream _crashHeavy;
    private AudioStream _crashLight;
    private Vector3 _previousVelocity;
    private double _crashCooldown;
    private AudioStreamPlayer3D _skid;
    private AudioStream _impactKenney;
    /// <summary>Rádio do painel: R liga/desliga (sintonizado na 96.6).</summary>
    public RadioPlayer Radio { get; private set; }

    public override void _Ready()
    {
        _steeringWheel = GetNodeOrNull<Node3D>("Cabin/SteeringWheel");
        _headlights = GetNodeOrNull<Node3D>("Headlights");
        if (_headlights != null)
            _headlights.Visible = false;

        EngineSound ??= ResourceLoader.Exists("res://Audio/SFX/engine_loop.wav") ? GD.Load<AudioStream>("res://Audio/SFX/engine_loop.wav") : null;
        DoorSound ??= ResourceLoader.Exists("res://Audio/SFX/van_door.wav") ? GD.Load<AudioStream>("res://Audio/SFX/van_door.wav") : null;

        _engine = new AudioStreamPlayer3D
        {
            Stream = AudioManager.MakeLooping(EngineSound),
            Bus = AudioManager.SfxBus,
            UnitSize = 8f,
            VolumeDb = -60f,
            Position = new Vector3(0f, 0.8f, 1.8f),
        };
        AddChild(_engine);

        _door = new AudioStreamPlayer3D { Stream = DoorSound, Bus = AudioManager.SfxBus, UnitSize = 5f, Position = new Vector3(0.95f, 1.3f, 1.4f) };
        AddChild(_door);

        _crashHeavy = ResourceLoader.Exists("res://Audio/SFX/crash_heavy.wav") ? GD.Load<AudioStream>("res://Audio/SFX/crash_heavy.wav") : null;
        _crashLight = ResourceLoader.Exists("res://Audio/SFX/crash_light.wav") ? GD.Load<AudioStream>("res://Audio/SFX/crash_light.wav") : null;
        _crash = new AudioStreamPlayer3D { Bus = AudioManager.SfxBus, UnitSize = 12f, MaxPolyphony = 2, Position = new Vector3(0f, 1f, 0f) };
        AddChild(_crash);

        // Sons CC0 da Kenney: pneu cantando e batida leve (ver Créditos).
        const string kenney = "res://Assets/Kenney/Racing/Audio/";
        _impactKenney = ResourceLoader.Exists(kenney + "impact.ogg") ? GD.Load<AudioStream>(kenney + "impact.ogg") : null;
        if (ResourceLoader.Exists(kenney + "skid.ogg"))
        {
            _skid = new AudioStreamPlayer3D
            {
                Stream = AudioManager.MakeLooping(GD.Load<AudioStream>(kenney + "skid.ogg")),
                Bus = AudioManager.SfxBus,
                UnitSize = 10f,
                VolumeDb = -60f,
                Position = new Vector3(0f, 0.3f, 0f),
            };
            AddChild(_skid);
        }

        Radio = new RadioPlayer { Name = "Radio", On = false, VolumeDb = -4f, UnitSize = 3f, MaxDistance = 18f, Position = new Vector3(0f, 1.25f, 1.75f) };
        AddChild(Radio);

        VanDetails.Build(this);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!HasDriver || !Driver.InputEnabled || IsRemote)
            return;
        if (@event.IsActionPressed("radio"))
        {
            Radio.Toggle();
            AudioManager.Instance?.SetMusicDucked(Radio.On);
            if (GetNodeOrNull<Label3D>("RadioDisplay") is Label3D display)
                display.Visible = Radio.On;
            GameHud.Toast(this, Radio.On ? "📻 " + Radio.NowPlaying : "Rádio desligado", UiStyle.Phosphor, 4.0);
            GetViewport().SetInputAsHandled();
        }
        else if (@event.IsActionPressed("radio_next") && Radio.On)
        {
            GameHud.Toast(this, "📻 " + Radio.NowPlaying + "  (só tem a 96.6 por aqui)", UiStyle.Phosphor, 4.0);
            GetViewport().SetInputAsHandled();
        }
    }

    /// <summary>Pneu cantando quando a van escorrega de lado (curva rápida ou freio de mão).</summary>
    private void UpdateSkid(float dt, float brake)
    {
        if (_skid == null)
            return;
        Vector3 v = LinearVelocity;
        float lateral = Mathf.Abs(v.Dot(GlobalBasis.X));
        float forward = Mathf.Abs(v.Dot(GlobalBasis.Z));
        bool grounded = GetNodeOrNull<VehicleWheel3D>("WheelRL")?.IsInContact() ?? true;
        float amount = grounded ? Mathf.Clamp((lateral - 3f) / 5f, 0f, 1f) : 0f;
        if (brake > 0.5f && forward > 6f && grounded)
            amount = Mathf.Max(amount, Mathf.Clamp((forward - 6f) / 10f, 0f, 0.8f));
        float target = amount > 0.01f ? Mathf.Lerp(-18f, -2f, amount) : -60f;
        _skid.VolumeDb = Mathf.MoveToward(_skid.VolumeDb, target, 90f * dt);
        if (amount > 0.01f && !_skid.Playing)
            _skid.Play();
        else if (_skid.Playing && _skid.VolumeDb <= -59f)
            _skid.Stop();
    }

    /// <summary>
    /// Remoto = outro jogador dirige. NÃO usamos Freeze: VehicleBody3D congelado divide por massa zero
    /// no cálculo das rodas e enche a van de NaN (era o travamento online). Em vez disso a física continua
    /// ligada, sem gravidade, e a cada passo a van é posicionada no estado recebido.
    /// </summary>
    public void SetRemote(bool remote)
    {
        if (IsRemote == remote)
            return;
        IsRemote = remote;
        Freeze = false;
        GravityScale = remote ? 0f : 1f;
        EngineForce = 0f;
        Brake = remote ? 0f : ParkedBrake;
        if (remote)
        {
            LinearVelocity = Vector3.Zero;
            AngularVelocity = Vector3.Zero;
        }
        else if (_hasRemoteState)
        {
            LinearVelocity = _remoteVelocity;
            Sleeping = false;
        }
    }

    public void SetRemoteDriver(bool driving)
    {
        HasRemoteDriver = driving;
        if (!IsInstanceValid(this))
            return;
        if (_headlights != null && IsInstanceValid(_headlights))
            _headlights.Visible = driving || HasDriver;
        if (driving && _engine != null && IsInstanceValid(_engine) && _engine.Stream != null && !_engine.Playing)
            _engine.Play();
    }

    public void ApplyRemoteState(Vector3 position, Quaternion rotation, Vector3 velocity)
    {
        if (!position.IsFinite() || !velocity.IsFinite() || !rotation.IsFinite())
            return;
        _remotePosition = position;
        _remoteRotation = rotation.Normalized();
        _remoteVelocity = velocity;
        if (!_hasRemoteState)
        {
            GlobalPosition = position;
            Quaternion = _remoteRotation;
        }
        _hasRemoteState = true;
    }

    public void PlayDoorSound()
    {
        if (_door?.Stream == null)
            return;
        _door.PitchScale = (float)GD.RandRange(0.95, 1.05);
        _door.Play();
    }

    public void SetDriver(PlayerController driver)
    {
        Driver = driver;
        if (Radio != null)
            AudioManager.Instance?.SetMusicDucked(driver != null && Radio.On);
        if (driver != null && _engine?.Stream != null && !_engine.Playing)
            _engine.Play();
        if (_headlights != null)
            _headlights.Visible = driver != null;
    }

    public override void _PhysicsProcess(double delta)
    {
        float dt = (float)delta;

        if (!GuardAgainstNaN())
            return;

        if (IsRemote)
        {
            if (_hasRemoteState)
            {
                float t = 1f - Mathf.Exp(-12f * dt);
                Vector3 position = GlobalPosition.Lerp(_remotePosition + _remoteVelocity * 0.05f, t);
                Quaternion rotation = GlobalBasis.GetRotationQuaternion().Slerp(_remoteRotation, t);
                GlobalTransform = new Transform3D(new Basis(rotation), position);
                LinearVelocity = _remoteVelocity;
                AngularVelocity = Vector3.Zero;
            }
            UpdateEngineSound(dt, 0f, _remoteVelocity.Dot(GlobalBasis.Z));
            return;
        }

        float throttle = 0f;
        float steerInput = 0f;
        bool handbrake = false;

        if (HasDriver && Driver.InputEnabled)
        {
            throttle = Input.GetAxis("move_back", "move_forward");
            steerInput = Input.GetAxis("move_right", "move_left"); // esterço positivo = esquerda
            handbrake = Input.IsActionPressed("jump");
        }

        // Velocidade no eixo da van: positiva indo para frente.
        float forwardSpeed = LinearVelocity.Dot(GlobalBasis.Z);

        float engine = 0f;
        float brake;

        if (!HasDriver)
        {
            brake = ParkedBrake;
        }
        else if (throttle > 0.05f)
        {
            // Acelerar enquanto anda de ré = frear primeiro.
            if (forwardSpeed < -0.5f) { brake = BrakeForce * throttle; }
            else { engine = MaxEngineForce * throttle; brake = 0f; }
        }
        else if (throttle < -0.05f)
        {
            if (forwardSpeed > 0.5f) { brake = BrakeForce * -throttle; }
            else { engine = MaxReverseForce * throttle; brake = 0f; }
        }
        else
        {
            brake = CoastBrake;
        }

        if (engine > 0f && SpeedKmh > MaxSpeedKmh)
            engine = 0f;
        if (handbrake)
            brake = HandbrakeForce;

        EngineForce = engine;
        Brake = brake;

        // Menos esterço em alta velocidade evita capotar.
        float falloff = Mathf.Lerp(1f, 0.5f, Mathf.Clamp(Mathf.Abs(forwardSpeed) / SteerFalloffSpeed, 0f, 1f));
        float targetSteer = steerInput * Mathf.DegToRad(MaxSteerDegrees) * falloff;
        Steering = Mathf.MoveToward(Steering, targetSteer, SteerSpeed * dt);

        UpdateEngineSound(dt, throttle, forwardSpeed);
        UpdateSkid(dt, handbrake && HasDriver ? 1f : 0f);
        DetectCrash(dt);

        if (_steeringWheel != null)
        {
            Vector3 rotation = _steeringWheel.Rotation;
            rotation.Z = -Steering * SteeringWheelRatio;
            _steeringWheel.Rotation = rotation;
        }
    }

    /// <summary>
    /// Batida = queda brusca de velocidade entre dois passos de física (sem precisar de contact monitor).
    /// Quanto maior a variação, mais forte o som.
    /// </summary>
    private void DetectCrash(float dt)
    {
        Vector3 velocity = LinearVelocity;
        Vector3 delta = velocity - _previousVelocity;
        delta.Y *= 0.3f; // pulos da suspensão não contam como batida
        float change = delta.Length();
        _previousVelocity = velocity;

        if (_crashCooldown > 0)
        {
            _crashCooldown -= dt;
            return;
        }
        if (change < 3.5f || _crash == null)
            return;

        bool heavy = change > 8f;
        _crash.Stream = heavy ? _crashHeavy : (_impactKenney != null && GD.Randf() < 0.5f ? _impactKenney : _crashLight);
        if (_crash.Stream == null)
            return;
        _crash.VolumeDb = Mathf.Lerp(-10f, 2f, Mathf.Clamp((change - 3.5f) / 12f, 0f, 1f));
        _crash.PitchScale = (float)GD.RandRange(0.9, 1.1);
        _crash.Play();
        _crashCooldown = heavy ? 0.6 : 0.3;
    }

    /// <summary>Se a física explodir (NaN/infinito), volta para a última posição boa em vez de travar o jogo.</summary>
    private bool GuardAgainstNaN()
    {
        Transform3D t = GlobalTransform;
        bool finite = t.Origin.IsFinite() && t.Basis.X.IsFinite() && t.Basis.Y.IsFinite() && t.Basis.Z.IsFinite()
                      && LinearVelocity.IsFinite() && AngularVelocity.IsFinite();
        if (finite)
        {
            _lastGoodTransform = t;
            _hasGoodTransform = true;
            return true;
        }

        GD.PushWarning("VanController: física inválida (NaN), restaurando a última posição boa.");
        LinearVelocity = Vector3.Zero;
        AngularVelocity = Vector3.Zero;
        EngineForce = 0f;
        Steering = 0f;
        if (_hasGoodTransform)
            GlobalTransform = _lastGoodTransform with { Origin = _lastGoodTransform.Origin + Vector3.Up * 0.5f };
        return false;
    }

    private void UpdateEngineSound(float dt, float throttle, float forwardSpeed)
    {
        if (_engine?.Stream == null)
            return;

        if (!HasDriver && !HasRemoteDriver)
        {
            // Motor desligado: some aos poucos.
            _engine.VolumeDb = Mathf.MoveToward(_engine.VolumeDb, -60f, 40f * dt);
            if (_engine.Playing && _engine.VolumeDb <= -59f)
                _engine.Stop();
            return;
        }

        _engineLoad = Mathf.MoveToward(_engineLoad, Mathf.Abs(throttle), 3f * dt);
        float speedFactor = Mathf.Clamp(Mathf.Abs(forwardSpeed) / 26f, 0f, 1f);
        float targetPitch = 0.55f + speedFactor * 1.1f + _engineLoad * 0.25f;
        _engine.PitchScale = Mathf.Lerp(_engine.PitchScale, targetPitch, 1f - Mathf.Exp(-6f * dt));
        float targetVolume = -10f + _engineLoad * 5f + speedFactor * 3f;
        _engine.VolumeDb = Mathf.MoveToward(_engine.VolumeDb, targetVolume, 60f * dt);
    }
}
