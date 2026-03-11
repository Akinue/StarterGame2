using Godot;

namespace StarterGame2;

/// <summary>
/// HUD displaying health bar, coins, cash, mode, and level seed.
/// Uses Kenney UI assets for the health bar and coin icon.
/// </summary>
public partial class GameHud : CanvasLayer
{
    private TextureProgressBar _healthBar = null!;
    private Label _healthValue = null!;
    private Label _coinValue = null!;
    private HBoxContainer _cashRow = null!;
    private Label _cashValue = null!;
    private Label _modeLabel = null!;
    private Label _seedLabel = null!;
    private TextureRect? _crosshair;

    public override void _Ready()
    {
        _healthBar = GetNode<TextureProgressBar>("StatsPanel/VBox/HealthRow/HealthBar");
        _healthValue = GetNode<Label>("StatsPanel/VBox/HealthRow/HealthValue");
        _coinValue = GetNode<Label>("StatsPanel/VBox/CoinRow/CoinValue");
        _cashRow = GetNode<HBoxContainer>("StatsPanel/VBox/CashRow");
        _cashValue = GetNode<Label>("StatsPanel/VBox/CashRow/CashValue");
        _modeLabel = GetNode<Label>("ModeLabel");
        _seedLabel = GetNode<Label>("SeedLabel");
        _crosshair = GetNodeOrNull<TextureRect>("Crosshair");

        var gm = GetNode<GameManager>("/root/GameManager");
        gm.ModeChanged += OnModeChanged;
        UpdateModeDisplay(gm.CurrentMode);

        var player = GetNodeOrNull<PlayerController>("../Player");
        if (player != null)
        {
            player.HealthUpdated += OnHealthUpdated;
            player.CoinCollected += OnCoinCollected;
        }

        var levelGen = GetNodeOrNull<LevelGenerator>("../LevelGenerator");
        if (levelGen != null)
            levelGen.LevelGenerated += OnLevelGenerated;
    }

    public void OnHealthUpdated(int health)
    {
        _healthBar.Value = health;
        _healthValue.Text = health.ToString();
    }

    public void OnCoinCollected(int coins)
    {
        _coinValue.Text = coins.ToString();
    }

    public void OnCashUpdated(int cash)
    {
        _cashValue.Text = "$" + cash;
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

    private void OnLevelGenerated(int seed)
    {
        _seedLabel.Text = "Seed: " + seed;
    }

    private void UpdateModeDisplay(GameManager.GameMode mode)
    {
        _modeLabel.Text = mode switch
        {
            GameManager.GameMode.Combat => "COMBAT",
            GameManager.GameMode.Build => "BUILD",
            _ => ""
        };
        _cashRow.Visible = mode == GameManager.GameMode.Build;
    }
}
