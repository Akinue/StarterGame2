using Godot;

namespace StarterGame2;

/// <summary>
/// Decorative cloud with sine-wave movement.
/// Converted from Kenney's cloud.gd (shared across FPS and Platformer kits).
/// </summary>
public partial class Cloud : Node3D
{
    private float _time = 0f;
    private float _randomVelocity;
    private float _randomTime;

    public override void _Ready()
    {
        var rng = new RandomNumberGenerator();
        _randomVelocity = rng.RandfRange(0.1f, 2.0f);
        _randomTime = rng.RandfRange(0.1f, 2.0f);
    }

    public override void _Process(double delta)
    {
        var pos = Position;
        pos.Y += (float)(Mathf.Cos(_time * _randomTime) * _randomVelocity) * (float)delta;
        Position = pos;
        _time += (float)delta;
    }
}
