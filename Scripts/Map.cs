using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    public record ReversibleAction(Action Do, Action Undo);
    
    public int CurrentHeight { get; set; }
    public object SelectedTile { get; private set; }
    public GfxData.Element SelectedElement { get; private set; }
    
    [Export] public Camera CustomCamera;
    
    private MultiMeshMapRenderer _multiMeshRenderer;
    [Export] private TopologyRenderer _topology;
    [Export] private Node2D _light;
    [Export] private EnvironmentRenderer _environment;
    [Export] private Sprite2D _grid;
    [Export] private Sprite2D _grid2;
    [Export] private PlacementPreview _placementPreview;

    private readonly Stack<ReversibleAction> _undos = [];
    private readonly Stack<ReversibleAction> _redos = [];
    
    private ShaderMaterial _gridMaterial;
    private ShaderMaterial _grid2Material;
    private MapData _mapData;
    private Enums.Mode _mode;
    private bool _isHighlightActivated;
    private int _z;
    private bool _pressed;
    private (int x, int y) _lastCoords = (int.MinValue, int.MinValue);
    
    private const double UndoInitialDelay = 0.4;
    private const double UndoRepeatInterval = 0.08;
    private bool _undoWasPressed, _redoWasPressed;
    private double _undoNextRepeatTime, _redoNextRepeatTime;
    
    public event EventHandler<GfxTileSelectedEventArgs> GfxTileSelected;
    public event EventHandler<TopologyTileSelectedEventArgs> TopologyTileSelected;
    public event EventHandler<EnvTileSelectedEventArgs> EnvTileSelected;

    private List<EnvData.Element> _selectedEnvElements;
    private readonly List<EnvMarker> _selectedEnvMarkers = [];
    private int _selectedEnvX;
    private int _selectedEnvY;

    public Vector2 SelectedTileGlobalPosition
    {
        get
        {
            if (_mode == Enums.Mode.Topology && SelectedTile != null)
                return _topology.GetSelectedCellGlobalPosition();
            if (_mode == Enums.Mode.Gfx && SelectedElement != null)
                return _multiMeshRenderer.GetSelectedTileCenter();
            return Vector2.Zero;
        }
    }

    public override void _Ready()
    {
        _gridMaterial = (ShaderMaterial)_grid.Material;
        _grid2Material = (ShaderMaterial)_grid2.Material;
        CustomCamera.ZoomUpdated += _OnZoomUpdated;

        var shader = GD.Load<Shader>("res://Shaders/MultiMeshTile.gdshader");
        var material = new ShaderMaterial();
        material.Shader = shader;
        _multiMeshRenderer = new MultiMeshMapRenderer();
        _multiMeshRenderer.Name = "MultiMeshTileRenderer";
        _multiMeshRenderer.Setup(material);
        AddChild(_multiMeshRenderer);
        MoveChild(_multiMeshRenderer, 3);

        _environment.Setup(GD.Load<CompressedTexture2D>("res://Assets/Placement/3.tgam.png"));
    }

    public override void _Process(double delta)
    {
        if (Input.IsActionJustPressed("delete"))
        {
            if (SelectedElement == null)
                return;
            RegisterRemoveElement(SelectedElement);
        }
        HandleRepeatAction("undo", ref _undoWasPressed, ref _undoNextRepeatTime, delta, Undo);
        HandleRepeatAction("redo", ref _redoWasPressed, ref _redoNextRepeatTime, delta, Redo);
    }

    private void HandleRepeatAction(string action, ref bool wasPressed, ref double nextRepeatTime, double delta, EventHandler handler)
    {
        if (Input.IsActionPressed(action))
        {
            if (!wasPressed)
            {
                wasPressed = true;
                nextRepeatTime = UndoInitialDelay;
                handler(null, EventArgs.Empty);
                return;
            }
            nextRepeatTime -= delta;
            while (nextRepeatTime <= 0)
            {
                nextRepeatTime += UndoRepeatInterval;
                handler(null, EventArgs.Empty);
            }
        }
        else
        {
            wasPressed = false;
        }
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

            if (_mode == Enums.Mode.Environment)
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

            if (_mode == Enums.Mode.Environment)
            {
                SelectTile(globalPosition);
            }
            else switch (GlobalData.Instance.SelectedTool)
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
        Load(newMap);
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
        
        if (_mode == Enums.Mode.Gfx)
            _multiMeshRenderer.SetHeightHighlight(_z);
    }

    public void ToggleTopologyRender(bool is2D)
    {
        _topology.Set2D(is2D);
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
        AddElement(newElement);
        SelectGfxElement(newElement);
    }

    public void UpdateTopologyCell(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
    {
        _mapData.Topology.Update(path, visibility);
        _topology.UpdateCell(path, visibility);
    }

    public void UpdateFight(FightData oldFight, FightData newFight)
    {
        _mapData.Fight = newFight;
        _topology.SetCenter(newFight.MapCenter.x, newFight.MapCenter.y);
        _topology.SetFightData(newFight);
    }

    public void AddElement(GfxData.Element element)
    {
        if (element == null)
            return;
        
        UnselectAll();
        UpdateTopologyFromElement(element);
        
        _mapData.Gfx.AddElement(element);
        _multiMeshRenderer.AddElement(element);
    }

    public void UpdateTopologyFromElement(GfxData.Element element)
    {
        if (element == null) 
            return;
        
        var pathData = _mapData.Topology.GetPathData(element.CellX, element.CellY);
        if (pathData is { Z: > short.MinValue })
            return;

        _mapData.Topology.AddFromElement(element);
        pathData = _mapData.Topology.GetPathData(element.CellX, element.CellY);
        var visibilityData = _mapData.Topology.GetVisibilityData(element.CellX, element.CellY);
        _topology.AddCellFromElement(pathData, visibilityData);
    }

    public void RemoveElement(GfxData.Element element)
    {
        if (element == null) 
            return;
        
        UnselectAll();
        _multiMeshRenderer.RemoveElement(element);
        _mapData.Gfx.RemoveElement(element);
        if (!_mapData.Gfx.HasElementAt(element.CellX, element.CellY))
        {
            var (path, visibility) = _mapData.Topology.ResetTile(element.CellX, element.CellY);
            _topology.UpdateCell(path, visibility);
        }
    }

    public void UpdateDisplay(Enums.Mode mode)
    {
        _mode = mode;
        _pressed = false;
        ResetDisplay();
        switch (_mode)
        {
            case Enums.Mode.Gfx:
                _multiMeshRenderer.Visible = true;
                break;
            case Enums.Mode.Topology:
                _topology.RebuildIfNeeded();
                _topology.Visible = true;
                break;
            case Enums.Mode.Light:
                _light.Visible = true;
                break;
            case Enums.Mode.Environment:
                _multiMeshRenderer.Visible = true;
                _environment.Visible = true;
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
        _environment.LoadElements(_mapData.Env);
        UpdateDisplay(Enums.Mode.Gfx);
    }

    public (int x, int y) PositionToCoord(Vector2 position, int z)
    {
        var posX = position.X;
        var posY = position.Y + GlobalData.CellHeight * 0.5f - z * GlobalData.ElevationStep;
        
        var x = posX / GlobalData.CellWidth + posY / GlobalData.CellHeight;
        var y = posY / GlobalData.CellHeight - posX / GlobalData.CellWidth;
        return ((int)Math.Floor(x), (int)Math.Floor(y));
    }

    public void ToggleHeightHighlight(bool toggledOn, int z)
    {
        _isHighlightActivated = toggledOn;
        if (_mode == Enums.Mode.Gfx)
        {
            if (!_isHighlightActivated)
                _multiMeshRenderer.ClearHeightHighlight();
            else
                _multiMeshRenderer.SetHeightHighlight(z);
        }
        else
        {
            if (!_isHighlightActivated)
                _topology.ClearHeightHighlight();
            else
                _topology.SetHeightHighlight(z);
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

    public void FlipPlacementPreview(object sender, EventArgs e)
    {
        _placementPreview.Flip();
    }
 
    private void LoadGfx()
    {
        UnselectAll();
        
        if (_multiMeshRenderer.Material is ShaderMaterial mat)
        {
            mat.SetShaderParameter("atlas_tex", GlobalData.Instance.AtlasTexture);
            mat.SetShaderParameter("total_layers", (float)GlobalData.Instance.TotalAtlasLayers);
        }

        var sortedElements = _mapData.Gfx.Partitions
            .SelectMany(p => p.Elements)
            .OrderBy(t => t.HashCode)
            .ToList();

        _multiMeshRenderer.LoadElements(sortedElements);
    }

    private void LoadTopology()
    {
        UnselectTopologyTile();

        var topology = _mapData.Topology;
        var (centerX, centerY) = _mapData.Fight != null
            ? (_mapData.Fight.MapCenter.x, _mapData.Fight.MapCenter.y)
            : (8, 8);
        var centerPath = _mapData.Topology.GetPathData(centerX, centerY);

        _topology.LoadTopology(topology, centerX, centerY, centerPath, _mapData.Fight);
    }

    private void LoadLight()
    {
        UnselectAll();
        foreach (var child in _light.GetChildren())
        {
            child.QueueFree();
        }
    }

    private void SelectGfxElement(GfxData.Element element)
    {
        if (element == null)
            return;
        
        SelectedElement = element;
        SelectedTile = null;
        _multiMeshRenderer.SelectElement(element.HashCode);
        
        _gridMaterial.SetShaderParameter("elevation", (float)(element.CellZ * GlobalData.ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(element.CellZ * GlobalData.ElevationStep));
        _z = element.CellZ;
        GfxTileSelected?.Invoke(this, new GfxTileSelectedEventArgs(element));
    }

    private void SelectTopologyTile(int cellIndex)
    {
        if (cellIndex < 0)
            return;
        
        _topology.SelectCell(cellIndex);
        SelectedTile = new object(); // non-null sentinel
        SelectedElement = null;
        
        var cellData = _topology.GetSelectedCellData();
        if (cellData == null)
            return;
        
        var (pathData, visibilityData) = cellData.Value;
        var z = visibilityData?.Z ?? 0;
        
        _gridMaterial.SetShaderParameter("elevation", (float)(z * GlobalData.ElevationStep));
        _grid2Material.SetShaderParameter("elevation", (float)(z * GlobalData.ElevationStep));
        _z = z;
        TopologyTileSelected?.Invoke(this, new TopologyTileSelectedEventArgs(pathData, visibilityData));
    }

    private void SelectTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;
        
        UnselectAll();
        
        if (_mode == Enums.Mode.Gfx)
        {
            var element = _multiMeshRenderer.GetTopElementAt(position, GlobalData.Instance.IgnoreGfxIds);
            SelectGfxElement(element);
        }
        if (_mode == Enums.Mode.Topology)
        {
            var cellIndex = _topology.GetCellIndexAt(position);
            SelectTopologyTile(cellIndex ?? -1);
        }
        if (_mode == Enums.Mode.Environment)
        {
            SelectEnvTileAtPosition(position);
        }
    }

    private void EraseTile(Vector2 position)
    {
        if (!CustomCamera.HasFocus)
            return;

        if (_mode == Enums.Mode.Gfx)
        {
            var element = _multiMeshRenderer.GetTopElementAt(position, []);
            if (element != null)
                RegisterRemoveElement(element);
        }
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
        if (_mapData.Gfx.HasElement(element.CellX, element.CellY, element.CellZ, element.CommonData.GfxId))
            return;
        RegisterAddElement(element);
    }
    
    private void _OnZoomUpdated(object sender, float zoom)
    {
        _gridMaterial.SetShaderParameter("zoom", zoom);
        _grid2Material.SetShaderParameter("zoom", zoom);
    }

    private void ResetDisplay()
    {
        UnselectAll();
        _multiMeshRenderer.Visible = false;
        _light.Visible = false;
        _topology.Visible = false;
        _environment.Visible = false;
        UnselectEnvTile();
    }

    private void UnselectAll()
    {
        UnselectGfxElement();
        UnselectTopologyTile();
        UnselectEnvTile();
    }

    private void UnselectGfxElement()
    {
        _multiMeshRenderer.DeselectAll();
        SelectedElement = null;
    }

    private void UnselectTopologyTile()
    {
        _topology.UnselectCell();
        SelectedTile = null;
    }
    
    private void SelectEnvTileAtPosition(Vector2 position)
    {
        var children = _environment.GetChildren();
        for (var i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is not EnvMarker marker || marker.Texture == null)
                continue;

            var tex = marker.Texture;
            var texSize = tex.GetSize();
            var left = marker.Position.X - texSize.X * 0.5f;
            var top = marker.Position.Y - texSize.Y * 0.5f;

            if (position.X < left || position.X > left + texSize.X ||
                position.Y < top || position.Y > top + texSize.Y)
                continue;

            var localX = (int)(position.X - left);
            var localY = (int)(position.Y - top);

            if (localX < 0 || localX >= texSize.X || localY < 0 || localY >= texSize.Y)
                continue;

            if (tex.GetImage().GetPixel(localX, localY).A <= 0.1f)
                continue;

            SelectEnvTile(marker.CellX, marker.CellY);
            return;
        }

        ResetEnvMarkerColors();
        _selectedEnvElements = [];
        EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, 0, 0, 0));
    }

    private void SelectEnvTile(int x, int y)
    {
        _selectedEnvX = x;
        _selectedEnvY = y;
        _selectedEnvElements = _mapData.Env.GetElementsAt(x, y);

        HighlightEnvMarkers(x, y);

        EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, x, y, 0));
    }

    private void UnselectEnvTile()
    {
        ResetEnvMarkerColors();
        _selectedEnvElements = null;
        _selectedEnvX = 0;
        _selectedEnvY = 0;
    }

    private void HighlightEnvMarkers(int x, int y)
    {
        ResetEnvMarkerColors();
        foreach (var child in _environment.GetChildren())
        {
            if (child is not EnvMarker marker || marker.CellX != x || marker.CellY != y)
                continue;
            marker.Modulate = Colors.Red;
            _selectedEnvMarkers.Add(marker);
        }
    }

    private void ResetEnvMarkerColors()
    {
        foreach (var marker in _selectedEnvMarkers)
            marker.Modulate = Colors.White;
        _selectedEnvMarkers.Clear();
    }

    private void AddEnvElement(int globalX, int globalY, EnvData.Element element)
    {
        _mapData.Env.AddElement(globalX, globalY, element);
    }

    private void RemoveEnvElement(int globalX, int globalY, EnvData.Element element)
    {
        _mapData.Env.RemoveElement(globalX, globalY, element);
    }

    public void RegisterUpdateEnvElement(EnvData.Element oldElement, EnvData.Element newElement, int index)
    {
        if (_mapData == null)
            return;

        _undos.Push(new ReversibleAction(
            Do: () =>
            {
                RemoveEnvElement(_selectedEnvX, _selectedEnvY, oldElement);
                AddEnvElement(_selectedEnvX, _selectedEnvY, newElement);
                RefreshEnvSelection(newElement);
            },
            Undo: () =>
            {
                RemoveEnvElement(_selectedEnvX, _selectedEnvY, newElement);
                AddEnvElement(_selectedEnvX, _selectedEnvY, oldElement);
                RefreshEnvSelection(oldElement);
            }));
        _redos.Clear();

        RemoveEnvElement(_selectedEnvX, _selectedEnvY, oldElement);
        AddEnvElement(_selectedEnvX, _selectedEnvY, newElement);
        RefreshEnvSelection(newElement);
    }

    public void RegisterAddEnvElement()
    {
        if (_mapData == null)
            return;

        var element = new EnvData.ParticleDef { X = 0, Y = 0, Z = 0 };
        AddEnvElement(_selectedEnvX, _selectedEnvY, element);

        _undos.Push(new ReversibleAction(
            Do: () =>
            {
                AddEnvElement(_selectedEnvX, _selectedEnvY, element);
                RefreshEnvSelection(element);
            },
            Undo: () =>
            {
                RemoveEnvElement(_selectedEnvX, _selectedEnvY, element);
                _environment.LoadElements(_mapData.Env);
                HighlightEnvMarkers(_selectedEnvX, _selectedEnvY);
                _selectedEnvElements = _mapData.Env.GetElementsAt(_selectedEnvX, _selectedEnvY);
                var idx = Math.Min(_selectedEnvElements.Count - 1, 0);
                EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, _selectedEnvX, _selectedEnvY, idx));
            }));
        _redos.Clear();
        RefreshEnvSelection(element);
    }

    public void RegisterRemoveEnvElement(int elementIndex)
    {
        if (_mapData == null || _selectedEnvElements == null || elementIndex >= _selectedEnvElements.Count)
            return;

        var element = _selectedEnvElements[elementIndex];
        RemoveEnvElement(_selectedEnvX, _selectedEnvY, element);

        _undos.Push(new ReversibleAction(
            Do: () =>
            {
                RemoveEnvElement(_selectedEnvX, _selectedEnvY, element);
                _environment.LoadElements(_mapData.Env);
                HighlightEnvMarkers(_selectedEnvX, _selectedEnvY);
                _selectedEnvElements = _mapData.Env.GetElementsAt(_selectedEnvX, _selectedEnvY);
                var idx = Math.Min(elementIndex, _selectedEnvElements.Count - 1);
                EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, _selectedEnvX, _selectedEnvY, idx));
            },
            Undo: () =>
            {
                AddEnvElement(_selectedEnvX, _selectedEnvY, element);
                RefreshEnvSelection(element);
            }));
        _redos.Clear();

        _environment.LoadElements(_mapData.Env);
        HighlightEnvMarkers(_selectedEnvX, _selectedEnvY);
        _selectedEnvElements = _mapData.Env.GetElementsAt(_selectedEnvX, _selectedEnvY);
        var newIndex = Math.Min(elementIndex, _selectedEnvElements.Count - 1);
        EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, _selectedEnvX, _selectedEnvY, newIndex));
    }

    private void RefreshEnvSelection(EnvData.Element element)
    {
        _environment.LoadElements(_mapData.Env);
        HighlightEnvMarkers(_selectedEnvX, _selectedEnvY);
        _selectedEnvElements = _mapData.Env.GetElementsAt(_selectedEnvX, _selectedEnvY);
        var newIndex = _selectedEnvElements.IndexOf(element);
        if (newIndex < 0)
            newIndex = Math.Min(_selectedEnvElements.Count - 1, 0);
        EnvTileSelected?.Invoke(this, new EnvTileSelectedEventArgs(_selectedEnvElements, _selectedEnvX, _selectedEnvY, newIndex));
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

    public class EnvTileSelectedEventArgs(
        List<EnvData.Element> elements,
        int x,
        int y,
        int currentIndex) : EventArgs
    {
        public List<EnvData.Element> Elements => elements;
        public int X => x;
        public int Y => y;
        public int CurrentIndex => currentIndex;
    }
}
