using Godot;

namespace StarterGame2;

/// <summary>
/// Enemy AI. Looks at player, sine-wave movement, shoots on timer.
/// Converted from Kenney's enemy.gd.
/// </summary>
public partial class Enemy : Node3D
{
    [Export] public Node3D? Player { get; set; }

    private RayCast3D _raycast = null!;
    private AnimatedSprite3D _muzzleA = null!;
    private AnimatedSprite3D _muzzleB = null!;

    private int _health = 100;
    private float _time = 0f;
    private Vector3 _targetPosition;
    private bool _destroyed = false;

    public override void _Ready()
    {
        _raycast = GetNode<RayCast3D>("RayCast");
        _muzzleA = GetNode<AnimatedSprite3D>("MuzzleA");
        _muzzleB = GetNode<AnimatedSprite3D>("MuzzleB");
        _targetPosition = Position;
    }

    public override void _Process(double delta)
    {
        if (Player == null) return;

        LookAt(Player.Position + new Vector3(0, 0.5f, 0), Vector3.Up, true);

        _targetPosition.Y += (float)(Mathf.Cos(_time * 5f) * 1f) * (float)delta;
        _time += (float)delta;
        Position = _targetPosition;
    }

    public void Damage(int amount)
    {
        var audio = GetNode<AudioManager>("/root/AudioManager");
        audio.Play("sounds/fps/enemy_hurt.ogg");

        _health -= amount;

        if (_health <= 0 && !_destroyed)
            Destroy();
    }

    private void Destroy()
    {
        var audio = GetNode<AudioManager>("/root/AudioManager");
        audio.Play("sounds/fps/enemy_destroy.ogg");

        _destroyed = true;
        QueueFree();
    }

    // Connected via signal from Timer node in scene
    public void OnTimerTimeout()
    {
        _raycast.ForceRaycastUpdate();

        if (!_raycast.IsColliding()) return;

        var collider = _raycast.GetCollider();
        if (collider is Node node && node.HasMethod("Damage"))
        {
            _muzzleA.Frame = 0;
            _muzzleA.Play("default");
            _muzzleA.RotationDegrees = new Vector3(_muzzleA.RotationDegrees.X, _muzzleA.RotationDegrees.Y, (float)GD.RandRange(-45, 45));

            _muzzleB.Frame = 0;
            _muzzleB.Play("default");
            _muzzleB.RotationDegrees = new Vector3(_muzzleB.RotationDegrees.X, _muzzleB.RotationDegrees.Y, (float)GD.RandRange(-45, 45));

            var audio = GetNode<AudioManager>("/root/AudioManager");
            audio.Play("sounds/fps/enemy_attack.ogg");

            node.Call("Damage", 5);
        }
    }
}
