using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public int CurrentHeight { get; set; }
    public List<Tile> SelectedTiles = [];
    
    [Export] public Camera CustomCamera;
    
    [Export] private Node2D _gfx;
    [Export] private Node2D _path;
    [Export] private Node2D _visibility;
    [Export] private Node2D _light;
    [Export] private Node2D _fight;
    [Export] private PackedScene _tileScene;
    [Export] private Sprite2D _grid;
    
    
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;

    private List<Tile> _tiles = [];
    private Color _highlightColor = new(0, 255, 0);
    private ShaderMaterial _gridMaterial;
    private MapData _mapData;
    
    public event EventHandler<TileSelectedEventArgs> TileSelected;

    public override void _Ready()
    {
        _gridMaterial = (ShaderMaterial)_grid.Material;
        CustomCamera.ZoomUpdated += _OnZoomUpdated;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            _gridMaterial.SetShaderParameter("mouse_position", GetGlobalMousePosition());
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
        {
            var globalPosition = GetGlobalMousePosition();
            SelectTile(globalPosition);
        }
    }

    public void UpdateFocus(bool hasFocus)
    {
        CustomCamera.HasFocus = hasFocus;
    }

    public void UpdateHeight(int height)
    {
        
    }

    public void UpdateDisplay(Enums.Mode mode)
    {
        switch (mode)
        {
            case Enums.Mode.Gfx:
            case Enums.Mode.GfxPresent:
                DisplayGfx();
                break;
            case Enums.Mode.Topology:
                DisplayTopology();
                break;
            case Enums.Mode.Light:
                DisplayLight();
                break;
            case Enums.Mode.Fight:
                DisplayFight();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    public void Load(MapData mapData, Enums.Mode mode)
    {
        _mapData = mapData;
        UpdateDisplay(mode);
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
        foreach (var tile in SelectedTiles)
        {
            tile.Unselect();
        }
        
        SelectedTiles = _tiles
            .FindAll(t => t.IsValidPixel(position))
            .OrderBy(t => t.Element.HashCode)
            .TakeLast(1)
            .ToList();

        foreach (var tile in SelectedTiles)
        {
            tile.Select();
            _gridMaterial.SetShaderParameter("elevation", (float)(tile.Element.CellZ * ElevationStep));
            TileSelected?.Invoke(this, new TileSelectedEventArgs(tile.Element));
        }
    }

    public class TileSelectedEventArgs(GfxData.Element element) : EventArgs
    {
        public GfxData.Element Element => element;
    }
    
    private void _OnZoomUpdated(object sender, Camera.ZoomUpdatedEventArgs e)
    {
        _gridMaterial.SetShaderParameter("zoom", e.Zoom);
    }

    private void DisplayGfx()
    {
        _tiles.Clear();
        SelectedTiles.Clear();
        _gfx.QueueFree();
        _gfx = new Node2D();
        AddChild(_gfx);

        var sortedElements = _mapData.Gfx.Partitions
            .SelectMany(p => p.Elements)
            .OrderBy(t => t.HashCode)
            .ToList();

        foreach (var element in sortedElements)
        {
            var tile = _tileScene.Instantiate<Tile>();
            tile.SetData(element);
            tile.PositionToIso(element.CellX, element.CellY, element.CellZ, element.Height, element.CommonData.OriginX, element.CommonData.OriginY);
            tile.FlipH = element.CommonData.Flip;
            _gfx.AddChild(tile);
            _tiles.Add(tile);
        }
    }

    private void DisplayTopology()
    {
        _tiles.Clear();
        SelectedTiles.Clear();
        
        var children = _gfx.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
    }

    private void DisplayLight()
    {
        _tiles.Clear();
        SelectedTiles.Clear();
        
        var children = _gfx.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
    }

    private void DisplayFight()
    {
        _tiles.Clear();
        SelectedTiles.Clear();
        
        var children = _gfx.GetChildren();
        foreach (var child in children)
        {
            child.QueueFree();
        }
    }
}
