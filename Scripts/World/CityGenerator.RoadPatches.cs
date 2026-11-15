// Scripts/World/CityGenerator.RoadPatches.cs
// Fase 1 · Cidade (ambientação Brazilcore): asfalto remendado (remendos escuros e valetas de concreto cortando a rua,
// do jeito que a companhia abre e tapa) e bueiros abertos sinalizados com um galho de árvore enfiado no buraco.
// Só visual, rente ao asfalto (1,2 cm): não atrapalha a suspensão. Escolha por AssetProps.Hash do bloco, então não mexe
// nos sorteios da cidade (layout igual para todos no online).
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private StandardMaterial3D _patchDark, _patchConcrete, _holeBlack, _branchWood, _branchLeaves;

    private void BuildRoadPatches()
    {
        if (_useLogicalBlockPlans)
            return; // Remendos acompanham o chunk.

        var root = new Node3D { Name = "RoadPatches" };
        _staticRoot.AddChild(root);
        for (int bx = -Radius; bx <= Radius; bx++)
        {
            for (int bz = -Radius; bz <= Radius; bz++)
                AddRoadPatchesForBlock(root, bx, bz);
        }
    }

    private void BuildRoadPatchesForBlock(int bx, int bz)
    {
        var root = new Node3D { Name = $"RoadPatches_{bx}_{bz}" };
        (_activeChunkRoot ?? _staticRoot).AddChild(root);
        AddRoadPatchesForBlock(root, bx, bz);
    }

    private static float Frac(int hash, int divisor) => hash / divisor % 1000 / 1000f;

    private void AddRoadPatchesForBlock(Node3D root, int bx, int bz)
    {
        if (bx == 0 && bz == 0)
            return; // Quarteirão do depósito: ruas ao redor ficam limpas.
        _patchDark ??= Flat(new Color(0.045f, 0.045f, 0.05f), 1f);
        _patchConcrete ??= Flat(new Color(0.2f, 0.2f, 0.19f), 1f);
        Vector3 c = BlockCenter(bx, bz);
        int hash = AssetProps.Hash(c, 79);
        float alongReach = BlockSize * 0.5f - 3f;
        float laneReach = RoadWidth * 0.5f - 1.3f;

        // Dois remendos escuros em ruas sorteadas (leste: corre ao longo de Z; sul: ao longo de X).
        for (int i = 0; i < 2; i++)
        {
            int h = hash / (i * 7 + 1) + i * 131;
            bool east = h % 2 == 0;
            float along = (Frac(h, 3) * 2f - 1f) * alongReach;
            float lane = (Frac(h, 31) * 2f - 1f) * laneReach;
            float len = 1.4f + Frac(h, 97) * 2.2f;
            float wid = 0.9f + Frac(h, 389) * 1.1f;
            Vector3 pos = east
                ? c + new Vector3(Period * 0.5f + lane, 0.006f, along)
                : c + new Vector3(along, 0.006f, Period * 0.5f + lane);
            Vector3 size = east ? new Vector3(wid, 0.012f, len) : new Vector3(len, 0.012f, wid);
            AddBox(root, pos, size, _patchDark, false);
        }

        // Valeta de concreto atravessando a rua (corte para tubulação), em ~35% dos blocos.
        if (hash / 1000 % 100 < 35)
        {
            bool east = hash / 100000 % 2 == 0;
            float along = (Frac(hash, 17) * 2f - 1f) * (alongReach - 2f);
            float width = RoadWidth - 0.4f;
            Vector3 pos = east
                ? c + new Vector3(Period * 0.5f, 0.007f, along)
                : c + new Vector3(along, 0.007f, Period * 0.5f);
            Vector3 size = east ? new Vector3(width, 0.012f, 0.55f) : new Vector3(0.55f, 0.012f, width);
            AddBox(root, pos, size, _patchConcrete, false);
        }

        // Bueiro aberto com galho de árvore de sinalização, em ~20% dos blocos (na rua leste).
        if (hash / 7 % 100 < 20)
        {
            float along = (Frac(hash, 53) * 2f - 1f) * alongReach;
            float lane = (Frac(hash, 211) * 2f - 1f) * (laneReach - 0.8f);
            BuildOpenManhole(root, c + new Vector3(Period * 0.5f + lane, 0f, along), hash);
        }
    }

    private void BuildOpenManhole(Node3D parent, Vector3 position, int hash)
    {
        _holeBlack ??= Flat(new Color(0.01f, 0.01f, 0.012f), 1f);
        _branchWood ??= Flat(new Color(0.24f, 0.16f, 0.1f), 1f);
        _branchLeaves ??= Flat(new Color(0.2f, 0.38f, 0.15f), 1f);
        StandardMaterial3D rim = _darkMetal;
        AddCylinder(parent, position + new Vector3(0f, 0.025f, 0f), 0.58f, 0.05f, rim);
        AddCylinder(parent, position + new Vector3(0f, 0.052f, 0f), 0.46f, 0.012f, _holeBlack);
        // Galho enfiado no buraco: tronco fino com a folhagem em cima (a tampa sumiu, o povo avisa assim).
        float lean = (Frac(hash, 19) - 0.5f) * 0.3f;
        Vector3 top = position + new Vector3(lean, 1.15f, 0f);
        AddCylinder(parent, position + new Vector3(lean * 0.5f, 0.55f, 0f), 0.025f, 1.1f, _branchWood).Name = "GalhoBueiro";
        AddCylinder(parent, top + new Vector3(0f, 0.2f, 0f), 0.36f, 0.6f, _branchLeaves, _treeTop);
    }
}
