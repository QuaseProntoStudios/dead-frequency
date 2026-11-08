// Scripts/Player/PlayerController.Needs.cs
// v0.9.5 — "Calçada viva": fôlego (correr cansa), leseira da madrugada, comidas e bebidas,
// e os Chinelos Legítimas (ploc-ploc, tira que arrebenta). Parte da classe PlayerController.
using Godot;

namespace DeadFrequency;

public partial class PlayerController
{
    public const float MaxStamina = 100f;

    /// <summary>Fôlego para correr (0–100).</summary>
    public float Stamina { get; private set; } = MaxStamina;
    /// <summary>Leseira da madrugada (0–100). Acima de 50 a tela escurece nas bordas e você anda mais devagar.</summary>
    public float Drowsiness { get; private set; }
    /// <summary>Segundos de "efeito pós-almoço" (marmitex).</summary>
    public float SlowLeft { get; private set; }
    /// <summary>Segundos em que o café segura o sono.</summary>
    public float CoffeeLeft { get; private set; }
    /// <summary>Segundos com o fôlego voltando mais rápido (guaraná).</summary>
    public float StaminaBoostLeft { get; private set; }
    /// <summary>Esgotou o fôlego: só volta a correr com 25%.</summary>
    public bool Exhausted { get; private set; }

    private float _staminaRegenDelay;
    private AudioStream[] _slipperSteps;

    private static string Feet => PlayerInventory.Instance != null && PlayerInventory.Instance.Equipped.TryGetValue(ItemSlot.Feet, out string id) ? id : null;
    private static bool OnFarm => ProfessionManager.Instance?.City?.Biome == WorldBiome.Farm;

    /// <summary>Pode correr agora? (segurar Shift sem fôlego = anda normal)</summary>
    private bool CanSprint => !Exhausted && Stamina > 0f;

    /// <summary>Multiplica a velocidade: chinelo, chinelo arrebentado, sono e pós-almoço.</summary>
    private float NeedsSpeedMultiplier()
    {
        float k = 1f;
        string feet = Feet;
        if (feet == ItemCatalog.Slippers)
            k *= 1.12f;
        else if (feet == ItemCatalog.SlippersBroken)
            k *= 0.9f;
        if (SlowLeft > 0f)
            k *= 0.7f;
        if (Drowsiness > 50f)
            k *= Mathf.Lerp(1f, 0.8f, (Drowsiness - 50f) / 50f);
        return k;
    }

    private void UpdateNeeds(float dt, bool sprinting)
    {
        SlowLeft = Mathf.Max(0f, SlowLeft - dt);
        CoffeeLeft = Mathf.Max(0f, CoffeeLeft - dt);
        StaminaBoostLeft = Mathf.Max(0f, StaminaBoostLeft - dt);

        // Fôlego: correr gasta ~7 s de corrida; parado ou andando, volta depois de um respiro.
        if (sprinting)
        {
            Stamina = Mathf.Max(0f, Stamina - 14f * dt);
            _staminaRegenDelay = 1.2f;
            if (Stamina <= 0f)
                Exhausted = true;
        }
        else if (_staminaRegenDelay > 0f)
        {
            _staminaRegenDelay -= dt;
        }
        else
        {
            float regen = 10f * (StaminaBoostLeft > 0f ? 2.5f : 1f) * (SlowLeft > 0f ? 0.5f : 1f);
            Stamina = Mathf.Min(MaxStamina, Stamina + regen * dt);
        }
        if (Exhausted && Stamina >= 25f)
            Exhausted = false;

        // Leseira: entre meia-noite e 5h o sono vem (o café segura); de dia passa.
        float hour = DayNightController.Hour;
        bool lateNight = hour >= 0f && hour < 5f;
        if (lateNight && CoffeeLeft <= 0f && State == PlayerState.Walking)
            Drowsiness = Mathf.Min(100f, Drowsiness + 0.9f * dt);
        else if (!lateNight || CoffeeLeft > 0f)
            Drowsiness = Mathf.Max(0f, Drowsiness - 3f * dt);
    }

    // ---------------- Comida e bebida (PlayerInventory.Use) ----------------

    public string DrinkGuarana()
    {
        Stamina = Mathf.Min(MaxStamina, Stamina + 60f);
        StaminaBoostLeft = 45f;
        Exhausted = false;
        return "Guaraná Pinguim geladinho! Fôlego de volta.";
    }

    public string DrinkCoffee()
    {
        Drowsiness = 0f;
        CoffeeLeft = 240f;
        return "Café Marreta: olho arregalado pelos próximos 4 minutos.";
    }

    public string EatMarmitex()
    {
        Stamina = MaxStamina;
        Exhausted = false;
        SlowLeft = 10f;
        return "Marmitex da Tia: barriga cheia... bateu aquela lerdeza pós-almoço.";
    }

    /// <summary>Espetinho: dá para o caramelo mais perto (ele segue você) ou come.</summary>
    public string UseSkewer()
    {
        FarmAnimal dog = FarmAnimal.NearestDog(GlobalPosition, 5f);
        if (dog != null)
        {
            dog.FollowPlayer(this, 180f);
            return "O caramelo comeu o espetinho e agora é seu parceiro por 3 minutos!";
        }
        Stamina = Mathf.Min(MaxStamina, Stamina + 25f);
        return "Nenhum caramelo por perto... você mesmo comeu o espetinho (duvidoso).";
    }

    // ---------------- Chinelo ----------------

    /// <summary>Som do passo: com chinelo é o ploc; a cada passo, a tira pode arrebentar.</summary>
    private AudioStream SlipperStep(bool sprinting)
    {
        string feet = Feet;
        if (feet != ItemCatalog.Slippers && feet != ItemCatalog.SlippersBroken)
            return null;
        _slipperSteps ??= LoadSlipperSteps();
        if (_slipperSteps.Length == 0)
            return null;
        if (feet == ItemCatalog.Slippers)
        {
            float chance = OnFarm ? (sprinting ? 0.015f : 0.003f) : (sprinting ? 0.0025f : 0f);
            if (GD.Randf() < chance && PlayerInventory.Instance?.BreakSlippers() == true)
                GameHud.Toast(this, "PLOC! A tira do chinelo arrebentou. Use um Prego para consertar.", UiStyle.Warning, 4.0);
        }
        return _slipperSteps[GD.Randi() % (uint)_slipperSteps.Length];
    }

    private static AudioStream[] LoadSlipperSteps()
    {
        var list = new System.Collections.Generic.List<AudioStream>();
        foreach (string path in new[] { "res://Audio/SFX/chinelo_1.wav", "res://Audio/SFX/chinelo_2.wav" })
            if (ResourceLoader.Exists(path))
                list.Add(GD.Load<AudioStream>(path));
        return list.ToArray();
    }
}
