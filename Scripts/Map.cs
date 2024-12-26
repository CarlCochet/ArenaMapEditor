using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    [Export] private Node2D _assetContainer;
    [Export] private Camera _camera;
    [Export] private PackedScene _tileScene;

    private List<Tile> _tiles = [];

    public override void _Ready()
    {
        
    }

    public void UpdateFocus(bool hasFocus)
    {
        _camera.HasFocus = hasFocus;
    }

    public void Load(MapData mapData)
    {
        var children = _assetContainer.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
        
        var sortedElements = mapData.Gfx.Partitions
            .SelectMany(p => p.Elements)
            .OrderBy(t => t.HashCode)
            .ToList();

        foreach (var element in sortedElements)
        {
            var tile = _tileScene.Instantiate<Tile>();
            tile.SetData(element);
            tile.PositionToIso(element.CellX, element.CellY, element.CellZ, element.Height, element.CommonData.OriginX, element.CommonData.OriginY);
            tile.FlipH = element.CommonData.Flip;
            _assetContainer.AddChild(tile);
        }
    }

    private (int x, int y) PositionToCoord(Vector2 globalPosition)
    {
        return (0, 0);
    }
}
