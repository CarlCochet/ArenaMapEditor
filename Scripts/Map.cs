using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    [Export] private Node2D _assetContainer;
    [Export] private Camera _camera;

    public override void _Ready() { }

    public void UpdateFocus(bool hasFocus)
    {
        _camera.HasFocus = hasFocus;
    }

    public void LoadMap(MapInfo mapInfo)
    {
        foreach (var partition in mapInfo.Partitions)
        {
            foreach (var element in partition.Elements)
            {
                var tile = new Tile();
                tile.SetData(GlobalData.Instance.Assets[element.CommonData.GfxId]);
                tile.Position = new Vector2(element.CellX * 43 + element.CellY * 21.5f, element.CellX * 21.5f + element.CellY * 43 + element.CellZ * 9);
                tile.Offset = new Vector2(-element.CommonData.OriginX, -element.CommonData.OriginY);
                _assetContainer.AddChild(tile);
            }
        }
    }
}