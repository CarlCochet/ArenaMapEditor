using Godot;
using System;

public partial class PreviewComponent : Control
{
    public int Index;
    public int GfxId;
    
    [Export] private TextureRect _thumbnail;
    [Export] private TextureButton _button;
    
    public event EventHandler<PressedEventArgs> Pressed;
    
    public override void _Ready() { }

    public void InitAsset(int index, TileData data)
    {
        _thumbnail.Texture = data.Texture;
        Index = index;
        GfxId = data.Id;
    }

    public void Select(bool selected)
    {
        _button.SetPressed(selected);
    }

    private void _OnPressed()
    {
        Pressed?.Invoke(this, new PressedEventArgs(GfxId));
    }
    
    public class PressedEventArgs(int gfxId): EventArgs
    {
        public int GfxId => gfxId;
    }
}
