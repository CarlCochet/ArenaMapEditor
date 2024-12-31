using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public int CurrentHeight { get; set; }
    
    [Export] private Node2D _assetContainer;
    [Export] private Camera _camera;
    [Export] private PackedScene _tileScene;
    
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;

    private List<Tile> _tiles = [];
    private List<Tile> _selectedTiles = [];

    public override void _Ready() { }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
        {
            foreach (var tile in _selectedTiles)
            {
                tile.ResetColor();
            }
            
            _selectedTiles.Clear();
            
            foreach (var tile in _tiles)
            {
                var globalPosition = GetGlobalMousePosition();
                if (!tile.GetRect().HasPoint(tile.ToLocal(globalPosition)))
                    continue;
                tile.SelfModulate = new Color(0, 255, 0);
                _selectedTiles.Add(tile);
            }
        }
    }

    public void UpdateFocus(bool hasFocus)
    {
        _camera.HasFocus = hasFocus;
    }

    public void UpdateHeight(int height)
    {
        
    }

    public void Load(MapData mapData)
    {
        var children = _assetContainer.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }

        foreach (var partition in mapData.Gfx.Partitions)
        {
            float[] colors = [GlobalData.Instance.Rng.Randf(), GlobalData.Instance.Rng.Randf(), GlobalData.Instance.Rng.Randf()];
            foreach (var element in partition.Elements)
            {
                element.Colors = colors;
            }
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
            _tiles.Add(tile);
        }
    }

    public void HighlightTiles(Vector2 position)
    {
        foreach (var tile in _tiles)
        {
            
        }
    }

    public (int x, int y) PositionToCoord(Vector2 position, int height)
    {
        var x = position.X / CellWidth - position.Y / CellHeight;
        var y = -(position.X / CellWidth + position.Y / CellHeight);
        return ((int)x, (int)y);
    }
}
