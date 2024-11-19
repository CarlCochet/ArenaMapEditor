using Godot;
using System;

public partial class PreviewComponent : Control
{
    [Export] private TextureRect _thumbnail;
    
    public override void _Ready() { }

    public void DisplayAsset(TileData data)
    {
        _thumbnail.Texture = data.Texture;
    }

    private void _OnToggled(bool toggledOn)
    {
        
    }
}
