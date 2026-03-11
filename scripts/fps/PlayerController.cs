using Godot;

namespace StarterGame2;

/// <summary>
/// Unified first-person player controller combining FPS shooting, platformer movement
/// (double-jump, coin collection), and build-mode awareness.
/// 
/// Converted and merged from Kenney's FPS player.gd and 3D-Platformer player.gd.
/// 
/// In Combat mode: WASD movement, mouse-look, shooting, weapon switching, double-jump.
/// In Build mode: WASD movement, mouse-look, no shooting (build actions handled by Builder node).
/// </summary>
public partial class PlayerController : CharacterBody3D
{
    // Movement properties
    [ExportSubgroup("Properties")]
    [Export] public float MovementSpeed { get; set; } = 5f;
    [Export(PropertyHint.Range, "0,100")] public int NumberOfJumps { get; set; } = 2;
    [Export] public float JumpStrength { get; set; } = 8f;

    // Weapons
    [ExportSubgroup("Weapons")]
    [Export] public Godot.Collections.Array<WeaponResource> Weapons { get; set; } = new();

    [Export] public TextureRect? Crosshair { get; set; }

    // State
    private WeaponResource? _weapon;
    private int _weaponIndex = 0;

    private float _mouseSensitivity = 700f;
    private float _gamepadSensitivity = 0.075f;
    private bool _mouseCaptured = true;

    private Vector3 _movementVelocity;
    private Vector3 _rotationTarget;
    private Vector2 _inputMouse;

    private int _health = 100;
    private float _gravity = 0f;
    private bool _previouslyFloored = false;
    private int _jumpsRemaining;
    private int _coins;

    private Vector3 _containerOffset = new(1.2f, -1.1f, -2.75f);
    private Tween? _tween;

    // Node references
    private Camera3D _camera = null!;
    private RayCast3D _raycast = null!;
    private AnimatedSprite3D _muzzle = null!;
    private Node3D _container = null!;
    private AudioStreamPlayer _soundFootsteps = null!;
    private Timer _blasterCooldown = null!;

    private AudioManager _audio = null!;
    private GameManager _gameManager = null!;

    [Signal] public delegate void HealthUpdatedEventHandler(int health);
    [Signal] public delegate void CoinCollectedEventHandler(int coins);

    public override void _Ready()
    {
        _audio = GetNode<AudioManager>("/root/AudioManager");
        _gameManager = GetNode<GameManager>("/root/GameManager");

        Input.MouseMode = Input.MouseModeEnum.Captured;

        _camera = GetNode<Camera3D>("Head/Camera");
        _raycast = GetNode<RayCast3D>("Head/Camera/RayCast");
        _muzzle = GetNode<AnimatedSprite3D>("Head/Camera/SubViewportContainer/SubViewport/CameraItem/Muzzle");
        _container = GetNode<Node3D>("Head/Camera/SubViewportContainer/SubViewport/CameraItem/Container");
        _soundFootsteps = GetNode<AudioStreamPlayer>("SoundFootsteps");
        _blasterCooldown = GetNode<Timer>("Cooldown");

        // Initialize weapon
        if (Weapons.Count > 0)
        {
            _weapon = Weapons[_weaponIndex];
            InitiateChangeWeapon(_weaponIndex);
        }

        AddToGroup("player");
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;

        HandleControls(dt);
        HandleGravity(dt);

        // Apply movement
        Vector3 appliedVelocity;

        _movementVelocity = Transform.Basis * _movementVelocity;
        appliedVelocity = Velocity.Lerp(_movementVelocity, dt * 10f);
        appliedVelocity.Y = -_gravity;

        Velocity = appliedVelocity;
        MoveAndSlide();

        // Weapon container sway
        _container.Position = _container.Position.Lerp(
            _containerOffset - (Basis.Inverse() * appliedVelocity / 30f),
            dt * 10f);

        // Footstep sounds
        _soundFootsteps.StreamPaused = true;
        if (IsOnFloor())
        {
            if (Mathf.Abs(Velocity.X) > 1 || Mathf.Abs(Velocity.Z) > 1)
                _soundFootsteps.StreamPaused = false;
        }

        // Landing
        var camPos = _camera.Position;
        camPos.Y = Mathf.Lerp(camPos.Y, 0f, dt * 5f);
        _camera.Position = camPos;

        if (IsOnFloor() && _gravity > 1 && !_previouslyFloored)
        {
            _audio.Play("sounds/fps/land.ogg");
            _camera.Position = new Vector3(_camera.Position.X, -0.1f, _camera.Position.Z);
        }

        _previouslyFloored = IsOnFloor();

        // Respawn on fall
        if (Position.Y < -10)
            GetTree().ReloadCurrentScene();
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion && _mouseCaptured)
        {
            _inputMouse = motion.Relative / _mouseSensitivity;
            HandleRotation(motion.Relative.X, motion.Relative.Y, false);
        }
    }

    private void HandleControls(float delta)
    {
        // Mouse capture
        if (Input.IsActionJustPressed("mouse_capture"))
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            _mouseCaptured = true;
        }
        if (Input.IsActionJustPressed("mouse_capture_exit"))
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            _mouseCaptured = false;
            _inputMouse = Vector2.Zero;
        }

        // Movement (works in both modes)
        var input = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        _movementVelocity = new Vector3(input.X, 0, input.Y).Normalized() * MovementSpeed;

        // Controller camera rotation
        var rotInput = Input.GetVector("camera_right", "camera_left", "camera_down", "camera_up");
        if (rotInput != Vector2.Zero)
            HandleRotation(rotInput.X, rotInput.Y, true, delta);

        // Combat-only actions
        if (_gameManager.IsCombatMode)
        {
            ActionShoot();

            if (Input.IsActionJustPressed("jump") && _jumpsRemaining > 0)
                ActionJump();

            ActionWeaponToggle();
        }
        else
        {
            // Build mode: jump still works for navigation
            if (Input.IsActionJustPressed("jump") && _jumpsRemaining > 0)
                ActionJump();
        }
    }

    private void HandleRotation(float xRot, float yRot, bool isController, float delta = 0f)
    {
        if (isController)
        {
            _rotationTarget -= new Vector3(-yRot, -xRot, 0).LimitLength(1f) * _gamepadSensitivity;
            _rotationTarget.X = Mathf.Clamp(_rotationTarget.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            var camRot = _camera.Rotation;
            camRot.X = Mathf.LerpAngle(camRot.X, _rotationTarget.X, delta * 25f);
            _camera.Rotation = camRot;
            var rot = Rotation;
            rot.Y = Mathf.LerpAngle(rot.Y, _rotationTarget.Y, delta * 25f);
            Rotation = rot;
        }
        else
        {
            _rotationTarget += new Vector3(-yRot, -xRot, 0) / _mouseSensitivity;
            _rotationTarget.X = Mathf.Clamp(_rotationTarget.X, Mathf.DegToRad(-90), Mathf.DegToRad(90));
            var camRot = _camera.Rotation;
            camRot.X = _rotationTarget.X;
            _camera.Rotation = camRot;
            var rot = Rotation;
            rot.Y = _rotationTarget.Y;
            Rotation = rot;
        }
    }

    private void HandleGravity(float delta)
    {
        _gravity += 20f * delta;

        if (_gravity > 0 && IsOnFloor())
        {
            _jumpsRemaining = NumberOfJumps;
            _gravity = 0;
        }
    }

    private void ActionJump()
    {
        _audio.Play("sounds/fps/jump_a.ogg, sounds/fps/jump_b.ogg, sounds/fps/jump_c.ogg");
        _gravity = -JumpStrength;
        _jumpsRemaining--;
    }

    private void ActionShoot()
    {
        if (_weapon == null) return;
        if (!Input.IsActionPressed("shoot")) return;
        if (!_blasterCooldown.IsStopped()) return;

        _audio.Play(_weapon.SoundShoot);

        _muzzle.Play("default");
        _muzzle.RotationDegrees = new Vector3(
            _muzzle.RotationDegrees.X,
            _muzzle.RotationDegrees.Y,
            (float)GD.RandRange(-45, 45));
        _muzzle.Scale = Vector3.One * (float)GD.RandRange(0.40, 0.75);
        _muzzle.Position = _container.Position - _weapon.MuzzlePosition;

        _blasterCooldown.Start(_weapon.Cooldown);

        // Fire raycasts per shot count
        for (int n = 0; n < _weapon.ShotCount; n++)
        {
            _raycast.TargetPosition = new Vector3(
                (float)GD.RandRange(-_weapon.Spread, _weapon.Spread),
                (float)GD.RandRange(-_weapon.Spread, _weapon.Spread),
                -1f * _weapon.MaxDistance);

            _raycast.ForceRaycastUpdate();

            if (!_raycast.IsColliding()) continue;

            var collider = _raycast.GetCollider();

            if (collider is Node node && node.HasMethod("Damage"))
                node.Call("Damage", (int)_weapon.Damage);

            // Create impact effect
            var impactScene = GD.Load<PackedScene>("res://objects/fps/impact.tscn");
            if (impactScene != null)
            {
                var impactInstance = impactScene.Instantiate<AnimatedSprite3D>();
                impactInstance.Play("shot");
                GetTree().Root.AddChild(impactInstance);
                impactInstance.Position = _raycast.GetCollisionPoint() + (_raycast.GetCollisionNormal() / 10f);
                impactInstance.LookAt(_camera.GlobalTransform.Origin, Vector3.Up, true);
            }
        }

        // Knockback
        var knockback = RandomVec2(_weapon.MinKnockback, _weapon.MaxKnockback);
        var cPos = _container.Position;
        cPos.Z += 0.25f;
        _container.Position = cPos;

        var cRot = _camera.Rotation;
        cRot.X += knockback.X;
        _camera.Rotation = cRot;

        var pRot = Rotation;
        pRot.Y += knockback.Y;
        Rotation = pRot;

        _rotationTarget.X += knockback.X;
        _rotationTarget.Y += knockback.Y;

        _movementVelocity += new Vector3(0, 0, _weapon.Knockback);
    }

    private void ActionWeaponToggle()
    {
        if (!Input.IsActionJustPressed("weapon_toggle")) return;
        if (Weapons.Count == 0) return;

        _weaponIndex = (int)Mathf.Wrap(_weaponIndex + 1, 0, Weapons.Count);
        InitiateChangeWeapon(_weaponIndex);
        _audio.Play("sounds/fps/weapon_change.ogg");
    }

    private void InitiateChangeWeapon(int index)
    {
        _weaponIndex = index;

        _tween = GetTree().CreateTween();
        _tween.SetEase(Tween.EaseType.InOut);
        _tween.TweenProperty(_container, "position",
            _containerOffset - new Vector3(0, 1, 0), 0.1);
        _tween.TweenCallback(Callable.From(ChangeWeapon));
    }

    private void ChangeWeapon()
    {
        if (Weapons.Count == 0) return;
        _weapon = Weapons[_weaponIndex];

        // Remove previous weapon models
        foreach (var child in _container.GetChildren())
        {
            _container.RemoveChild(child);
            child.QueueFree();
        }

        // Add new weapon model
        var weaponModel = _weapon.Model?.Instantiate<Node3D>();
        if (weaponModel == null) return;

        _container.AddChild(weaponModel);
        weaponModel.Position = _weapon.Position;
        weaponModel.RotationDegrees = _weapon.Rotation;

        // Set mesh to weapon camera layer (layer 2)
        foreach (var child in weaponModel.FindChildren("*", "MeshInstance3D"))
        {
            if (child is MeshInstance3D mesh)
                mesh.Layers = 2;
        }

        _raycast.TargetPosition = new Vector3(0, 0, -1) * _weapon.MaxDistance;

        if (Crosshair != null)
            Crosshair.Texture = _weapon.Crosshair;
    }

    /// <summary>Called by enemies or hazards to deal damage to the player.</summary>
    public void Damage(int amount)
    {
        _health -= amount;
        EmitSignal(SignalName.HealthUpdated, _health);

        if (_health < 0)
            GetTree().ReloadCurrentScene();
    }

    /// <summary>Called by Coin objects when collected (platformer feature).</summary>
    public void CollectCoin()
    {
        _coins++;
        EmitSignal(SignalName.CoinCollected, _coins);

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.Coins = _coins;
    }

    private static Vector2 RandomVec2(Vector2 min, Vector2 max)
    {
        int sign = GD.Randi() % 2 == 0 ? -1 : 1;
        return new Vector2(
            (float)GD.RandRange(min.X, max.X),
            (float)GD.RandRange(min.Y, max.Y) * sign);
    }
}
