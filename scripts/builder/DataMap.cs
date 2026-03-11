using Godot;
using Godot.Collections;

namespace StarterGame2;

/// <summary>
/// Map save data containing cash and all placed structures. Converted from Kenney's data_map.gd.
/// </summary>
[GlobalClass]
public partial class DataMap : Resource
{
    [Export] public int Cash { get; set; } = 10000;
    [Export] public Array<DataStructure> Structures { get; set; } = new();
}
