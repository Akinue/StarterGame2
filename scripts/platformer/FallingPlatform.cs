using Godot;

namespace StarterGame2;

/// <summary>
/// Platform that falls when the player steps on it.
/// Converted from Kenney's platform_falling.gd.
/// </summary>
public partial class FallingPlatform : Node3D
{
    private bool _falling = false;
    private float _fallVelocity = 0f;

    public override void _PhysicsProcess(double delta)
    {
        Scale = Scale.Lerp(Vector3.One, (float)delta * 10f);

        if (_falling)
        {
            _fallVelocity += 15f * (float)delta;
            var pos = Position;
            pos.Y -= _fallVelocity * (float)delta;
            Position = pos;
        }
        else
        {
            _fallVelocity = 0f;
        }

        if (Position.Y < -10f)
            QueueFree();
    }

    // Connected from Area3D signal in scene
    public void OnBodyEntered(Node3D _body)
    {
        if (!_falling)
        {
            var audio = GetNode<AudioManager>("/root/AudioManager");
            audio.Play("sounds/platformer/fall.ogg");
            Scale = new Vector3(1.25f, 1f, 1.25f);
        }
        _falling = true;
    }
}
