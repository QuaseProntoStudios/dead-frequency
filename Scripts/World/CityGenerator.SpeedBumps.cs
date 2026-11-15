// Scripts/World/CityGenerator.SpeedBumps.cs
// Fase 1 · Cidade (ambientação Brazilcore): lombadas nas ruas, amarelas com faixa preta, com colisão em rampa
// (trapézio) para a suspensão da van/caminhonete sentir. Escolha por AssetProps.Hash do bloco, então não mexe nos
// sorteios da cidade (endereços e layout continuam iguais para todos no online). Fora das ruas coladas ao depósito.
using Godot;

namespace DeadFrequency;

public partial class CityGenerator
{
    private const float BumpHeight = 0.1f;
    private const float BumpHalfBase = 0.9f; // meia largura da base no sentido da rua (rampa de ~0,55 m)
    private const float BumpHalfTop = 0.35f;

    private ArrayMesh _bumpMesh;
    private ConvexPolygonShape3D _bumpShape;

    private void BuildSpeedBumps()
    {
        if (_useLogicalBlockPlans)
            return; // As lombadas acompanham o chunk.

        var root = new Node3D { Name = "SpeedBumps" };
        _staticRoot.AddChild(root);
        for (int bx = -Radius; bx <= Radius; bx++)
        {
            for (int bz = -Radius; bz <= Radius; bz++)
                AddSpeedBumpsForBlock(root, bx, bz);
        }
    }

    private void BuildSpeedBumpsForBlock(int bx, int bz)
    {
        var root = new Node3D { Name = $"SpeedBumps_{bx}_{bz}" };
        (_activeChunkRoot ?? _staticRoot).AddChild(root);
        AddSpeedBumpsForBlock(root, bx, bz);
    }

    private void AddSpeedBumpsForBlock(Node3D root, int bx, int bz)
    {
        int hash = AssetProps.Hash(BlockCenter(bx, bz), 61);
        Vector3 c = BlockCenter(bx, bz);
        float off = (hash / 10000 % 7 - 3) * 1.5f; // ±4,5 m: longe dos cruzamentos
        // Rua a leste do bloco (corre ao longo de Z), se não encosta no quarteirão do depósito (0,0).
        if (hash % 100 < 30 && !((bx == 0 || bx + 1 == 0) && bz == 0))
            SpawnSpeedBump(root, c + new Vector3(Period * 0.5f, 0f, off), 0f);
        // Rua ao sul do bloco (corre ao longo de X).
        if (hash / 100 % 100 < 30 && !(bx == 0 && (bz == 0 || bz + 1 == 0)))
            SpawnSpeedBump(root, c + new Vector3(off, 0f, Period * 0.5f), Mathf.Pi * 0.5f);
    }

    private void SpawnSpeedBump(Node3D root, Vector3 position, float rotY)
    {
        EnsureBumpMesh();
        var body = new StaticBody3D { Name = "Lombada", Position = position, Rotation = new Vector3(0f, rotY, 0f), CollisionLayer = 1, CollisionMask = 0 };
        body.AddChild(new CollisionShape3D { Shape = _bumpShape });
        body.AddChild(new MeshInstance3D { Mesh = _bumpMesh, CastShadow = GeometryInstance3D.ShadowCastingSetting.Off });
        root.AddChild(body);
    }

    /// <summary>Perfil em trapézio extrudado pela largura da rua (eixo X); o tráfego passa ao longo de Z. Rampas amarelas, topo preto.</summary>
    private void EnsureBumpMesh()
    {
        if (_bumpMesh != null)
            return;
        float hw = (RoadWidth - 0.2f) * 0.5f;
        // Pontos do perfil (z, y) da esquerda para a direita.
        Vector2 a = new(-BumpHalfBase, 0f), b = new(-BumpHalfTop, BumpHeight), c = new(BumpHalfTop, BumpHeight), d = new(BumpHalfBase, 0f);

        var yellow = new SurfaceTool();
        yellow.Begin(Mesh.PrimitiveType.Triangles);
        yellow.SetMaterial(Flat(new Color(0.95f, 0.78f, 0.1f), 0.8f));
        Quad(yellow, a, b, hw);
        Quad(yellow, c, d, hw);
        Cap(yellow, a, b, c, d, hw);
        Cap(yellow, a, b, c, d, -hw);
        yellow.GenerateNormals();
        ArrayMesh mesh = yellow.Commit();

        var black = new SurfaceTool();
        black.Begin(Mesh.PrimitiveType.Triangles);
        black.SetMaterial(Flat(new Color(0.06f, 0.06f, 0.06f), 0.8f));
        Quad(black, b, c, hw);
        black.GenerateNormals();
        black.Commit(mesh);

        _bumpMesh = mesh;
        var pts = new Vector3[8];
        Vector2[] prof = { a, b, c, d };
        for (int i = 0; i < 4; i++)
        {
            pts[i] = new Vector3(-hw, prof[i].Y, prof[i].X);
            pts[i + 4] = new Vector3(hw, prof[i].Y, prof[i].X);
        }
        _bumpShape = new ConvexPolygonShape3D { Points = pts };
    }

    /// <summary>Faixa do perfil p→q extrudada de x = -hw a +hw (virada para cima).</summary>
    private static void Quad(SurfaceTool st, Vector2 p, Vector2 q, float hw)
    {
        Vector3 p0 = new(-hw, p.Y, p.X), p1 = new(hw, p.Y, p.X), q0 = new(-hw, q.Y, q.X), q1 = new(hw, q.Y, q.X);
        // Sentido horário visto de cima (Godot: frente = horário).
        Tri(st, p0, q0, q1);
        Tri(st, p0, q1, p1);
        // Dupla face (a malha é pequena; evita sumir por erro de sentido).
        Tri(st, p0, q1, q0);
        Tri(st, p0, p1, q1);
    }

    private static void Cap(SurfaceTool st, Vector2 a, Vector2 b, Vector2 c, Vector2 d, float x)
    {
        Vector3 A = new(x, a.Y, a.X), B = new(x, b.Y, b.X), C = new(x, c.Y, c.X), D = new(x, d.Y, d.X);
        Tri(st, A, B, C);
        Tri(st, A, C, D);
        Tri(st, A, C, B);
        Tri(st, A, D, C);
    }

    private static void Tri(SurfaceTool st, Vector3 a, Vector3 b, Vector3 c)
    {
        st.AddVertex(a);
        st.AddVertex(b);
        st.AddVertex(c);
    }
}
