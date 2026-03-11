using Godot;

namespace StarterGame2;

/// <summary>
/// Weapon resource definition. Converted from Kenney's weapon.gd Resource class.
/// Create .tres files referencing this script for each weapon.
/// </summary>
[GlobalClass]
public partial class WeaponResource : Resource
{
    // Model
    [ExportSubgroup("Model")]
    [Export] public PackedScene? Model { get; set; }
    [Export] public Vector3 Position { get; set; }
    [Export] public Vector3 Rotation { get; set; }
    [Export] public Vector3 MuzzlePosition { get; set; }

    // Properties
    [ExportSubgroup("Properties")]
    [Export(PropertyHint.Range, "0.1,1")] public float Cooldown { get; set; } = 0.1f;
    [Export(PropertyHint.Range, "1,20")] public int MaxDistance { get; set; } = 10;
    [Export(PropertyHint.Range, "0,100")] public float Damage { get; set; } = 25f;
    [Export(PropertyHint.Range, "0,5")] public float Spread { get; set; } = 0f;
    [Export(PropertyHint.Range, "1,5")] public int ShotCount { get; set; } = 1;
    [Export(PropertyHint.Range, "0,50")] public int Knockback { get; set; } = 20;

    [Export] public Vector2 MinKnockback { get; set; } = new(0.001f, 0.001f);
    [Export] public Vector2 MaxKnockback { get; set; } = new(0.0025f, 0.002f);

    // Sounds
    [ExportSubgroup("Sounds")]
    [Export] public string SoundShoot { get; set; } = "";

    // Crosshair
    [ExportSubgroup("Crosshair")]
    [Export] public Texture2D? Crosshair { get; set; }
}
