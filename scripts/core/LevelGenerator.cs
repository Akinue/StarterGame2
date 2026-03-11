using Godot;
using System.Collections.Generic;

namespace StarterGame2;

/// <summary>
/// Procedurally generates a level with platforms, enemies, coins, and clouds.
/// Attach to a Node3D in the main scene. Set Seed to 0 for random levels.
/// </summary>
public partial class LevelGenerator : Node3D
{
    [ExportSubgroup("Seed")]
    [Export] public int Seed { get; set; } = 0;

    [ExportSubgroup("Platforms")]
    [Export] public int PlatformCount { get; set; } = 14;
    [Export] public float MinPlatformSpacing { get; set; } = 6f;
    [Export] public float MaxRadius { get; set; } = 40f;
    [Export] public float MaxHeight { get; set; } = 12f;

    [ExportSubgroup("Enemies")]
    [Export] public int EnemyCount { get; set; } = 4;

    [ExportSubgroup("Coins")]
    [Export(PropertyHint.Range, "0,1")] public float CoinChance { get; set; } = 0.6f;

    [ExportSubgroup("Clouds")]
    [Export] public int CloudCount { get; set; } = 10;
    [Export] public float CloudMinHeight { get; set; } = 8f;
    [Export] public float CloudMaxHeight { get; set; } = 22f;

    private PackedScene _platformLg = null!;
    private PackedScene _platformSm = null!;
    private PackedScene _enemyScene = null!;
    private PackedScene _coinScene = null!;
    private PackedScene _cloudScene = null!;

    private RandomNumberGenerator _rng = new();

    [Signal]
    public delegate void LevelGeneratedEventHandler(int seed);

    public override void _Ready()
    {
        LoadScenes();

        if (Seed == 0)
            Seed = (int)(GD.Randi() % 999999) + 1;
        _rng.Seed = (ulong)Seed;

        GenerateLevel();
        EmitSignal(SignalName.LevelGenerated, Seed);
        GD.Print($"Level generated with seed: {Seed}");
    }

    private void LoadScenes()
    {
        _platformLg = GD.Load<PackedScene>("res://objects/fps/platform_large_grass.tscn");
        _platformSm = GD.Load<PackedScene>("res://objects/fps/platform.tscn");
        _enemyScene = GD.Load<PackedScene>("res://objects/fps/enemy.tscn");
        _coinScene = GD.Load<PackedScene>("res://objects/platformer/coin.tscn");
        _cloudScene = GD.Load<PackedScene>("res://objects/fps/cloud.tscn");
    }

    private void GenerateLevel()
    {
        var platforms = GeneratePlatforms();
        SpawnEnemies(platforms);
        SpawnCoins(platforms);
        SpawnClouds();
    }

    private List<Vector3> GeneratePlatforms()
    {
        var positions = new List<Vector3>();

        // Always place a starting platform near the player spawn
        var startPos = new Vector3(0, 0, 0);
        SpawnPlatform(startPos, true);
        positions.Add(startPos);

        // Generate a ring of nearby easy-to-reach platforms
        for (int i = 0; i < 3; i++)
        {
            float angle = _rng.RandfRange(0, Mathf.Tau);
            float dist = _rng.RandfRange(6f, 10f);
            float height = _rng.RandfRange(0.5f, 2.5f);
            var pos = new Vector3(Mathf.Cos(angle) * dist, height, Mathf.Sin(angle) * dist);

            if (!IsTooClose(pos, positions))
            {
                SpawnPlatform(pos, true);
                positions.Add(pos);
            }
        }

        // Generate remaining platforms spreading outward
        int remaining = PlatformCount - positions.Count;
        for (int i = 0; i < remaining; i++)
        {
            Vector3 pos = Vector3.Zero;
            bool placed = false;

            for (int attempt = 0; attempt < 60; attempt++)
            {
                float angle = _rng.RandfRange(0, Mathf.Tau);
                float dist = _rng.RandfRange(8f, MaxRadius);
                float heightRatio = dist / MaxRadius;
                float height = heightRatio * MaxHeight + _rng.RandfRange(-2f, 2f);
                height = Mathf.Max(0.5f, height);

                pos = new Vector3(Mathf.Cos(angle) * dist, height, Mathf.Sin(angle) * dist);

                if (!IsTooClose(pos, positions))
                {
                    placed = true;
                    break;
                }
            }

            if (!placed) continue;

            bool isLarge = _rng.Randf() > 0.4f;
            SpawnPlatform(pos, isLarge);
            positions.Add(pos);
        }

        return positions;
    }

    private void SpawnPlatform(Vector3 pos, bool isLarge)
    {
        var scene = isLarge ? _platformLg : _platformSm;
        var instance = scene.Instantiate<Node3D>();
        instance.Position = pos;

        // Add slight random rotation for visual variety
        instance.RotationDegrees = new Vector3(0, _rng.RandfRange(0, 360), 0);

        AddChild(instance);
    }

    private bool IsTooClose(Vector3 pos, List<Vector3> existing)
    {
        foreach (var other in existing)
        {
            if (pos.DistanceTo(other) < MinPlatformSpacing)
                return true;
        }
        return false;
    }

    private void SpawnEnemies(List<Vector3> platforms)
    {
        if (platforms.Count < 2) return;

        // Skip the starting platform (index 0) for enemy placement
        var candidates = new List<int>();
        for (int i = 1; i < platforms.Count; i++)
            candidates.Add(i);

        Shuffle(candidates);

        int count = Mathf.Min(EnemyCount, candidates.Count);
        for (int i = 0; i < count; i++)
        {
            var platPos = platforms[candidates[i]];
            var enemy = _enemyScene.Instantiate<Node3D>();
            enemy.Position = platPos + new Vector3(
                _rng.RandfRange(-2f, 2f),
                _rng.RandfRange(1.5f, 3f),
                _rng.RandfRange(-2f, 2f)
            );
            AddChild(enemy);
        }
    }

    private void SpawnCoins(List<Vector3> platforms)
    {
        // Skip starting platform for coins
        for (int i = 1; i < platforms.Count; i++)
        {
            if (_rng.Randf() > CoinChance) continue;

            var pos = platforms[i];
            var coin = _coinScene.Instantiate<Node3D>();
            coin.Position = pos + new Vector3(
                _rng.RandfRange(-1f, 1f),
                0.6f,
                _rng.RandfRange(-1f, 1f)
            );
            AddChild(coin);
        }

        // Also place a coin near the start as a tutorial hint
        var startCoin = _coinScene.Instantiate<Node3D>();
        startCoin.Position = new Vector3(2f, 0.5f, -2f);
        AddChild(startCoin);
    }

    private void SpawnClouds()
    {
        for (int i = 0; i < CloudCount; i++)
        {
            float angle = _rng.RandfRange(0, Mathf.Tau);
            float dist = _rng.RandfRange(10f, 55f);
            float height = _rng.RandfRange(CloudMinHeight, CloudMaxHeight);
            float scale = _rng.RandfRange(2f, 5f);

            var cloud = _cloudScene.Instantiate<Node3D>();
            cloud.Position = new Vector3(
                Mathf.Cos(angle) * dist,
                height,
                Mathf.Sin(angle) * dist
            );
            cloud.Scale = Vector3.One * scale;
            AddChild(cloud);
        }
    }

    private void Shuffle(List<int> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = (int)(_rng.Randi() % (uint)(i + 1));
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
