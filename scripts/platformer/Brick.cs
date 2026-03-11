using Godot;

namespace StarterGame2;

/// <summary>
/// Breakable brick that explodes when hit from below.
/// Converted from Kenney's brick.gd.
/// </summary>
public partial class Brick : StaticBody3D
{
    private Area3D _bottomDetector = null!;
    private Node3D _mesh = null!;
    private GpuParticles3D _particles = null!;
    private bool _exploded = false;

    public override void _Ready()
    {
        _bottomDetector = GetNode<Area3D>("BottomDetector");
        _mesh = GetNode<Node3D>("Mesh");
        _particles = GetNode<GpuParticles3D>("Particles");

        _bottomDetector.BodyEntered += OnBottomHit;
    }

    private void OnBottomHit(Node3D body)
    {
        if (body.IsInGroup("player"))
            Explode();
    }

    private async void Explode()
    {
        if (_exploded) return;
        _exploded = true;

        var audio = GetNode<AudioManager>("/root/AudioManager");
        audio.Play("sounds/platformer/break.ogg");

        _particles.Restart();
        _mesh.Hide();
        GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        _bottomDetector.Monitoring = false;

        await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout);
        QueueFree();
    }
}
