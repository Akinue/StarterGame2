using Godot;

namespace StarterGame2;

/// <summary>
/// Game state manager singleton (autoload). Tracks the current game mode
/// and provides mode-switching signals for the unified FPS+Platformer+Builder game.
/// </summary>
public partial class GameManager : Node
{
    public enum GameMode
    {
        Combat,  // FPS shooting + platforming
        Build    // First-person city building
    }

    [Signal]
    public delegate void ModeChangedEventHandler(int mode);

    public GameMode CurrentMode { get; private set; } = GameMode.Combat;

    public int Cash { get; set; } = 10000;

    public int Coins { get; set; } = 0;

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("toggle_build_mode"))
        {
            ToggleMode();
        }
    }

    public void ToggleMode()
    {
        CurrentMode = CurrentMode == GameMode.Combat ? GameMode.Build : GameMode.Combat;
        EmitSignal(SignalName.ModeChanged, (int)CurrentMode);
        GD.Print($"Mode switched to: {CurrentMode}");
    }

    public bool IsBuildMode => CurrentMode == GameMode.Build;
    public bool IsCombatMode => CurrentMode == GameMode.Combat;
}
