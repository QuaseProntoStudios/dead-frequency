// Scripts/Core/GameLevels.cs
// Lista das fases. O id vai para o servidor ao criar a sala, então todos carregam a mesma fase.
namespace DeadFrequency;

public static class GameLevels
{
    public readonly record struct Level(string Id, string Name, string Subtitle, string Scene);

    public static readonly Level[] All =
    {
        new("city", "Fase 1 · Cidade", "prédios, ônibus e lojas na zona morta", "res://Scenes/Levels/test_level.tscn"),
        new("farm", "Fase 2 · Rancho", "estradas de terra, sítios, lago e capela", "res://Scenes/Levels/farm_level.tscn"),
    };

    public static Level Get(string id)
    {
        foreach (Level l in All)
            if (l.Id == id)
                return l;
        return All[0];
    }
}
