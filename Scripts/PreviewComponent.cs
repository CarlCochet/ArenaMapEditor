using Godot;
using System;

public partial class PreviewComponent : Control
{
    public int Index;
    
    [Export] private TextureRect _thumbnail;
    [Export] private TextureButton _button;
    
    public event EventHandler<PressedEventArgs> Pressed;
    
    public override void _Ready() { }

    public void InitAsset(int index, TileData data)
    {
        _thumbnail.Texture = data.Texture;
        Index = index;
    }

    public void Unselect()
    {
        _button.SetPressed(false);
    }

    public void Select(bool selected)
    {
        _button.SetPressed(selected);
    }

    private void _OnPressed()
    {
        Pressed?.Invoke(this, new PressedEventArgs(Index));
    }
    
    public class PressedEventArgs(int index): EventArgs
    {
        public int Index => index;
    }
}
