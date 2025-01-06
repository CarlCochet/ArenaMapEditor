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
    
    private Color _highlightColor = new(0, 255, 0);
    
    public event EventHandler<TileSelectedEventArgs> TileSelected; 

    public override void _Ready() { }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
        {
            var globalPosition = GetGlobalMousePosition();
            SelectTile(globalPosition);
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
        var posX = position.X + CellWidth * 0.5f;
        var posY = position.Y + CellHeight * 0.5f - height * ElevationStep;
        
        var x = posX / CellWidth + posY / CellHeight;
        var y = posY / CellHeight - posX / CellWidth;
        return ((int)x, (int)y);
    }

    private void SelectTile(Vector2 position)
    {
        foreach (var tile in _selectedTiles)
        {
            tile.Unselect();
        }
        
        _selectedTiles = _tiles
            .FindAll(t => t.IsValidPixel(position))
            .OrderBy(t => t.Element.HashCode)
            .TakeLast(1)
            .ToList();

        foreach (var tile in _selectedTiles)
        {
            tile.Select();
            TileSelected?.Invoke(this, new TileSelectedEventArgs(tile.Element));
        }
    }

    public class TileSelectedEventArgs(GfxData.Element element) : EventArgs
    {
        public GfxData.Element Element => element;
    }
}
