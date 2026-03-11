using Godot;

namespace StarterGame2;

/// <summary>
/// Collectible coin with rotation and sine-wave animation.
/// Converted from Kenney's coin.gd.
/// </summary>
public partial class Coin : Area3D
{
    private float _time = 0f;
    private bool _grabbed = false;

    public override void _Ready()
    {
        BodyEntered += OnBodyEntered;
    }

    private void OnBodyEntered(Node3D body)
    {
        if (_grabbed) return;

        if (body.HasMethod("CollectCoin"))
        {
            body.Call("CollectCoin");

            var audio = GetNode<AudioManager>("/root/AudioManager");
            audio.Play("sounds/platformer/coin.ogg");

            GetNode<Node3D>("Mesh").QueueFree();

            var particles = GetNodeOrNull<GpuParticles3D>("Particles");
            if (particles != null)
                particles.Emitting = false;

            _grabbed = true;
        }
    }

    public override void _Process(double delta)
    {
        RotateY(2f * (float)delta);

        var pos = Position;
        pos.Y += (float)(Mathf.Cos(_time * 5f) * 1f) * (float)delta;
        Position = pos;

        _time += (float)delta;
    }
}
