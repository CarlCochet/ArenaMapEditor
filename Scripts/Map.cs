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
    [Export] private Sprite2D _grid2;
    
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;

    private List<Tile> _tiles = [];
    private Color _highlightColor = new(0, 255, 0);
    private ShaderMaterial _gridMaterial;
    private ShaderMaterial _grid2Material;
    private MapData _mapData;
    
    public event EventHandler<TileSelectedEventArgs> TileSelected;

    public override void _Ready()
    {
        _gridMaterial = (ShaderMaterial)_grid.Material;
        _grid2Material = (ShaderMaterial)_grid2.Material;
        CustomCamera.ZoomUpdated += _OnZoomUpdated;
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            _gridMaterial.SetShaderParameter("mouse_position", GetGlobalMousePosition());
            _grid2Material.SetShaderParameter("mouse_position", GetGlobalMousePosition());
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
            case Enums.Mode.GfxCurrent:
                DisplayGfx();
                break;
            case Enums.Mode.Path:
                DisplayPath();
                break;
            case Enums.Mode.Visibility:
                DisplayVisibility();
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
        Clear();
        UpdateDisplay(mode);
    }

    public void Save(string path)
    {
        _mapData.Save(path);
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
            _grid2Material.SetShaderParameter("elevation", (float)(tile.Element.CellZ * ElevationStep));
            TileSelected?.Invoke(this, new TileSelectedEventArgs(tile.Element));
        }
    }

    public class TileSelectedEventArgs(GfxData.Element element) : EventArgs
    {
        public GfxData.Element Element => element;
    }

    public void Clear()
    {
        _tiles.Clear();
        _gfx.QueueFree();
        _gfx = new Node2D();
        AddChild(_gfx);
    }
    
    private void _OnZoomUpdated(object sender, Camera.ZoomUpdatedEventArgs e)
    {
        _gridMaterial.SetShaderParameter("zoom", e.Zoom);
        _grid2Material.SetShaderParameter("zoom", e.Zoom);
    }

    private void DisplayGfx()
    {
        try
        {
            SelectedTiles.Clear();
            _gfx.Modulate = Colors.White;
            _fight.Visible = false;
            _light.Visible = false;
            _path.Visible = false;
            _visibility.Visible = false;

            var sortedElements = _mapData.Gfx.Partitions
                .SelectMany(p => p.Elements)
                .OrderBy(t => t.HashCode)
                .ToList();

            foreach (var element in sortedElements)
            {
                var tile = _tileScene.Instantiate<Tile>();
                tile.SetElementData(element);
                tile.PositionToIso(element.CellX, element.CellY, element.CellZ, element.Height, element.CommonData.OriginX, element.CommonData.OriginY);
                tile.FlipH = element.CommonData.Flip;
                _gfx.AddChild(tile);
                _tiles.Add(tile);
            }
        }
        catch (Exception e)
        {
            GD.Print("Error: " + e.Message);
        }
    }

    private void DisplayPath()
    {
        SelectedTiles.Clear();
        _gfx.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        _fight.Visible = false;
        _light.Visible = false;
        _path.Visible = true;
        _visibility.Visible = false;
        
        var topology = _mapData.Topology;
        for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
        {
            for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
            {
                var cellPathData = topology.GetPathData(x, y);
                if (cellPathData == null)
                {
                    continue;
                }

                foreach (var pathData in cellPathData)
                {
                    var tile = _tileScene.Instantiate<Tile>();
                    tile.SetPathData(pathData);
                    tile.PositionToIso(pathData.X, pathData.Y, 0, 0, 0, 0);
                    _path.AddChild(tile);
                    _tiles.Add(tile);
                }
            }
        }
    }
    
    private void DisplayVisibility()
    {
        SelectedTiles.Clear();
        _gfx.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.1f);
        _fight.Visible = false;
        _light.Visible = false;
        _path.Visible = false;
        _visibility.Visible = true;

        var topology = _mapData.Topology;
        for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
        {
            for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
            {
                var cellVisibilityData = topology.GetVisibilityData(x, y);
                if (cellVisibilityData == null)
                {
                    continue;
                }

                foreach (var visibilityData in cellVisibilityData)
                {
                    var tile = _tileScene.Instantiate<Tile>();
                    tile.SetVisibilityData(visibilityData);
                    tile.PositionToIso(visibilityData.X, visibilityData.Y, 0, 0, 0, 0);
                    _visibility.AddChild(tile);
                    _tiles.Add(tile);
                }
            }
        }
    }

    private void DisplayLight()
    {
        SelectedTiles.Clear();
        _gfx.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        _fight.Visible = false;
        _light.Visible = true;
        _path.Visible = false;
        _visibility.Visible = false;
        
        
    }

    private void DisplayFight()
    {
        SelectedTiles.Clear();
        _gfx.Modulate = new Color(1.0f, 1.0f, 1.0f, 0.5f);
        _fight.Visible = true;
        _light.Visible = false;
        _path.Visible = false;
        _visibility.Visible = false;
        
        
    }
}
