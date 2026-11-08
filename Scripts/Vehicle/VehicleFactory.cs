// Scripts/Vehicle/VehicleFactory.cs
// Veículos extras do pátio do depósito (v0.9.3), montados em código com o mesmo VanController da van:
//   • moto de entrega (com baú: leva uma caixa de até 20 kg; dá para fazer entregas com ela);
//   • caminhões emprestados (amarelo e vermelho) para ir para casa ou passear.
// Os modelos são da Kenney (CC0). Frente = +Z, como a van. Cada um tem um NetId próprio no online.
using Godot;

namespace DeadFrequency;

public enum VehicleKind
{
    Moto,
    TruckYellow,
    TruckRed,
}

public static class VehicleFactory
{
    public readonly record struct Spec(string NetId, string Name, string Model, float ModelScale, float ModelYaw, Vector3 Body, float WheelX, float WheelZ, float WheelRadius,
        float Mass, float Engine, float Brake, float MaxKmh, Vector3 Seat, Vector3 Exit, Vector3 Lights, string Prompt);

    public static Spec Get(VehicleKind kind) => kind switch
    {
        VehicleKind.Moto => new("moto", "moto", "vehicle-motorcycle.glb", 0.95f, 0f, new Vector3(0.7f, 0.9f, 2.0f), 0.18f, 0.72f, 0.32f,
            260f, 900f, 14f, 85f, new Vector3(0f, 0.05f, -0.15f), new Vector3(1.1f, 0f, 0f), new Vector3(0f, 0.95f, 0.95f), "[E] Pilotar a moto de entrega"),
        VehicleKind.TruckRed => new("truck_red", "caminhão", "vehicle-truck-red.glb", 1.7f, 0f, new Vector3(2.4f, 2.2f, 4.6f), 1.0f, 1.45f, 0.45f,
            2600f, 4200f, 70f, 80f, new Vector3(0.5f, 0.85f, 1.2f), new Vector3(2.0f, 0f, 1.2f), new Vector3(0f, 1.1f, 2.35f), "[E] Pegar o caminhão emprestado"),
        _ => new("truck_yellow", "caminhão", "vehicle-truck-yellow.glb", 1.7f, 0f, new Vector3(2.4f, 2.2f, 4.6f), 1.0f, 1.45f, 0.45f,
            2600f, 4200f, 70f, 80f, new Vector3(0.5f, 0.85f, 1.2f), new Vector3(2.0f, 0f, 1.2f), new Vector3(0f, 1.1f, 2.35f), "[E] Pegar o caminhão emprestado"),
    };

    /// <summary>Monta o veículo (ainda fora da árvore). Posicione e adicione depois.</summary>
    public static VanController Build(VehicleKind kind)
    {
        Spec s = Get(kind);
        var v = new VanController
        {
            Name = s.NetId,
            NetId = s.NetId,
            Kind = kind == VehicleKind.Moto ? "moto" : "truck",
            DisplayName = s.Name,
            Mass = s.Mass,
            CollisionLayer = 16, // Vehicle
            CollisionMask = 1 | 2 | 8,
            CenterOfMassMode = RigidBody3D.CenterOfMassModeEnum.Custom,
            CenterOfMass = new Vector3(0f, kind == VehicleKind.Moto ? 0.05f : 0.5f, 0f),
            MaxEngineForce = s.Engine,
            MaxReverseForce = s.Engine * 0.45f,
            BrakeForce = s.Brake,
            HandbrakeForce = s.Brake * 2f,
            ParkedBrake = s.Brake * 0.7f,
            CoastBrake = s.Brake * 0.07f,
            MaxSpeedKmh = s.MaxKmh,
            MaxSteerDegrees = kind == VehicleKind.Moto ? 30f : 28f,
        };
        if (kind == VehicleKind.Moto)
            v.AngularDamp = 3f; // segura a moto em pé

        float bodyY = s.Body.Y * 0.5f + s.WheelRadius * 0.6f;
        v.AddChild(new CollisionShape3D { Name = "BodyShape", Shape = new BoxShape3D { Size = s.Body }, Position = new Vector3(0f, bodyY, 0f) });

        string path = AssetProps.Kenney + s.Model;
        if (ResourceLoader.Exists(path) && GD.Load<PackedScene>(path)?.Instantiate() is Node3D model)
        {
            model.Name = "Model";
            model.Scale = Vector3.One * s.ModelScale;
            model.Rotation = new Vector3(0f, s.ModelYaw, 0f);
            v.AddChild(model);
        }

        // Rodas invisíveis (o modelo já tem as dele). Na moto ficam bem juntas: ela não tomba.
        foreach ((string name, float x, float z, bool steer) in new[]
        {
            ("WheelFL", s.WheelX, s.WheelZ, true), ("WheelFR", -s.WheelX, s.WheelZ, true),
            ("WheelRL", s.WheelX, -s.WheelZ, false), ("WheelRR", -s.WheelX, -s.WheelZ, false),
        })
        {
            v.AddChild(new VehicleWheel3D
            {
                Name = name,
                Position = new Vector3(x, s.WheelRadius + 0.12f, z),
                WheelRadius = s.WheelRadius,
                WheelRestLength = 0.15f,
                WheelFrictionSlip = kind == VehicleKind.Moto ? 3.2f : 2.4f,
                WheelRollInfluence = kind == VehicleKind.Moto ? 0.02f : 0.1f,
                SuspensionTravel = 0.2f,
                SuspensionStiffness = kind == VehicleKind.Moto ? 55f : 55f,
                SuspensionMaxForce = s.Mass * 7f,
                DampingCompression = 1.2f,
                DampingRelaxation = 1.4f,
                UseAsSteering = steer,
                UseAsTraction = !steer || kind != VehicleKind.Moto,
            });
        }

        var seat = new Marker3D { Name = "SeatMarker", Position = s.Seat, Rotation = new Vector3(0f, Mathf.Pi, 0f) };
        var exit = new Marker3D { Name = "ExitMarker", Position = s.Exit, Rotation = new Vector3(0f, Mathf.Pi * 0.5f, 0f) };
        v.AddChild(seat);
        v.AddChild(exit);
        var door = new DriverSeatInteractable
        {
            Name = "DriverDoor",
            Van = v,
            SeatMarker = seat,
            ExitMarker = exit,
            CollisionLayer = 4,
            CollisionMask = 0,
            Monitoring = false,
            Position = new Vector3(0f, bodyY, 0f),
        };
        door.AddChild(new CollisionShape3D { Shape = new BoxShape3D { Size = s.Body + new Vector3(0.5f, 0.2f, 0.2f) } });
        v.AddChild(door);

        var lights = new Node3D { Name = "Headlights" };
        v.AddChild(lights);
        foreach (float x in kind == VehicleKind.Moto ? new[] { 0f } : new[] { -0.75f, 0.75f })
        {
            lights.AddChild(new SpotLight3D
            {
                Position = s.Lights + new Vector3(x, 0f, 0f),
                Rotation = new Vector3(0f, Mathf.Pi, 0f),
                LightColor = new Color(1f, 0.94f, 0.78f),
                LightEnergy = 6f,
                SpotRange = 30f,
                SpotAngle = 28f,
                ShadowEnabled = false,
            });
        }
        if (kind == VehicleKind.Moto)
            MotoTopCase.Attach(v);
        return v;
    }
}
