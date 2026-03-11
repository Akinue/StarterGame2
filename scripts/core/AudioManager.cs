using Godot;
using System.Collections.Generic;

namespace StarterGame2;

/// <summary>
/// Audio manager singleton (autoload). Pools AudioStreamPlayers for efficient sound playback.
/// Converted from Kenney's audio.gd — supports comma-separated random sound selection
/// and per-sound volume control.
/// </summary>
public partial class AudioManager : Node
{
    private const int NumPlayers = 12;
    private const string Bus = "Master";

    private readonly List<AudioStreamPlayer> _available = new();
    private readonly Queue<(string Path, float Volume)> _queue = new();

    public override void _Ready()
    {
        for (int i = 0; i < NumPlayers; i++)
        {
            var player = new AudioStreamPlayer();
            AddChild(player);
            _available.Add(player);

            player.VolumeDb = -10;
            player.Bus = Bus;
            player.Finished += () => _available.Add(player);
        }
    }

    /// <summary>
    /// Play a sound. Supports comma-separated paths for random selection.
    /// Example: "sounds/fps/jump_a.ogg, sounds/fps/jump_b.ogg"
    /// </summary>
    public void Play(string soundPath, float volumeDb = -10f)
    {
        string[] sounds = soundPath.Split(',');
        string chosen = sounds[GD.Randi() % sounds.Length].Trim();

        if (!chosen.StartsWith("res://"))
            chosen = "res://" + chosen;

        _queue.Enqueue((chosen, volumeDb));
    }

    public override void _Process(double delta)
    {
        while (_queue.Count > 0 && _available.Count > 0)
        {
            var (path, volume) = _queue.Dequeue();
            var player = _available[0];
            _available.RemoveAt(0);

            player.Stream = GD.Load<AudioStream>(path);
            player.VolumeDb = volume;
            player.PitchScale = (float)GD.RandRange(0.9, 1.1);
            player.Play();
        }
    }
}
