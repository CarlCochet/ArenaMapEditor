using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public record ReversibleAction(Action Do, Action Undo);
    
    public int CurrentHeight { get; set; }
    public Tile SelectedTile;
    public Tile Center { get; set; }
    
    [Export] public Camera CustomCamera;
    
    [Export] private Node2D _gfx;
    [Export] private Node2D _topology;
    [Export] private Node2D _light;
    [Export] private PackedScene _tileScene;
    [Export] private Sprite2D _grid;
    [Export] private Sprite2D _grid2;
    [Export] private PlacementPreview _placementPreview;

    private readonly Stack<ReversibleAction> _undos = [];
    private readonly Stack<ReversibleAction> _redos = [];
    
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
    
    public event EventHandler<GfxTileSelectedEventArgs> GfxTileSelected;
    public event EventHandler<TopologyTileSelectedEventArgs> TopologyTileSelected;

    public override void _Ready()
    {
        _gridMaterial = (ShaderMaterial)_grid.Material;
        _grid2Material = (ShaderMaterial)_grid2.Material;
        CustomCamera.ZoomUpdated += _OnZoomUpdated;
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("delete"))
        {
            if (SelectedTile?.Element == null)
                return;
            RegisterRemoveElement(SelectedTile.Element);
        }
        if (Input.IsActionJustPressed("undo"))
            Undo(null, EventArgs.Empty);
        if (Input.IsActionJustPressed("redo"))
            Redo(null, EventArgs.Empty);
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
                    PaintTile();
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
                    PaintTile();
                    break;
            }
        }

        if (@event is InputEventMouseButton { Pressed: false, ButtonIndex: MouseButton.Left } eventMouseButton2)
        {
            _pressed = false;
            _lastCoords = (int.MinValue, int.MinValue);
        }
    }

    public void CreateNewMap(int id)
    {
        var mapId = $"{id}";
        var newMap = new MapData(mapId);
        newMap.CreateEmpty();
        GlobalData.Instance.Maps[mapId] = newMap;
        _mapData = newMap;
    }

    public void UpdateFocus(bool hasFocus)
    {
        CustomCamera.HasFocus = hasFocus;
    }

    public void UpdateHeight(int z)
    {
        _z = z;
        
        if (!_isHighlightActivated)
            return;
        
        foreach (var child in _gfx.GetChildren())
        {
            if (child is not Tile tile)
                continue;
            
            if (tile.Z == _z)
            {
                tile.Highlight();
                continue;
            }
            tile.RemoveHighlight();
        }
    }

    public void ToggleTopologyRender(bool is2D)
    {
        foreach (var child in _topology.GetChildren())
        {
            if (child is not Tile tile)
                continue;
            
            if (is2D)
                tile.Render2D();
            else
                tile.Render3D();
        }
    }

    public void Undo(object sender, EventArgs e)
    {
        if (_undos.Count == 0)
            return;
        
        var action = _undos.Pop();
        action.Undo();
        _redos.Push(action);
    }

    public void Redo(object sender, EventArgs e)
    {
        if (_redos.Count == 0)
            return;
        
        var action = _redos.Pop();
        action.Do();
        _undos.Push(action);
    }

    public void RegisterUpdateElement(GfxData.Element oldElement, GfxData.Element newElement)
    {
        if (_mapData == null)
            return;
        
        _undos.Push(new ReversibleAction(Do: () => UpdateElement(oldElement, newElement),
            Undo: () => UpdateElement(newElement, oldElement)));
        _redos.Clear();
        UpdateElement(oldElement, newElement);
    }

    public void RegisterUpdateTopologyCell(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
    {
        if (_mapData == null)
            return;
        
        var oldPath = _mapData.Topology.GetPathData(path.X, path.Y);
        var oldVisibility = _mapData.Topology.GetVisibilityData(path.X, path.Y);
        _undos.Push(new ReversibleAction(Do: () => UpdateTopologyCell(path, visibility),
            Undo: () => UpdateTopologyCell(oldPath, oldVisibility)));
        _redos.Clear();
        UpdateTopologyCell(path, visibility);
    }

    public void RegisterUpdateFight(FightData oldFightData, FightData newFightData)
    {
        if (_mapData == null)
            return;
        _undos.Push(new ReversibleAction(Do: () => UpdateFight(oldFightData, newFightData),
            Undo: () => UpdateFight(newFightData, oldFightData)));
        _redos.Clear();
        UpdateFight(oldFightData, newFightData);
    }

    public void RegisterAddElement(GfxData.Element element)
    {
        if (_mapData == null)
            return;
        
        _undos.Push(new ReversibleAction(Do: () => AddElement(element),
            Undo: () => RemoveElement(element)));
        _redos.Clear();
        AddElement(element);
    }

    public void RegisterRemoveElement(GfxData.Element element)
    {
        if (_mapData == null)
            return;
        
        _undos.Push(new ReversibleAction(Do: () => RemoveElement(element),
            Undo: () => AddElement(element)));
        _redos.Clear();
        RemoveElement(element);
    }

    public void UpdateElement(GfxData.Element oldElement, GfxData.Element newElement)
    {
        RemoveElement(oldElement);
        var tile = AddElement(newElement);
        SelectGfxTile(tile);
    }

    public void UpdateTopologyCell(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
    {
        _mapData.Topology.Update(path, visibility);
        if (SelectedTile.Mode == Enums.Mode.Topology)
            SelectedTile.SetTopology(path, visibility);
    }

    public void UpdateFight(FightData oldFight, FightData newFight)
    {
        var offset = Math.Max(0, oldFight.MapCenter.x - newFight.MapCenter.x + oldFight.MapCenter.y - newFight.MapCenter.y);
        _mapData.Fight = newFight;
        foreach (var child in _topology.GetChildren())
        {
            if (child is not Tile tile)
                continue;
            tile.SetFightData(newFight);
        }
        
        Center.SetCenter(newFight.MapCenter.x, newFight.MapCenter.y, _mapData.Topology.GetPathData(newFight.MapCenter.x, newFight.MapCenter.y));
        
        var children = _topology.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child is not Tile tile)
                continue;
            if (tile.Name == "Center")
                continue;
            if (tile.X < Center.X || tile.Y < Center.Y)
                continue;
            
            _topology.MoveChild(Center, i+offset);
            break;
        }
    }

    public Tile AddElement(GfxData.Element element)
    {
        UnselectTile();
        UpdateTopologyFromElement(element);
        
        _mapData.Gfx.AddElement(element);
        var tile = _tileScene.Instantiate<Tile>();
        tile.SetElementData(element);
        tile.Name = element.HashCode.ToString();
        _gfx.AddChild(tile);
        
        var children = _gfx.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child is not Tile t)
                continue;
            if (t.Element.HashCode <= element.HashCode)
                continue;
            
            _gfx.MoveChild(tile, i);
            break;
        }
        
        CleanupGfxState();
        return tile;
    }

    public void UpdateTopologyFromElement(GfxData.Element element)
    {
        var pathData = _mapData.Topology.GetPathData(element.CellX, element.CellY);
        if (pathData is { Z: > short.MinValue })
            return;

        _mapData.Topology.AddFromElement(element);
        var topologyTile = _tileScene.Instantiate<Tile>();
        pathData = _mapData.Topology.GetPathData(element.CellX, element.CellY);
        var visibilityData = _mapData.Topology.GetVisibilityData(element.CellX, element.CellY);
        topologyTile.SetTopology(pathData, visibilityData);
        topologyTile.SetFightData(_mapData.Fight);
        _topology.AddChild(topologyTile);
        
        var children = _topology.GetChildren();
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            if (child is not Tile t)
                continue;
            if (t.GetHash() <= pathData.GetHash())
                continue;
            _topology.MoveChild(topologyTile, i);
            break;
        }
    }

    public void RemoveElement(GfxData.Element element)
    {
        UnselectTile();
        _gfx.GetNodeOrNull<Tile>(element.HashCode.ToString())?.QueueFree();
        _mapData.Gfx.RemoveElement(element);
        if (!_mapData.Gfx.HasElementAt(element.CellX, element.CellY))
        {
            var (path, visibility) = _mapData.Topology.ResetTile(element.CellX, element.CellY);
            _topology.GetNodeOrNull<Tile>($"{element.CellX}_{element.CellY}")?.SetTopology(path, visibility);
        }
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
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    public void Load(MapData mapData)
    {
        _redos.Clear();
        _undos.Clear();
        
        _mapData = mapData;
        LoadGfx();
        LoadTopology();
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
            foreach (var child in _gfx.GetChildren())
            {
                if (child is not Tile tile)
                    continue;
                tile.Highlight();
            }
            return;
        }

        foreach (var child in _gfx.GetChildren())
        {
            if (child is not Tile tile)
                continue;
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

    public void ShowPlacementPreview(Tools.ToolSelectedEventArgs e)
    {
        _placementPreview.Visible = e.Tool != Enums.Tool.Select && e.Tool != Enums.Tool.Erase;
    }
 
    private void LoadGfx()
    {
        UnselectTile();
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
        }
    }

    private void LoadTopology()
    {
        UnselectTile();
        foreach (var child in _topology.GetChildren())
        {
            child.QueueFree();
        }
        
        var topology = _mapData.Topology;
        var centerAdded = false;
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
                tile.SetFightData(_mapData.Fight);
                _topology.AddChild(tile);

                if (x != _mapData.Fight.MapCenter.x || y != _mapData.Fight.MapCenter.y)
                    continue;
                
                Center = _tileScene.Instantiate<Tile>();
                Center.SetCenter(_mapData.Fight.MapCenter.x, _mapData.Fight.MapCenter.y, cellPathData);
                _topology.AddChild(Center);
                centerAdded = true;
            }
        }
        
        if (centerAdded)
            return;
        
        Center = _tileScene.Instantiate<Tile>();
        Center.SetCenter(_mapData.Fight.MapCenter.x, _mapData.Fight.MapCenter.y, null);
        _topology.AddChild(Center);
    }

    private void LoadLight()
    {
        UnselectTile();
        foreach (var child in _light.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SelectGfxTile(Tile selectedTile)
    {
        if (selectedTile == null)
            return;
        
        SelectedTile = selectedTile;
        SelectedTile.Select();
        
        _gridMaterial.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _z = selectedTile.Z;
        GfxTileSelected?.Invoke(this, new GfxTileSelectedEventArgs(selectedTile.Element));
    }

    private void SelectTopologyTile(Tile selectedTile)
    {
        if (selectedTile == null)
            return;
        
        SelectedTile = selectedTile;
        SelectedTile.Select();
        
        _gridMaterial.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(selectedTile.Z * GlobalData.ElevationStep));
        _z = selectedTile.Z;
        TopologyTileSelected?.Invoke(this, new TopologyTileSelectedEventArgs(selectedTile.PathData, SelectedTile.VisibilityData));
    }

    private void SelectTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;
        
        UnselectTile();
        var selectedTile = GetTopTile(position, GlobalData.Instance.IgnoreGfxIds);
        if (_mode == Enums.Mode.Gfx)
            SelectGfxTile(selectedTile);
        if (_mode == Enums.Mode.Topology)
            SelectTopologyTile(selectedTile);
    }

    private void EraseTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;

        var topTile = GetTopTile(position, []);
        if (topTile == null)
            return;
        
        RegisterRemoveElement(topTile.Element);
    }

    private Tile GetTopTile(Vector2 position, int[] ignoreIds)
    {
        Tile selectedTile = null;
        
        foreach (var child in _mode == Enums.Mode.Gfx ? _gfx.GetChildren() : _topology.GetChildren())
        {
            if (child is not Tile tile)
                continue;
            if (!tile.IsValidPixel(position))
                continue;
            if (_mode == Enums.Mode.Gfx && ignoreIds.Contains(tile.Element.CommonData.GfxId))
                continue;
            
            if (selectedTile == null)
            {
                selectedTile = tile;
                continue;
            }
            if (_mode == Enums.Mode.Gfx && selectedTile.Element.HashCode > tile.Element.HashCode)
                continue;
            if (_mode == Enums.Mode.Topology && (selectedTile.X > tile.X || selectedTile.Y > tile.Y))
                continue;
            selectedTile = tile;
        }

        return selectedTile;
    }

    private void PaintTile()
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
        RegisterAddElement(element);
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
        UnselectTile();
        _gfx.Visible = false;
        _light.Visible = false;
        _topology.Visible = false;
    }

    private void UnselectTile()
    {
        SelectedTile?.Unselect();
        SelectedTile = null;
    }
    
    public class GfxTileSelectedEventArgs(GfxData.Element element) : EventArgs
    {
        public GfxData.Element Element => element;
    }

    public class TopologyTileSelectedEventArgs(
        TopologyData.CellPathData pathData,
        TopologyData.CellVisibilityData visibilityData) : EventArgs
    {
        public TopologyData.CellPathData PathData => pathData;
        public TopologyData.CellVisibilityData VisibilityData => visibilityData;
    }
}
