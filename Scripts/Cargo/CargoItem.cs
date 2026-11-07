// Scripts/Cargo/CargoItem.cs
// Caixa física de entrega. Pode ser pega, arremessada e sofre dano por impacto.
// Cena: CargoBox (RigidBody3D + este script) > CollisionShape3D + MeshInstance3D
using Godot;

namespace DeadFrequency;

public partial class CargoItem : RigidBody3D, IInteractable
{
    [Signal] public delegate void IntegrityChangedEventHandler(float integrity);
    [Signal] public delegate void CargoDestroyedEventHandler();

    [Export] public CargoData Data { get; set; }

    /// <summary>Velocidade relativa mínima (m/s) para um impacto causar dano.</summary>
    [Export(PropertyHint.Range, "0.5,20,0.1,suffix:m/s")]
    public float ImpactThreshold { get; set; } = 3.5f;

    /// <summary>Dano por m/s acima do limite, antes de multiplicar pela fragilidade.</summary>
    [Export] public float DamagePerMs { get; set; } = 0.08f;

    /// <summary>Evita que um único choque conte como vários (quiques).</summary>
    [Export] public float DamageCooldown { get; set; } = 0.15f;

    /// <summary>Som tocado em batidas (volume proporcional à força).</summary>
    [Export] public AudioStream ImpactSound { get; set; }

    public float Integrity { get; private set; } = 1f;

    /// <summary>Id no multiplayer (ex.: c2_0). Vazio no modo solo.</summary>
    public string NetId { get; set; } = "";
    /// <summary>Outro jogador controla esta caixa: aqui ela só segue o estado recebido.</summary>
    public bool IsRemote { get; private set; }
    private Vector3 _remotePosition;
    private Quaternion _remoteRotation = Quaternion.Identity;
    private Vector3 _remoteVelocity;
    private bool _hasRemoteState;
    public bool IsHeld { get; private set; }
    public float WeightKg => Data?.WeightKg ?? Mass;

    private Vector3 _lastLinearVelocity;
    private double _damageCooldownLeft;

    private AudioStreamPlayer3D _impactPlayer;
    private double _soundCooldownLeft;

    private float _defaultGravityScale;
    private float _defaultAngularDamp;
    private bool _defaultCanSleep;

    public const string GroupName = "cargo";

    public override void _EnterTree() => AddToGroup(GroupName);

    public override void _Ready()
    {
        if (Data == null)
            GD.PushWarning($"{Name}: CargoItem sem CargoData atribuído no Inspector.");
        else
        {
            Mass = Data.WeightKg;
            CargoVisuals.Apply(this);
        }

        // Necessário para BodyEntered funcionar em RigidBody3D.
        ContactMonitor = true;
        MaxContactsReported = Mathf.Max(MaxContactsReported, 4);
        // Evita a caixa atravessar paredes finas da van em alta velocidade.
        ContinuousCd = true;

        _defaultGravityScale = GravityScale;
        _defaultAngularDamp = AngularDamp;
        _defaultCanSleep = CanSleep;

        ImpactSound ??= ResourceLoader.Exists("res://Audio/SFX/impact_box.wav") ? GD.Load<AudioStream>("res://Audio/SFX/impact_box.wav") : null;
        _impactPlayer = new AudioStreamPlayer3D { Stream = ImpactSound, Bus = AudioManager.SfxBus, UnitSize = 4f, MaxPolyphony = 2 };
        AddChild(_impactPlayer);

        BodyEntered += OnBodyEntered;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (IsRemote)
        {
            FollowRemote((float)delta);
            return;
        }

        // Guardamos a velocidade ANTES do passo de física:
        // quando BodyEntered dispara, essa é a velocidade pré-impacto.
        _lastLinearVelocity = LinearVelocity;

        if (_damageCooldownLeft > 0)
            _damageCooldownLeft -= delta;
        if (_soundCooldownLeft > 0)
            _soundCooldownLeft -= delta;
    }

    // ---------------- Multiplayer ----------------

    /// <summary>true = congela a física local e passa a seguir o dono remoto.</summary>
    public void SetRemote(bool remote)
    {
        if (IsRemote == remote)
            return;
        IsRemote = remote;
        FreezeMode = FreezeModeEnum.Kinematic;
        Freeze = remote;
        if (!remote && _hasRemoteState)
        {
            LinearVelocity = _remoteVelocity;
            Sleeping = false;
        }
    }

    public void ApplyRemoteState(Vector3 position, Quaternion rotation, Vector3 velocity, float integrity)
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

        if (integrity > Integrity + 0.001f)
            Integrity = Mathf.Min(1f, integrity); // outro jogador reparou
        else if (integrity >= 0f && integrity < Integrity - 0.001f)
        {
            Integrity = integrity;
            EmitSignal(SignalName.IntegrityChanged, Integrity);
            if (Integrity <= 0f)
                EmitSignal(SignalName.CargoDestroyed);
        }
    }

    private void FollowRemote(float dt)
    {
        if (!_hasRemoteState)
            return;
        float t = 1f - Mathf.Exp(-14f * dt);
        Vector3 target = _remotePosition + _remoteVelocity * 0.05f;
        GlobalPosition = GlobalPosition.Lerp(target, t);
        Quaternion = Quaternion.Slerp(_remoteRotation, t);
    }

    // ---------------- IInteractable ----------------

    public string GetInteractionPrompt(PlayerController player)
    {
        string name = Data?.CargoName ?? "Caixa";
        if (WeightKg > player.MaxCarryWeightKg)
            return $"{name} — pesado demais ({WeightKg:0.#} kg)";
        return $"Pegar {name} ({WeightKg:0.#} kg)";
    }

    public bool CanInteract(PlayerController player) =>
        !IsHeld && WeightKg <= player.MaxCarryWeightKg;

    public void Interact(PlayerController player) => player.TryGrab(this);

    // ---------------- Pegar / soltar ----------------

    /// <summary>Chamado pelo PlayerController. Não chame diretamente.</summary>
    private PlayerController _holder;

    public void OnGrabbed(PlayerController player)
    {
        _holder = player;
        IsHeld = true;
        GravityScale = 0f;      // o player controla a velocidade enquanto segura
        AngularDamp = 8f;       // para de girar na mão
        CanSleep = false;
        Sleeping = false;
        AddCollisionExceptionWith(player); // não empurra/levanta o próprio player
    }

    /// <summary>Chamado pelo PlayerController. Não chame diretamente.</summary>
    public void OnReleased(PlayerController player)
    {
        IsHeld = false;
        GravityScale = _defaultGravityScale;
        AngularDamp = _defaultAngularDamp;
        CanSleep = _defaultCanSleep;
        RemoveCollisionExceptionWith(player);
    }

    // ---------------- Dano ----------------

    public int GetCurrentPayment() =>
        Data == null ? 0 : Mathf.RoundToInt(Data.BasePayment * Integrity);

    private void OnBodyEntered(Node body)
    {
        // VehicleBody3D herda de RigidBody3D, então a van entra no primeiro caso.
        Vector3 otherVelocity = body switch
        {
            RigidBody3D rb => rb.LinearVelocity,
            CharacterBody3D cb => cb.Velocity,
            _ => Vector3.Zero,
        };

        float impactSpeed = (_lastLinearVelocity - otherVelocity).Length();
        PlayImpactSound(impactSpeed);

        if (Data == null || _damageCooldownLeft > 0 || Integrity <= 0f)
            return;
        if (impactSpeed < ImpactThreshold)
            return;

        float damage = (impactSpeed - ImpactThreshold) * DamagePerMs * Data.Fragility;
        if (IsHeld)
            damage *= _holder?.HeldDamageMultiplier ?? 0.5f; // segurando dói menos; luvas protegem mais

        ApplyDamage(damage);
        _damageCooldownLeft = DamageCooldown;
    }

    private void PlayImpactSound(float impactSpeed)
    {
        if (_impactPlayer?.Stream == null || impactSpeed < 1.2f || _soundCooldownLeft > 0)
            return;
        float strength = Mathf.Clamp((impactSpeed - 1.2f) / 8f, 0f, 1f);
        _impactPlayer.VolumeDb = Mathf.Lerp(-20f, 0f, strength);
        // Caixas pesadas soam mais graves.
        _impactPlayer.PitchScale = Mathf.Clamp(1.35f - WeightKg / 60f, 0.6f, 1.4f) * (float)GD.RandRange(0.92, 1.08);
        _impactPlayer.Play();
        _soundCooldownLeft = 0.08;
    }

    /// <summary>Kit de reparo / fita adesiva.</summary>
    public void Repair(float amount)
    {
        if (Integrity <= 0f || amount <= 0f)
            return;
        Integrity = Mathf.Min(1f, Integrity + amount);
        EmitSignal(SignalName.IntegrityChanged, Integrity);
    }

    public void ApplyDamage(float amount)
    {
        if (amount <= 0f || Integrity <= 0f)
            return;

        Integrity = Mathf.Max(0f, Integrity - amount);
        EmitSignal(SignalName.IntegrityChanged, Integrity);

        if (Integrity <= 0f)
            EmitSignal(SignalName.CargoDestroyed);
    }
}
