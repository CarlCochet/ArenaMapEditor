using Godot;
using System;

public partial class PlacementPreview : Sprite2D
{
    public ElementData ElementData { get; set; }
    public TileData TileData { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public short Z { get; set; }

    public override void _Ready() { }

    public void SetAsset(ElementData data)
    {
        ElementData = data;
        if (!GlobalData.Instance.ValidAssets.TryGetValue(ElementData.GfxId, out var asset))
            return;
        
        TileData = asset.Copy();
        Texture = asset.Texture;
    }
    
    public void PositionToIso(int x, int y, int z)
    {
        if (ElementData == null) 
            return;
        
        X = x;
        Y = y;
        Z = (short)z;
        
        var newX = (x - y) * GlobalData.CellWidth * 0.5f;
        var newY = (x + y) * GlobalData.CellHeight * 0.5f - (z - ElementData.VisualHeight) * GlobalData.ElevationStep;
        Offset = Offset with { X = -ElementData.OriginX, Y = -ElementData.OriginY };
        Position = new Vector2(newX, newY);
    }
}
