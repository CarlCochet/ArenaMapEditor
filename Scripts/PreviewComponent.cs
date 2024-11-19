using Godot;
using System;

public partial class PreviewComponent : Control
{
    public int Index;
    
    [Export] private TextureRect _thumbnail;
    [Export] private TextureButton _button;
    
    public event EventHandler<ToggledOnEventArgs> ToggledOn;
    
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

    private void _OnToggled(bool toggledOn)
    {
        ToggledOn?.Invoke(this, new ToggledOnEventArgs(Index, toggledOn));
    }

    public class ToggledOnEventArgs(int index, bool toggledOn): EventArgs
    {
        public int Index => index;
        public bool ToggledOn => toggledOn;
    }
}
