using Godot;

namespace StarterGame2;

/// <summary>
/// Data for a single structure placement on the grid. Converted from Kenney's data_structure.gd.
/// </summary>
[GlobalClass]
public partial class DataStructure : Resource
{
    [Export] public Vector2I Position { get; set; }
    [Export] public int Orientation { get; set; }
    [Export] public int Structure { get; set; }
}
