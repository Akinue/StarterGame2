using Godot;

namespace StarterGame2;

/// <summary>
/// Impact effect that self-destructs after animation.
/// Converted from Kenney's impact.gd.
/// </summary>
public partial class Impact : AnimatedSprite3D
{
    public void OnAnimationFinished()
    {
        QueueFree();
    }
}
