using Godot;

namespace StarterGame2;

/// <summary>
/// FPS HUD displaying health, coins, cash, and current mode.
/// Converted from Kenney's hud.gd with additions for the unified game.
/// </summary>
public partial class GameHud : CanvasLayer
{
    private Label _healthLabel = null!;
    private Label _coinsLabel = null!;
    private Label _cashLabel = null!;
    private Label _modeLabel = null!;
    private TextureRect? _crosshair;

    public override void _Ready()
    {
        _healthLabel = GetNode<Label>("Health");
        _coinsLabel = GetNode<Label>("Coins");
        _cashLabel = GetNode<Label>("Cash");
        _modeLabel = GetNode<Label>("Mode");

        _crosshair = GetNodeOrNull<TextureRect>("Crosshair");

        // Connect to GameManager mode changes
        var gm = GetNode<GameManager>("/root/GameManager");
        gm.ModeChanged += OnModeChanged;

        UpdateModeDisplay(gm.CurrentMode);
    }

    public void OnHealthUpdated(int health)
    {
        _healthLabel.Text = health + "%";
    }

    public void OnCoinCollected(int coins)
    {
        _coinsLabel.Text = coins.ToString();
    }

    public void OnCashUpdated(int cash)
    {
        _cashLabel.Text = "$" + cash;
    }

    public void SetCrosshair(Texture2D? texture)
    {
        if (_crosshair != null)
            _crosshair.Texture = texture;
    }

    private void OnModeChanged(int mode)
    {
        UpdateModeDisplay((GameManager.GameMode)mode);
    }

    private void UpdateModeDisplay(GameManager.GameMode mode)
    {
        _modeLabel.Text = mode switch
        {
            GameManager.GameMode.Combat => "COMBAT",
            GameManager.GameMode.Build => "BUILD [Tab]",
            _ => ""
        };
    }
}
