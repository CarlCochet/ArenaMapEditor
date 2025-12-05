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
    private int _z;
    
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
            var mousePosition = GetGlobalMousePosition();
            _gridMaterial.SetShaderParameter("mouse_position", mousePosition);
            _grid2Material.SetShaderParameter("mouse_position", mousePosition);
            
            var coords = PositionToCoord(mousePosition, _z);
            _placementPreview.PositionToIso(coords.x, coords.y, _z);
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
        {
            var globalPosition = GetGlobalMousePosition();
            SelectTile(globalPosition);
        }
    }

    public void UpdateFocus(bool hasFocus) => CustomCamera.HasFocus = hasFocus;

    public void UpdateHeight(int z)
    {
        _z = z;
        
        if (!_isHighlightActivated)
            return;
        
        foreach (var tile in _gfxTiles)
        {
            if (tile.Z == _z)
            {
                tile.Highlight();
                continue;
            }
            tile.RemoveHighlight();
        }
    }

    public void UpdateElement(GfxData.Element oldElement, GfxData.Element newElement)
    {
        _mapData.UpdateElement(oldElement, newElement);
        SelectedTiles.FirstOrDefault(t => t.Mode == Enums.Mode.Gfx)?.SetElementData(newElement);
    }

    public void UpdateTopologyCell(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
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
        LoadGfx();
        LoadTopology();
        LoadFight();
        LoadLight();
        UpdateDisplay(Enums.Mode.Gfx);
    }

    public (int x, int y) PositionToCoord(Vector2 position, int z)
    {
        var posX = position.X;
        var posY = position.Y + GlobalData.CellHeight * 0.5f - z * GlobalData.ElevationStep;
        
        var x = posX / GlobalData.CellWidth + posY / GlobalData.CellHeight;
        var y = posY / GlobalData.CellHeight - posX / GlobalData.CellWidth;
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
        _mapData.Topology.GenerateFromGfx(_mapData.Gfx);
        LoadTopology();
    }

    public void UpdatePreview(ElementData elementData) => _placementPreview.SetAsset(elementData);
    
    public void ShowPlacementPreview(bool show) => _placementPreview.Visible = show;
 
    private void LoadGfx()
    {
        foreach (var child in _gfx.GetChildren())
        {
            child.QueueFree();
        }
        _gfxTiles.Clear();
        
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
    }

    private void LoadTopology()
    {
        foreach (var child in _topology.GetChildren())
        {
            child.QueueFree();
        }
        _topologyTiles.Clear();
        
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
    }

    private void LoadFight()
    {
        foreach (var child in _fight.GetChildren())
        {
            child.QueueFree();
        }
        _fightTiles.Clear();
    }

    private void LoadLight()
    {
        foreach (var child in _light.GetChildren())
        {
            child.QueueFree();
        }
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
        
        _gridMaterial.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _z = selectedTile.Z;
        var cellPathData = _mapData.Topology.GetPathData(selectedTile.X, selectedTile.Y);
        var visibilityData = _mapData.Topology.GetVisibilityData(selectedTile.X, selectedTile.Y);
        TileSelected?.Invoke(this, new TileSelectedEventArgs(selectedTile.Element, cellPathData, visibilityData));
    }
    
    private void _OnZoomUpdated(object sender, Camera.ZoomUpdatedEventArgs e)
    {
        _gridMaterial.SetShaderParameter("zoom", e.Zoom);
        _grid2Material.SetShaderParameter("zoom", e.Zoom);
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
