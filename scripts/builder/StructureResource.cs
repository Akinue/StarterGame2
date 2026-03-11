using Godot;

namespace StarterGame2;

/// <summary>
/// Structure resource for city building. Converted from Kenney's structure.gd.
/// </summary>
[GlobalClass]
public partial class StructureResource : Resource
{
    [ExportSubgroup("Model")]
    [Export] public PackedScene? Model { get; set; }

    [ExportSubgroup("Gameplay")]
    [Export] public int Price { get; set; }
}
