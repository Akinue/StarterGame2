# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

This is a Godot 4.7 C# project targeting .NET 8.0 with nullable reference types enabled and implicit usings disabled.

```bash
# Build from command line
dotnet build StarterGame2.csproj

# Run from Godot editor (F5) - main scene is res://scenes/main.tscn
```

There are no tests, linting tools, or CI/CD pipelines configured.

## Architecture

A unified game combining three Kenney starter kits (FPS, 3D Platformer, City Builder) into a single Godot C# project with mode switching.

### Core Systems (Autoload Singletons)

- **GameManager** (`scripts/core/GameManager.cs`) — Tracks game state, mode (Combat/Build), cash, and coins. Tab key toggles mode. Emits `ModeChanged` signal.
- **AudioManager** (`scripts/core/AudioManager.cs`) — Pooled audio (12 AudioStreamPlayer nodes). Accepts comma-separated sound paths for random selection. Supports per-sound volume and pitch variation.

Access singletons via: `GetNode<AudioManager>("/root/AudioManager")`

### Game Modes

**Combat Mode** — FPS movement + platformer collectibles:
- `scripts/fps/PlayerController.cs` (378 lines) — First-person controller with WASD movement, mouse look, double-jump, raycast shooting, weapon system, health/damage
- `scripts/fps/Enemy.cs` — Enemy AI with look-at-player, sine-wave movement, timed shooting
- `scripts/fps/GameHud.cs` — HUD with health bar (Kenney UI textures), coin icon, cash display, mode indicator, seed display
- `scripts/platformer/Coin.cs`, `Brick.cs`, `FallingPlatform.cs` — Collectibles and destructibles

**Build Mode** — Grid-based city builder:
- `scripts/builder/Builder.cs` (254 lines) — Raycast placement on GridMap, structure cycling, rotation, save/load to `user://map.res`
- `scripts/builder/StructureResource.cs` — Resource with Model (PackedScene) and Price
- `scripts/builder/DataMap.cs` / `DataStructure.cs` — Serialization for save/load

### Procedural Level Generation

- **LevelGenerator** (`scripts/core/LevelGenerator.cs`) — Generates platforms, enemies, coins, and clouds at runtime. Configurable via exports: Seed, PlatformCount, EnemyCount, CoinChance, CloudCount, MaxRadius, MaxHeight. Seed=0 means random. Emits `LevelGenerated(int seed)` signal.
  - Platforms: Starting platform at origin + spread pattern with minimum spacing. Mix of large grass and small platforms. Height increases with distance.
  - Enemies: Placed near random platforms (not the start), offset above.
  - Coins: 60% chance per platform + tutorial coin near start.
  - Clouds: Decorative, random positions at high altitude with random scale.

### Scene Structure

Main scene (`scenes/main.tscn`) contains environment (panorama skybox), lighting, floor, Player, HUD (CanvasLayer), GridMap, Builder, and LevelGenerator. Prefab scenes live in `objects/` organized by game mode.

## Code Conventions

All classes use `namespace StarterGame2;` and `partial class` (required by Godot).

- **Properties**: PascalCase with `[Export]` for editor-exposed values, grouped with `[ExportSubgroup("Name")]`
- **Private fields**: `_camelCase` (e.g., `_audio`, `_weaponIndex`)
- **Signal handlers**: `OnEventName` pattern
- **Delta casting**: `float dt = (float)delta;` at top of `_Process`/`_PhysicsProcess`
- **Signals**: Modern C# delegate syntax — `[Signal] public delegate void NameEventHandler(params);`
- **Async**: `await ToSignal(GetTree().CreateTimer(1.0), SceneTreeTimer.SignalName.Timeout)` for delays
- **Damage**: Duck-typed via `node.HasMethod("Damage")` then `node.Call("Damage", amount)`

## Asset Layout

- `models/` — GLB files per game mode with shared colormap textures
- `sounds/` — OGG audio files per game mode
- `sprites/` — 2D textures (crosshairs, shadows, particles, skyboxes)
- `sprites/ui/` — Kenney UI Pack assets (health bar textures, icons)
- `weapons/` — WeaponResource `.tres` files (blaster, blaster-repeater)
- `structures/` — StructureResource `.tres` files (roads, buildings, grass, pavement)
- `fonts/` — Lilita One TTF
