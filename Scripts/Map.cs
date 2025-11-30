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
    [Export] private Node2D _topology;
    [Export] private Node2D _light;
    [Export] private Node2D _fight;
    [Export] private PackedScene _tileScene;
    [Export] private Sprite2D _grid;
    [Export] private Sprite2D _grid2;
    [Export] private PlacementPreview _placementPreview;
    
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;

    private List<Tile> _gfxTiles = [];
    private List<Tile> _topologyTiles = [];
    private List<Tile> _fightTiles = [];
    
    private Color _selectColor = new(0, 255, 0);
    private Color _noHighlightColor = new(0, 255, 0, 64);
    
    private ShaderMaterial _gridMaterial;
    private ShaderMaterial _grid2Material;
    private MapData _mapData;
    private Enums.Mode _mode;
    private bool _isHighlightActivated;
    
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

    public void UpdateHeightHighlight(int z)
    {
        if (!_isHighlightActivated)
            return;
        
        foreach (var tile in _gfxTiles)
        {
            if (tile.Z == z)
            {
                tile.Highlight();
                continue;
            }
            tile.RemoveHighlight();
        }
    }

    public void UpdateElement(GfxData.Element elementData)
    {
        _mapData.UpdateElement(elementData);
        SelectedTiles.FirstOrDefault(t => t.Mode == Enums.Mode.Gfx)?.SetElementData(elementData);
    }

    public void UpdateTopology(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
    {
        _mapData.UpdateTopology(path, visibility);
        SelectedTiles.FirstOrDefault(t => t.Mode == Enums.Mode.Topology)?.SetTopology(path, visibility);
    }

    public void UpdateDisplay(Enums.Mode mode)
    {
        _mode = mode;
        ResetDisplay();
        switch (_mode)
        {
            case Enums.Mode.Gfx:
                _gfx.Visible = true;
                break;
            case Enums.Mode.Topology:
                _topology.Visible = true;
                break;
            case Enums.Mode.Light:
                _light.Visible = true;
                break;
            case Enums.Mode.Fight:
                _fight.Visible = true;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    public void Load(MapData mapData)
    {
        _mapData = mapData;
        Clear();
        
        var sortedElements = _mapData.Gfx.Partitions
            .SelectMany(p => p.Elements)
            .OrderBy(t => t.HashCode)
            .ToList();

        foreach (var element in sortedElements)
        {
            var tile = _tileScene.Instantiate<Tile>();
            tile.SetElementData(element);
            _gfx.AddChild(tile);
            _gfxTiles.Add(tile);
        }
        
        var topology = _mapData.Topology;
        for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
        {
            for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
            {
                var cellVisibilityData = topology.GetVisibilityData(x, y);
                var cellPathData = topology.GetPathData(x, y);
                if (cellVisibilityData == null || cellPathData == null)
                    continue;
                if (cellVisibilityData.CanViewThrough && cellPathData.CanMoveThrough) 
                    continue;
                
                var tile = _tileScene.Instantiate<Tile>();
                tile.SetTopology(cellPathData, cellVisibilityData);
                _topology.AddChild(tile);
                _topologyTiles.Add(tile);
            }
        }
        
        UpdateDisplay(Enums.Mode.Gfx);
    }

    public (int x, int y) PositionToCoord(Vector2 position, int height)
    {
        var posX = position.X + CellWidth * 0.5f;
        var posY = position.Y + CellHeight * 0.5f - height * ElevationStep;
        
        var x = posX / CellWidth + posY / CellHeight;
        var y = posY / CellHeight - posX / CellWidth;
        return ((int)x, (int)y);
    }

    public void ToggleHeightHighlight(bool toggledOn, int z)
    {
        _isHighlightActivated = toggledOn;
        if (!_isHighlightActivated)
        {
            _gfxTiles.ForEach(t => t.Highlight());
            return;
        }
        
        foreach (var tile in _gfxTiles)
        {
            if (tile.Z == z)
            {
                tile.Highlight();
                continue;
            }
            tile.RemoveHighlight();
        }
    }

    public void GenerateTopology()
    {
        
    }

    private void SelectTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;
        
        SelectedTiles.ForEach(t => t.Unselect());
        SelectedTiles.Clear();
        
        var selectedTile = _gfxTiles
            .FindAll(t => t.IsValidPixel(position))
            .OrderBy(t => t.Element.HashCode)
            .LastOrDefault();
        if (selectedTile == null)
            return;

        var topologyTile = _topologyTiles.FirstOrDefault(t => t.X == selectedTile.X && t.Y == selectedTile.Y);
        if (topologyTile == null)
            return;
        
        SelectedTiles.Add(selectedTile);
        SelectedTiles.Add(topologyTile);
        SelectedTiles.ForEach(t => t.Select());
        
        _gridMaterial.SetShaderParameter("elevation", (float)(selectedTile.Z * ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(selectedTile.Z * ElevationStep));
        var cellPathData = _mapData.Topology.GetPathData(selectedTile.X, selectedTile.Y);
        var visibilityData = _mapData.Topology.GetVisibilityData(selectedTile.X, selectedTile.Y);
        TileSelected?.Invoke(this, new TileSelectedEventArgs(selectedTile.Element, cellPathData, visibilityData));
    }

    public void Clear()
    {
        foreach (var child in _gfx.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _fight.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _light.GetChildren())
        {
            child.QueueFree();
        }
        foreach (var child in _topology.GetChildren())
        {
            child.QueueFree();
        }
        
        _gfxTiles.Clear();
        _topologyTiles.Clear();
        _fightTiles.Clear();
    }
    
    private void _OnZoomUpdated(object sender, Camera.ZoomUpdatedEventArgs e)
    {
        _gridMaterial.SetShaderParameter("zoom", e.Zoom);
        _grid2Material.SetShaderParameter("zoom", e.Zoom);
    }

    private void DisplayGfx()
    {
        ResetDisplay();
    }

    // private void DisplayPath()
    // {
    //     ResetDisplay();
    //     _path.Visible = true;
    //     
    //     var topology = _mapData.Topology;
    //     for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
    //     {
    //         for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
    //         {
    //             var cellPathData = topology.GetPathData(x, y);
    //             if (cellPathData == null)
    //                 continue;
    //
    //             var tile = _tileScene.Instantiate<Tile>();
    //             tile.SetPathData(cellPathData);
    //             tile.PositionToIso(cellPathData.X - 1, cellPathData.Y, cellPathData.Z, 0, 0, 0);
    //             _path.AddChild(tile);
    //             // _tiles.Add(tile);
    //         }
    //     }
    // }
    
    private void DisplayTopology()
    {
        ResetDisplay();
        _topology.Visible = true;

        // var topology = _mapData.Topology;
        // for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
        // {
        //     for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
        //     {
        //         var cellVisibilityData = topology.GetVisibilityData(x, y);
        //         if (cellVisibilityData == null)
        //             continue;
        //
        //         if (cellVisibilityData.CanViewThrough) 
        //             continue;
        //         
        //         var tile = _tileScene.Instantiate<Tile>();
        //         tile.SetVisibilityData(cellVisibilityData);
        //         tile.PositionToIso(cellVisibilityData.X, cellVisibilityData.Y, cellVisibilityData.Z - cellVisibilityData.Height, -cellVisibilityData.Height, 0, 0);
        //         _topology.AddChild(tile);
        //         // _tiles.Add(tile);
        //     }
        // }
    }

    private void DisplayLight()
    {
        ResetDisplay();
        _light.Visible = true;
    }

    private void DisplayFight()
    {
        ResetDisplay();
        _fight.Visible = true;
    }

    private void ResetDisplay()
    {
        SelectedTiles?.ForEach(t => t.Unselect());
        SelectedTiles = [];
        _fight.Visible = false;
        _light.Visible = false;
        _topology.Visible = false;
    }
    
    public class TileSelectedEventArgs(GfxData.Element element, TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visibilityData) : EventArgs
    {
        public GfxData.Element Element => element;
        public TopologyData.CellPathData PathData => pathData;
        public TopologyData.CellVisibilityData VisibilityData => visibilityData;
    }
}
