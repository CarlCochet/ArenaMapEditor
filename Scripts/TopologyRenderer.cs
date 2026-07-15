using Godot;
using System;
using System.Collections.Generic;

public partial class TopologyRenderer : Node2D
{
    private struct CellData
    {
        public int X, Y, Z, Height, LayerIndex;
        public bool CanViewThrough, IsBlocked, Highlighted, IsCenter;
        public TopologyData.CellPathData PathData;
        public TopologyData.CellVisibilityData VisibilityData;
        public Aabb Bounds;
    }

    private readonly List<CellData> _cells = [];
    private SubViewport _subViewport;
    private Sprite2D _viewportSprite;
    private Node3D _world;
    private MeshInstance3D _mesh;
    private MeshInstance3D _selectionMesh;
    private Camera3D _camera;
    private FightData _fightData;
    private int _selectedIndex = -1;
    private int _centerX = 8;
    private int _centerY = 8;
    private bool _topDown;
    private bool _highlightActive;
    private int _highlightZ;
    private float _yaw = Mathf.Pi / 4.0f;
    private float _pitch = -0.75f;
    private float _cameraSize = 30.0f;
    private Vector3 _focus;
    private readonly Dictionary<(int X, int Y), List<int>> _columns = [];
    private Aabb _topologyBounds;
    private bool _hasTopologyBounds;

    private const float CellSize = 1.0f;
    private const float VerticalScale = 0.35f;
    private const float MinimumHeight = 0.15f;

    private static readonly Color OpenColor = new(0.72f, 0.76f, 0.8f);
    private static readonly Color BlockedColor = new(0.78f, 0.25f, 0.22f);
    private static readonly Color PassThroughColor = new(0.22f, 0.58f, 0.88f);
    private static readonly Color HoleColor = new(0.25f, 0.09f, 0.34f);
    private static readonly Color CoachColor = new(0.08f, 0.34f, 0.15f);
    private static readonly Color RedTeamColor = new(0.42f, 0.08f, 0.09f);
    private static readonly Color BlueTeamColor = new(0.07f, 0.14f, 0.42f);
    private static readonly Color SelectedColor = new(0.25f, 1.0f, 0.35f);
    private static readonly Color CenterColor = new(0.2f, 0.9f, 0.95f);
    private static readonly Color BonusColor = new(1.0f, 0.65f, 0.12f);

    public override void _Ready()
    {
        _subViewport = new SubViewport
        {
            Name = "Topology3DViewport",
            Size = ToVector2I(GetViewport().GetVisibleRect().Size),
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
            TransparentBg = false,
            HandleInputLocally = false
        };
        AddChild(_subViewport);

        _world = new Node3D { Name = "TopologyWorld" };
        _subViewport.AddChild(_world);

        var environment = new WorldEnvironment
        {
            Environment = new Godot.Environment
            {
                BackgroundMode = Godot.Environment.BGMode.Color,
                BackgroundColor = new Color(0.055f, 0.065f, 0.08f),
                AmbientLightSource = Godot.Environment.AmbientSource.Color,
                AmbientLightColor = new Color(0.75f, 0.78f, 0.85f),
                AmbientLightEnergy = 0.65f
            }
        };
        _world.AddChild(environment);

        _camera = new Camera3D
        {
            Projection = Camera3D.ProjectionType.Perspective,
            Current = true,
            Near = 0.05f,
            Far = 1000.0f
        };
        _world.AddChild(_camera);

        _mesh = new MeshInstance3D
        {
            Name = "TopologyCells",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        _world.AddChild(_mesh);

        _selectionMesh = new MeshInstance3D
        {
            Name = "TopologySelection",
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off
        };
        _world.AddChild(_selectionMesh);

        _viewportSprite = new Sprite2D
        {
            Name = "Topology3D",
            Centered = false,
            Texture = _subViewport.GetTexture(),
            ZIndex = -1
        };
        AddChild(_viewportSprite);
        UpdateViewportLayout();
        UpdateCamera();
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        var size = ToVector2I(GetViewport().GetVisibleRect().Size);
        if (_subViewport.Size != size)
        {
            _subViewport.Size = size;
            RequestViewportUpdate();
        }
        UpdateViewportLayout();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!Visible)
            return;

        if (@event is InputEventMouseMotion motion &&
            Input.IsMouseButtonPressed(MouseButton.Right) && !_topDown)
        {
            _yaw -= motion.Relative.X * 0.008f;
            _pitch = Mathf.Clamp(_pitch - motion.Relative.Y * 0.008f, -1.45f, -0.15f);
            UpdateCamera();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventMouseMotion panMotion &&
                 Input.IsMouseButtonPressed(MouseButton.Middle))
        {
            var unitsPerPixel = _cameraSize / Math.Max(1.0f, _subViewport.Size.Y);
            var basis = _camera.GlobalTransform.Basis;
            _focus += (-basis.X * panMotion.Relative.X + basis.Y * panMotion.Relative.Y) * unitsPerPixel;
            UpdateCamera();
            GetViewport().SetInputAsHandled();
        }
        else if (@event is InputEventMouseButton { Pressed: true } button &&
                 button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown)
        {
            _cameraSize = Mathf.Clamp(_cameraSize * (button.ButtonIndex == MouseButton.WheelUp ? 0.9f : 1.1f), 3.0f, 180.0f);
            UpdateCamera();
            GetViewport().SetInputAsHandled();
        }
    }

    private void UpdateViewportLayout()
    {
        // Cancel the active Camera2D transform so the 3D viewport remains screen-aligned.
        GlobalTransform = GetViewport().CanvasTransform.AffineInverse();
    }

    private static Vector2I ToVector2I(Vector2 value) =>
        new(Math.Max(1, Mathf.RoundToInt(value.X)), Math.Max(1, Mathf.RoundToInt(value.Y)));

    private void RequestViewportUpdate()
    {
        if (_subViewport != null)
            _subViewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Once;
    }

    public void LoadTopology(TopologyData topology, int centerX, int centerY,
        TopologyData.CellPathData centerPath, FightData fightData)
    {
        _fightData = fightData;
        _centerX = centerX;
        _centerY = centerY;
        _cells.Clear();
        _selectedIndex = -1;

        foreach (var instance in topology.InstanceSet.Maps)
        {
            var map = instance.TopologyMap;
            for (var y = map.Y; y < map.Y + MapConstants.MapLength; y++)
            {
                for (var x = map.X; x < map.X + MapConstants.MapWidth; x++)
                {
                    foreach (var layer in topology.GetLayers(x, y))
                        AddCell(layer.PathData, layer.VisibilityData, layer.LayerIndex,
                            x == centerX && y == centerY);
                }
            }
        }

        if (_cells.Count == 0 && centerPath != null)
        {
            var visibility = new TopologyData.CellVisibilityData
            {
                X = centerPath.X,
                Y = centerPath.Y,
                Z = centerPath.Z,
                Height = centerPath.Height
            };
            AddCell(centerPath, visibility, 0, true);
        }

        RebuildSpatialIndex();
        FrameTopology();
        BuildMesh();
        BuildSelectionMesh();
    }

    private void AddCell(TopologyData.CellPathData pathData,
        TopologyData.CellVisibilityData visibilityData, int layerIndex, bool isCenter)
    {
        if (pathData == null || visibilityData == null)
            return;

        var displayZ = pathData.Z == short.MinValue ? 0 : pathData.Z;
        var bottom = (displayZ - pathData.Height) * VerticalScale;
        var height = Math.Max(pathData.Height * VerticalScale, MinimumHeight);
        _cells.Add(new CellData
        {
            X = pathData.X,
            Y = pathData.Y,
            Z = pathData.Z,
            Height = pathData.Height,
            LayerIndex = layerIndex,
            CanViewThrough = visibilityData.CanViewThrough,
            IsBlocked = pathData.Cost == -1,
            Highlighted = !_highlightActive || pathData.Z == _highlightZ,
            IsCenter = isCenter,
            PathData = pathData,
            VisibilityData = visibilityData,
            Bounds = new Aabb(
                new Vector3(pathData.X - CellSize * 0.5f, bottom, pathData.Y - CellSize * 0.5f),
                new Vector3(CellSize, height, CellSize))
        });
    }

    private void FrameTopology()
    {
        if (_cells.Count == 0)
        {
            _focus = new Vector3(_centerX, 0, _centerY);
            _cameraSize = 20;
            UpdateCamera();
            return;
        }

        var min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        var max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        foreach (var cell in _cells)
        {
            min = min.Min(cell.Bounds.Position);
            max = max.Max(cell.Bounds.End);
        }
        _focus = (min + max) * 0.5f;
        _cameraSize = Math.Max(8.0f, Math.Max(max.X - min.X, max.Z - min.Z) * 1.25f);
        UpdateCamera();
    }

    private void RebuildSpatialIndex()
    {
        _columns.Clear();
        _hasTopologyBounds = false;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            var key = (cell.X, cell.Y);
            if (!_columns.TryGetValue(key, out var column))
            {
                column = [];
                _columns.Add(key, column);
            }
            column.Add(i);

            if (!_hasTopologyBounds)
            {
                _topologyBounds = cell.Bounds;
                _hasTopologyBounds = true;
            }
            else
            {
                _topologyBounds = _topologyBounds.Merge(cell.Bounds);
            }
        }
    }

    private void UpdateCamera()
    {
        if (_camera == null)
            return;
        if (_topDown)
        {
            _camera.Projection = Camera3D.ProjectionType.Orthogonal;
            _camera.Size = _cameraSize;
            _camera.Position = _focus + Vector3.Up * 100.0f;
            _camera.LookAt(_focus, Vector3.Forward);
        }
        else
        {
            _camera.Projection = Camera3D.ProjectionType.Perspective;
            _camera.Fov = 55.0f;
            var horizontal = Mathf.Cos(_pitch);
            var direction = new Vector3(
                Mathf.Sin(_yaw) * horizontal,
                -Mathf.Sin(_pitch),
                Mathf.Cos(_yaw) * horizontal);
            var distance = _cameraSize / (2.0f * Mathf.Tan(Mathf.DegToRad(_camera.Fov) * 0.5f)) * 1.25f;
            _camera.Position = _focus + direction * distance;
            _camera.LookAt(_focus, Vector3.Up);
        }
        RequestViewportUpdate();
    }

    private void BuildMesh()
    {
        if (_mesh == null)
            return;

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        var hasGeometry = false;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (!cell.Highlighted)
                continue;
            AddBox(st, cell.Bounds, GetCellColor(cell),
                !IsSideCovered(_columns, cell, -1, 0),
                !IsSideCovered(_columns, cell, 1, 0),
                !IsSideCovered(_columns, cell, 0, -1),
                !IsSideCovered(_columns, cell, 0, 1),
                !IsTopCovered(_columns, cell, i));
            hasGeometry = true;
        }

        if (!hasGeometry)
        {
            _mesh.Mesh = null;
            RequestViewportUpdate();
            return;
        }

        st.SetMaterial(CreateTopologyMaterial());
        _mesh.Mesh = st.Commit();
        RequestViewportUpdate();
    }

    private void BuildSelectionMesh()
    {
        if (_selectionMesh == null)
            return;
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count || !_cells[_selectedIndex].Highlighted)
        {
            _selectionMesh.Mesh = null;
            RequestViewportUpdate();
            return;
        }

        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);
        AddBox(st, _cells[_selectedIndex].Bounds.Grow(0.0125f), SelectedColor,
            true, true, true, true, true);
        st.SetMaterial(CreateTopologyMaterial());
        _selectionMesh.Mesh = st.Commit();
        RequestViewportUpdate();
    }

    private static StandardMaterial3D CreateTopologyMaterial()
    {
        return new StandardMaterial3D
        {
            VertexColorUseAsAlbedo = true,
            Transparency = BaseMaterial3D.TransparencyEnum.Disabled,
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            CullMode = BaseMaterial3D.CullModeEnum.Back,
            Roughness = 0.85f
        };
    }

    private bool IsSideCovered(Dictionary<(int X, int Y), List<int>> columns,
        in CellData cell, int offsetX, int offsetY)
    {
        if (!columns.TryGetValue((cell.X + offsetX, cell.Y + offsetY), out var neighbors))
            return false;
        const float epsilon = 0.0001f;
        foreach (var index in neighbors)
        {
            var neighbor = _cells[index];
            if (neighbor.Highlighted && neighbor.Bounds.Position.Y <= cell.Bounds.Position.Y + epsilon &&
                neighbor.Bounds.End.Y >= cell.Bounds.End.Y - epsilon)
                return true;
        }
        return false;
    }

    private bool IsTopCovered(Dictionary<(int X, int Y), List<int>> columns,
        in CellData cell, int cellIndex)
    {
        const float epsilon = 0.0001f;
        foreach (var index in columns[(cell.X, cell.Y)])
        {
            if (index == cellIndex)
                continue;
            var other = _cells[index];
            if (other.Highlighted && Mathf.Abs(other.Bounds.Position.Y - cell.Bounds.End.Y) <= epsilon)
                return true;
        }
        return false;
    }

    private Color GetCellColor(in CellData cell)
    {
        if (cell.PathData.Z == short.MinValue)
            return HoleColor;
        if (_fightData != null)
        {
            var (placement, bonus) = _fightData.GetData(cell.X, cell.Y, cell.Z);
            if (placement == 0)
                return RedTeamColor;
            if (placement == 1)
                return BlueTeamColor;
            if (placement == 2)
                return CoachColor;
            if (bonus != -1)
                return BonusColor;
        }
        if (cell.IsCenter)
            return CenterColor;
        if (cell.CanViewThrough)
            return PassThroughColor;
        return cell.IsBlocked ? BlockedColor : OpenColor;
    }

    private static void AddBox(SurfaceTool st, Aabb box, Color color,
        bool left, bool right, bool back, bool front, bool top)
    {
        var p = box.Position;
        var e = box.End;
        var vertices = new[]
        {
            new Vector3(p.X,p.Y,p.Z), new Vector3(e.X,p.Y,p.Z), new Vector3(e.X,e.Y,p.Z), new Vector3(p.X,e.Y,p.Z),
            new Vector3(p.X,p.Y,e.Z), new Vector3(e.X,p.Y,e.Z), new Vector3(e.X,e.Y,e.Z), new Vector3(p.X,e.Y,e.Z)
        };
        if (back)
            AddFace(st, vertices, 0, 3, 2, 1, Vector3.Back, Shade(color, 0.72f));
        if (front)
            AddFace(st, vertices, 4, 5, 6, 7, Vector3.Forward, Shade(color, 0.9f));
        if (left)
            AddFace(st, vertices, 0, 4, 7, 3, Vector3.Left, Shade(color, 0.8f));
        if (right)
            AddFace(st, vertices, 1, 2, 6, 5, Vector3.Right, Shade(color, 0.62f));
        if (top)
            AddFace(st, vertices, 3, 7, 6, 2, Vector3.Up, color);
    }

    private static Color Shade(Color color, float brightness)
    {
        return new Color(color.R * brightness, color.G * brightness, color.B * brightness, color.A);
    }

    private static void AddFace(SurfaceTool st, Vector3[] vertices, int a, int b, int c, int d,
        Vector3 normal, Color color)
    {
        AddVertex(st, vertices[a], normal, color);
        AddVertex(st, vertices[c], normal, color);
        AddVertex(st, vertices[b], normal, color);
        AddVertex(st, vertices[a], normal, color);
        AddVertex(st, vertices[d], normal, color);
        AddVertex(st, vertices[c], normal, color);
    }

    private static void AddVertex(SurfaceTool st, Vector3 vertex, Vector3 normal, Color color)
    {
        st.SetColor(color);
        st.SetNormal(normal);
        st.AddVertex(vertex);
    }

    public int? GetCellIndexAt(Vector2 globalPosition)
    {
        if (_camera == null || !_hasTopologyBounds)
            return null;
        var screenPosition = GetViewport().CanvasTransform * globalPosition;
        var origin = _camera.ProjectRayOrigin(screenPosition);
        var direction = _camera.ProjectRayNormal(screenPosition);
        if (!RayIntersectsAabb(origin, direction, _topologyBounds, out var entry, out var exit))
            return null;

        const float epsilon = 0.0001f;
        var point = origin + direction * (entry + epsilon);
        var x = Mathf.FloorToInt(point.X + 0.5f);
        var y = Mathf.FloorToInt(point.Z + 0.5f);
        var stepX = Math.Sign(direction.X);
        var stepY = Math.Sign(direction.Z);
        var nextX = stepX == 0
            ? float.MaxValue
            : (x + (stepX > 0 ? 0.5f : -0.5f) - origin.X) / direction.X;
        var nextY = stepY == 0
            ? float.MaxValue
            : (y + (stepY > 0 ? 0.5f : -0.5f) - origin.Z) / direction.Z;
        var deltaX = stepX == 0 ? float.MaxValue : 1.0f / Mathf.Abs(direction.X);
        var deltaY = stepY == 0 ? float.MaxValue : 1.0f / Mathf.Abs(direction.Z);
        var nearest = float.MaxValue;
        int? result = null;
        var maxSteps = Mathf.CeilToInt(_topologyBounds.Size.X + _topologyBounds.Size.Z) + 4;
        if (maxSteps > _cells.Count)
            return FindNearestCell(origin, direction);

        var onXBoundary = stepX == 0 &&
                          Mathf.Abs(point.X + 0.5f - Mathf.Round(point.X + 0.5f)) <= epsilon;
        var onYBoundary = stepY == 0 &&
                          Mathf.Abs(point.Z + 0.5f - Mathf.Round(point.Z + 0.5f)) <= epsilon;

        void TestColumn(int columnX, int columnY)
        {
            if (!_columns.TryGetValue((columnX, columnY), out var column))
                return;
            foreach (var index in column)
            {
                if (!_cells[index].Highlighted ||
                    !RayIntersectsAabb(origin, direction, _cells[index].Bounds, out var distance) ||
                    distance >= nearest)
                    continue;
                nearest = distance;
                result = index;
            }
        }

        for (var step = 0; step < maxSteps; step++)
        {
            TestColumn(x, y);
            if (onXBoundary)
                TestColumn(x - 1, y);
            if (onYBoundary)
                TestColumn(x, y - 1);
            if (onXBoundary && onYBoundary)
                TestColumn(x - 1, y - 1);

            var next = Math.Min(nextX, nextY);
            if (nearest <= next || next > exit)
                break;
            var advanceX = nextX <= nextY;
            var advanceY = nextY <= nextX;
            if (advanceX)
            {
                x += stepX;
                nextX += deltaX;
            }
            if (advanceY)
            {
                y += stepY;
                nextY += deltaY;
            }
        }
        return result;
    }

    private int? FindNearestCell(Vector3 origin, Vector3 direction)
    {
        var nearest = float.MaxValue;
        int? result = null;
        for (var i = 0; i < _cells.Count; i++)
        {
            if (!_cells[i].Highlighted ||
                !RayIntersectsAabb(origin, direction, _cells[i].Bounds, out var distance) ||
                distance >= nearest)
                continue;
            nearest = distance;
            result = i;
        }
        return result;
    }

    private static bool RayIntersectsAabb(Vector3 origin, Vector3 direction, Aabb box, out float distance)
    {
        var intersects = RayIntersectsAabb(origin, direction, box, out distance, out _);
        return intersects;
    }

    private static bool RayIntersectsAabb(Vector3 origin, Vector3 direction, Aabb box,
        out float entry, out float exit)
    {
        var min = box.Position;
        var max = box.End;
        var tMin = 0.0f;
        var tMax = float.MaxValue;
        for (var axis = 0; axis < 3; axis++)
        {
            var o = origin[axis];
            var d = direction[axis];
            if (Mathf.Abs(d) < 0.00001f)
            {
                if (o < min[axis] || o > max[axis])
                {
                    entry = 0;
                    exit = 0;
                    return false;
                }
                continue;
            }
            var inverse = 1.0f / d;
            var t1 = (min[axis] - o) * inverse;
            var t2 = (max[axis] - o) * inverse;
            if (t1 > t2)
                (t1, t2) = (t2, t1);
            tMin = Math.Max(tMin, t1);
            tMax = Math.Min(tMax, t2);
            if (tMin > tMax)
            {
                entry = 0;
                exit = 0;
                return false;
            }
        }
        entry = tMin;
        exit = tMax;
        return true;
    }

    public void SelectCell(int index)
    {
        _selectedIndex = index;
        BuildSelectionMesh();
    }

    public void UnselectCell() => SelectCell(-1);

    public Vector2 GetSelectedCellGlobalPosition()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count)
            return Vector2.Zero;
        var center = _cells[_selectedIndex].Bounds.GetCenter();
        var screen = _camera.UnprojectPosition(center);
        return GetViewport().CanvasTransform.AffineInverse() * screen;
    }

    public (TopologyData.CellPathData Path, TopologyData.CellVisibilityData Visibility, int LayerIndex)? GetSelectedCellData()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count)
            return null;
        var cell = _cells[_selectedIndex];
        return (cell.PathData, cell.VisibilityData, cell.LayerIndex);
    }

    public void UpdateCell(TopologyData.CellPathData pathData,
        TopologyData.CellVisibilityData visibilityData, int layerIndex = 0)
    {
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell.X != pathData.X || cell.Y != pathData.Y || cell.LayerIndex != layerIndex)
                continue;
            var selected = _selectedIndex == i;
            _cells.RemoveAt(i);
            if (_selectedIndex > i)
                _selectedIndex--;
            var oldCount = _cells.Count;
            AddCell(pathData, visibilityData, layerIndex, pathData.X == _centerX && pathData.Y == _centerY);
            if (_cells.Count > oldCount)
            {
                if (selected)
                    _selectedIndex = _cells.Count - 1;
            }
            else if (selected)
            {
                _selectedIndex = -1;
            }
            RebuildSpatialIndex();
            BuildMesh();
            BuildSelectionMesh();
            return;
        }
        AddCell(pathData, visibilityData, layerIndex, false);
        RebuildSpatialIndex();
        BuildMesh();
        BuildSelectionMesh();
    }

    public void Set2D(bool is2D)
    {
        _topDown = is2D;
        UpdateCamera();
    }

    public void RebuildIfNeeded() => BuildMesh();

    public void SetCenter(int x, int y)
    {
        _centerX = x;
        _centerY = y;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            cell.IsCenter = cell.X == x && cell.Y == y;
            _cells[i] = cell;
        }
        BuildMesh();
        BuildSelectionMesh();
    }

    public void SetHeightHighlight(int z)
    {
        _highlightActive = true;
        _highlightZ = z;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            cell.Highlighted = cell.Z == z;
            _cells[i] = cell;
        }
        BuildMesh();
        BuildSelectionMesh();
    }

    public void ClearHeightHighlight()
    {
        _highlightActive = false;
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            cell.Highlighted = true;
            _cells[i] = cell;
        }
        BuildMesh();
        BuildSelectionMesh();
    }

    public void SetFightData(FightData fightData)
    {
        _fightData = fightData;
        BuildMesh();
    }

    public int? HasCellAt(int x, int y)
    {
        return _columns.TryGetValue((x, y), out var column) && column.Count > 0
            ? column[0]
            : null;
    }
}
