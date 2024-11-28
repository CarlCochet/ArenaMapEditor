using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    [Export] private Node2D _assetContainer;
    [Export] private Camera _camera;
    [Export] private PackedScene _tileScene;

    private List<Chunk> _chunks = [];

    public override void _Ready() { }

    public void UpdateFocus(bool hasFocus)
    {
        _camera.HasFocus = hasFocus;
    }

    public void LoadMap(MapInfo mapInfo)
    {
        var children = _assetContainer.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
        
        var sortedTiles = mapInfo.Partitions
            .SelectMany(p => p.Elements)
            .OrderBy(t => t.HashCode)
            .ToList();

        foreach (var tileData in sortedTiles)
        {
            var tile = _tileScene.Instantiate<Tile>();
            tile.SetData(tileData);
            tile.PositionToIso(tileData.CellX, tileData.CellY, tileData.CellZ, tileData.Height, tileData.CommonData.OriginX, tileData.CommonData.OriginY);
            tile.FlipH = tileData.CommonData.Flip;
            _assetContainer.AddChild(tile);
        }
    }
}
