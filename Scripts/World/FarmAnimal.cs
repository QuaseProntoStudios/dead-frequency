// Scripts/World/FarmAnimal.cs
// Bicho da roça animado (Quaternius: Ultimate Animated Animals e Farm Animals, CC0).
// Fica parado comendo/olhando em volta e, se tiver raio de passeio, anda devagar pelo cercado.
// O tamanho e a direção da cabeça são medidos pelos ossos, então qualquer modelo do pacote serve.
using Godot;

namespace DeadFrequency;

public partial class FarmAnimal : Node3D
{
    public const string Dir = "res://Assets/Quaternius/Animals/";

    /// <summary>Arquivo do modelo (Cow.gltf, Pig.fbx...).</summary>
    public string Model { get; set; } = "Cow.gltf";
    /// <summary>Comprimento do bicho em metros (do focinho ao rabo).</summary>
    public float Length { get; set; } = 2.3f;
    /// <summary>Raio de passeio em volta do ponto inicial (0 = fica no lugar).</summary>
    public float WanderRadius { get; set; }
    public float WalkSpeed { get; set; } = 0.55f;

    private Node3D _body;
    private AnimationPlayer _anim;
    private string _idle, _walk;
    private string[] _idles = System.Array.Empty<string>();
    private Vector3 _home, _target;
    private bool _walking;
    private float _timer;
    private readonly RandomNumberGenerator _rng = new();

    public static FarmAnimal Create(string model, float length, Vector3 position, float rotY, float wander = 0f) =>
        new() { Model = model, Length = length, Position = position, Rotation = new Vector3(0f, rotY, 0f), WanderRadius = wander, Name = model.Split('.')[0] };

    public override void _Ready()
    {
        _rng.Seed = (ulong)AssetProps.Hash(GlobalPosition, 9) + 1;
        _home = Position;
        if (AssetProps.Load(Dir + Model)?.Instantiate() is not Node3D body)
            return;
        _body = body;
        AddChild(body);
        FitToSize();
        _anim = body.FindChild("AnimationPlayer", true, false) as AnimationPlayer;
        if (_anim != null)
        {
            var idles = new System.Collections.Generic.List<string>();
            foreach (string a in _anim.GetAnimationList())
            {
                string s = a.Contains('|') ? a[(a.IndexOf('|') + 1)..] : a;
                bool loop = true;
                if (s is "Idle" or "Idle_2" or "Idle_Headlow" or "Eating")
                    idles.Add(a);
                else if (s is "Walk" or "Walking")
                    _walk = a;
                else
                    loop = false;
                if (loop)
                    _anim.GetAnimation(a).LoopMode = Animation.LoopModeEnum.Linear;
                if (s == "Idle")
                    _idle = a;
            }
            _idles = idles.ToArray();
            _idle ??= _idles.Length > 0 ? _idles[0] : null;
            if (_idle != null)
            {
                _anim.Play(_idle);
                _anim.Seek(_rng.Randf() * (float)_anim.GetAnimation(_idle).Length, true);
            }
        }
        _timer = _rng.RandfRange(2f, 8f);
    }

    /// <summary>Mede o esqueleto em repouso: escala para o comprimento pedido e vira a cabeça para -Z.</summary>
    private void FitToSize()
    {
        var skeleton = _body.FindChild("Skeleton3D", true, false) as Skeleton3D;
        if (skeleton == null || skeleton.GetBoneCount() == 0)
            return;
        Transform3D toLocal = GlobalTransform.AffineInverse() * skeleton.GlobalTransform;
        var min = new Vector3(1e9f, 1e9f, 1e9f);
        var max = -min;
        Vector3 head = Vector3.Zero;
        bool hasHead = false;
        for (int i = 0; i < skeleton.GetBoneCount(); i++)
        {
            Vector3 p = toLocal * skeleton.GetBoneGlobalRest(i).Origin;
            min = min.Min(p);
            max = max.Max(p);
            if (!hasHead && skeleton.GetBoneName(i).ToLowerInvariant().Contains("head"))
            {
                head = p;
                hasHead = true;
            }
        }
        Vector3 size = max - min;
        float horizontal = Mathf.Max(size.X, size.Z);
        if (horizontal < 0.0001f)
            return;
        // Os ossos vão do focinho à base do rabo; a malha passa um pouco disso.
        float k = Length / (horizontal * 1.1f);
        _body.Scale *= k;
        Vector3 center = (min + max) * 0.5f;
        if (hasHead)
        {
            Vector3 dir = head - center;
            dir.Y = 0f;
            if (dir.LengthSquared() > 1e-8f)
                _body.Rotation = new Vector3(0f, Mathf.Atan2(dir.X, dir.Z) - Mathf.Pi, 0f) + _body.Rotation;
        }
        // Apoia os cascos no chão (o osso mais baixo fica em y = 0).
        _body.Position = new Vector3(0f, -min.Y * k, 0f);
    }

    public override void _Process(double delta)
    {
        if (_anim == null)
            return;
        float dt = (float)delta;
        _timer -= dt;
        if (_walking)
        {
            Vector3 to = _target - Position;
            to.Y = 0f;
            if (to.Length() < 0.2f || _timer <= 0f)
            {
                _walking = false;
                _timer = _rng.RandfRange(4f, 12f);
                PlayIdle();
                return;
            }
            float yaw = Mathf.Atan2(-to.X, -to.Z);
            Rotation = new Vector3(0f, Mathf.LerpAngle(Rotation.Y, yaw, 1f - Mathf.Exp(-3f * dt)), 0f);
            Vector3 fwd = -Transform.Basis.Z;
            Position += fwd * WalkSpeed * dt;
            return;
        }
        if (_timer > 0f)
            return;
        if (WanderRadius > 0.5f && _walk != null && _rng.Randf() < 0.45f)
        {
            float a = _rng.Randf() * Mathf.Tau, r = _rng.RandfRange(0.5f, WanderRadius);
            _target = _home + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            _walking = true;
            _timer = 12f;
            _anim.Play(_walk, 0.3f);
        }
        else
        {
            PlayIdle();
            _timer = _rng.RandfRange(4f, 10f);
        }
    }

    private void PlayIdle()
    {
        if (_idles.Length == 0)
            return;
        _anim.Play(_idles[_rng.RandiRange(0, _idles.Length - 1)], 0.4f);
    }
}
