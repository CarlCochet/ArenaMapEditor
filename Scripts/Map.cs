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
    private bool _pressed;
    private (int x, int y) _lastCoords = (int.MinValue, int.MinValue);
    
    public event EventHandler<TileSelectedEventArgs> TileSelected;

    public override void _Ready()
    {
        _gridMaterial = (ShaderMaterial)_grid.Material;
        _grid2Material = (ShaderMaterial)_grid2.Material;
        CustomCamera.ZoomUpdated += _OnZoomUpdated;
    }

    public override void _Process(double delta)
    {
        
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion)
        {
            var globalPosition = GetGlobalMousePosition();
            _gridMaterial.SetShaderParameter("mouse_position", globalPosition);
            _grid2Material.SetShaderParameter("mouse_position", globalPosition);
            
            var coords = PositionToCoord(globalPosition, _z);
            _placementPreview.PositionToIso(coords.x, coords.y, _z);
            
            if (!_pressed)
                return;
            if (coords.x == _lastCoords.x && coords.y == _lastCoords.y)
                return;
            
            _lastCoords = coords;
            switch (GlobalData.Instance.SelectedTool)
            {
                case Enums.Tool.Erase:
                    EraseTile(globalPosition);
                    break;
                case Enums.Tool.Brush:
                    PaintTile(globalPosition);
                    break;
            }
        }
        if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
        {
            var globalPosition = GetGlobalMousePosition();
            _pressed = true;
            
            switch (GlobalData.Instance.SelectedTool)
            {
                case Enums.Tool.Select:
                    SelectTile(globalPosition);
                    break;
                case Enums.Tool.Erase:
                    _lastCoords = PositionToCoord(globalPosition, _z);
                    EraseTile(globalPosition);
                    break;
                case Enums.Tool.Brush:
                    _lastCoords = PositionToCoord(globalPosition, _z);
                    PaintTile(globalPosition);
                    break;
            }
        }

        if (@event is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left } eventMouseButton2)
        {
            _pressed = false;
            _lastCoords = (int.MinValue, int.MinValue);
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
        _mapData.Gfx.UpdateElement(oldElement, newElement);
        SelectedTiles.FirstOrDefault(t => t.Mode == Enums.Mode.Gfx)?.SetElementData(newElement);
        
    }

    public void UpdateTopologyCell(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
    {
        _mapData.Topology.Update(path, visibility);
        SelectedTiles.FirstOrDefault(t => t.Mode == Enums.Mode.Topology)?.SetTopology(path, visibility);
        CleanupGfxState();
    }

    public void AddElement(GfxData.Element element)
    {
        _mapData.Gfx.AddElement(element);
        
        var tile = _tileScene.Instantiate<Tile>();
        tile.SetElementData(element);
        tile.Name = element.HashCode.ToString();

        var insertIndex = _gfxTiles.FindIndex(t => t.Element.HashCode > element.HashCode);
        _gfx.AddChild(tile);
        if (insertIndex >= 0)
        {
            _gfx.MoveChild(tile, insertIndex);
            _gfxTiles.Insert(insertIndex, tile);
        }
        else
        {
            _gfxTiles.Add(tile);
        }
        CleanupGfxState();
    }

    public void RemoveElement(GfxData.Element element)
    {
        _gfxTiles.RemoveAll(t => t.Element.HashCode == element.HashCode);
        _gfx.GetNodeOrNull<Tile>(element.HashCode.ToString())?.QueueFree();
        _mapData.Gfx.RemoveElement(element);
        CleanupGfxState();
    }

    public void UpdateDisplay(Enums.Mode mode)
    {
        _mode = mode;
        _pressed = false;
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
        _gfxTiles.Clear();
        foreach (var child in _gfx.GetChildren())
        {
            child.QueueFree();
        }
        
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
        _topologyTiles.Clear();
        foreach (var child in _topology.GetChildren())
        {
            child.QueueFree();
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
    }

    private void LoadFight()
    {
        _fightTiles.Clear();
        foreach (var child in _fight.GetChildren())
        {
            child.QueueFree();
        }
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
            .FindAll(t => t.IsValidPixel(position) && !GlobalData.Instance.IgnoreGfxIds.Contains(t.Element.CommonData.GfxId))
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

    private void EraseTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;
        
        var topTile = _gfxTiles
            .FindAll(t => t.IsValidPixel(position))
            .OrderBy(t => t.Element.HashCode)
            .LastOrDefault();
        if (topTile == null)
            return;
        
        RemoveElement(topTile.Element);
    }

    private void PaintTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;
        if (_placementPreview.ElementData == null)
            return;

        var element = new GfxData.Element(_placementPreview.X, _placementPreview.Y)
        {
            CellZ = _placementPreview.Z,
            Height = _placementPreview.ElementData.VisualHeight,
            Left = (int)_placementPreview.GlobalPosition.X,
            Top = _placementPreview.ElementData.OriginY - (int)_placementPreview.GlobalPosition.Y,
            Occluder = true,
            CommonData = _placementPreview.ElementData,
            Color = Colors.White,
            Colors = [1f, 1f, 1f]
        };
        AddElement(element);
    }

    private void CleanupGfxState()
    {
        foreach (var child in _gfx.GetChildren())
        {
            if (child is not Tile tile)
                continue;
            if (!_mapData.Gfx.ElementExists(tile.Element))
            {
                tile.QueueFree();
                continue;
            }
            
            if (tile.Element.HashCode.ToString().Equals(tile.Name))
                continue;
            tile.Name = tile.Element.HashCode.ToString();
        }
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
