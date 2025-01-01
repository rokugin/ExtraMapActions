using Microsoft.Xna.Framework;

namespace ExtraMapActions.Models;

public sealed class CustomDoorsModel {

    public string? Texture { get; set; }
    public Rectangle SourceRect { get; set; }
    public bool Flip { get; set; } = false;
    public int AnimationFrames { get; set; } = 4;
    public float FrameDuration { get; set; } = 100f;

}