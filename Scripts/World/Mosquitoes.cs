// Scripts/World/Mosquitoes.cs
// Pernilongos do sítio: simulação silenciosa temporária enquanto o zumbido é refeito.
using Godot;

namespace DeadFrequency;

public partial class Mosquitoes : Node
{
    private float _timer = 20f, _activityLeft;
    private bool _toldDog;
    private readonly RandomNumberGenerator _rng = new();

    public override void _Ready()
    {
        _rng.Randomize();
    }

    /// <summary>Segundos restantes do repelente (Agro-Tudão). Enquanto durar, nada de pernilongo.</summary>
    public static float RepelLeft { get; private set; }

    /// <summary>Passa o repelente. Retorna a mensagem do HUD.</summary>
    public static string Repel(float seconds)
    {
        RepelLeft = seconds;
        return "Repelente passado: 5 minutos sem muriçoca.";
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        if (RepelLeft > 0f)
            RepelLeft = Mathf.Max(0f, RepelLeft - dt);

        PlayerController player = ProfessionManager.Instance?.Player;
        float hour = DayNightController.Hour;
        bool night = hour >= 19f || hour < 5f;
        bool onFoot = player != null
            && IsInstanceValid(player)
            && player.State == PlayerState.Walking
            && player.GlobalPosition.Y > -5f;
        bool dog = FarmAnimal.Companion != null
            && player != null
            && FarmAnimal.Companion.GlobalPosition.DistanceTo(player.GlobalPosition) < 12f;

        // Mantém a janela silenciosa para o repelente e para o cachorro ainda poder espantar
        // os pernilongos; nenhum AudioStreamPlayer é criado ou reproduzido.
        if (_activityLeft > 0f)
        {
            _activityLeft = Mathf.Max(0f, _activityLeft - dt);
            if (dog && !_toldDog)
            {
                _toldDog = true;
                GameHud.Toast(this, "Au! Au! O caramelo espantou os pernilongos.", new Color(1f, 0.85f, 0.5f), 3.0);
            }

            if (dog || !onFoot || RepelLeft > 0f)
                _activityLeft = 0f;

            if (_activityLeft > 0f)
                return;
        }

        _timer -= dt;
        if (_timer > 0f)
            return;

        _timer = _rng.RandfRange(25f, 60f);
        if (!night || !onFoot || dog || RepelLeft > 0f)
            return;

        _activityLeft = _rng.RandfRange(5f, 10f);
        _toldDog = false;
    }
}
