using Godot;
using System;
using System.Collections.Generic;

public partial class TopologyRenderer : Node2D
{
    private struct CellData
    {
        public int X, Y, Z, Height;
        public bool CanViewThrough, IsBlocked, IsSelected, Highlighted, IsCenter;
        public TopologyData.CellPathData PathData;
        public TopologyData.CellVisibilityData VisibilityData;
        public Vector2 IsoPos;
    }

    private readonly List<CellData> _cells = [];
    private bool _is2D;
    private int _selectedIndex = -1;
    private int _highlightZ;
    private bool _highlightActive;
    private FightData _fightData;
    private bool _needsMeshRebuild;
    private int _centerX = 8;
    private int _centerY = 8;

    private MeshInstance2D _mesh;
    private Sprite2D _centerMarker;

    private const float HalfWidth = GlobalData.CellWidth * 0.5f;
    private const float HalfHeight = GlobalData.CellHeight * 0.5f;
    private const float OutlineWidth = 0.5f;

    private static readonly Color TopColor = new(0.9f, 0.9f, 0.9f);
    private static readonly Color LeftColor = new(0.8f, 0.8f, 0.8f);
    private static readonly Color RightColor = new(0.6f, 0.6f, 0.6f);
    private static readonly Color TopObstacleColor = new(0.7f, 0.4f, 0.4f, 0.75f);
    private static readonly Color LeftObstacleColor = new(0.6f, 0.3f, 0.3f, 0.75f);
    private static readonly Color RightObstacleColor = new(0.5f, 0.2f, 0.2f, 0.75f);
    private static readonly Color TopHighlightColor = new(0.3f, 1.0f, 0.3f);
    private static readonly Color LeftHighlightColor = new(0.2f, 0.8f, 0.2f);
    private static readonly Color RightHighlightColor = new(0.1f, 0.6f, 0.1f);

    private const float DimAlpha = 0.25f;

    private static Color TopFaceColor(bool selected, bool blocked, bool highlighted)
    {
        var alpha = highlighted ? 1.0f : DimAlpha;
        if (selected) return TopHighlightColor * new Color(1, 1, 1, alpha);
        if (blocked) return TopObstacleColor * new Color(1, 1, 1, alpha);
        return TopColor * new Color(1, 1, 1, alpha);
    }

    private static (Color left, Color right) SideColors(bool selected, bool blocked, bool highlighted)
    {
        var alpha = highlighted ? 1.0f : DimAlpha;
        if (selected)
            return (LeftHighlightColor * new Color(1, 1, 1, alpha),
                    RightHighlightColor * new Color(1, 1, 1, alpha));
        if (blocked)
            return (LeftObstacleColor * new Color(1, 1, 1, alpha),
                    RightObstacleColor * new Color(1, 1, 1, alpha));
        return (LeftColor * new Color(1, 1, 1, alpha),
                RightColor * new Color(1, 1, 1, alpha));
    }

    public override void _Ready()
    {
        _mesh = new MeshInstance2D
        {
            Name = "TopologyMesh",
            ZIndex = -1
        };
        AddChild(_mesh);

        _centerMarker = new Sprite2D
        {
            Name = "CenterMarker",
            Centered = false,
            ZIndex = 1
        };
        AddChild(_centerMarker);
        UpdateCenterPosition();
    }

    private void UpdateCenterPosition()
    {
        var z = 0;
        for (var i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].X == _centerX && _cells[i].Y == _centerY)
            {
                z = _is2D ? 0 : _cells[i].Z;
                break;
            }
        }
        _centerMarker.Position = IsoFromGrid(_centerX, _centerY, z, 0) + new Vector2(-43, -21);
    }

    private static Vector2 IsoFromGrid(int x, int y, int z, int height)
    {
        return new Vector2(
            (x - y) * GlobalData.CellWidth * 0.5f,
            (x + y) * GlobalData.CellHeight * 0.5f - (z - height) * GlobalData.ElevationStep
        );
    }

    public void LoadTopology(TopologyData topology, int centerX, int centerY, TopologyData.CellPathData centerPath, FightData fightData)
    {
        _fightData = fightData;
        _centerX = centerX;
        _centerY = centerY;
        _cells.Clear();
        _selectedIndex = -1;
        _needsMeshRebuild = false;
        _centerMarker.Texture = GlobalData.Instance.PlacementTextures[3];

        if (topology.InstanceSet.Maps.Count == 0)
        {
            AddCenterCell(centerX, centerY, centerPath);
            BuildMesh();
            UpdateCenterPosition();
            QueueRedraw();
            return;
        }

        var hasCenterInBounds = false;
        for (var x = topology.InstanceSet.MinX; x <= topology.InstanceSet.MinX + topology.InstanceSet.Width; x++)
        {
            for (var y = topology.InstanceSet.MinY; y <= topology.InstanceSet.MinY + topology.InstanceSet.Height; y++)
            {
                var visData = topology.GetVisibilityData(x, y);
                var pathData = topology.GetPathData(x, y);
                if (visData == null || pathData == null)
                    continue;

                var isCenter = x == centerX && y == centerY;
                AddCell(pathData, visData, isCenter);
                if (isCenter)
                    hasCenterInBounds = true;
            }
        }

        if (!hasCenterInBounds)
            AddCenterCell(centerX, centerY, centerPath);

        _cells.Sort(CellSort);
        BuildMesh();
        UpdateCenterPosition();
        QueueRedraw();
    }

    private static int CellSort(CellData a, CellData b)
    {
        var ha = (a.Y + 8192L & 0x3FFFL) << 34 | (a.X + 8192L & 0x3FFFL) << 19;
        var hb = (b.Y + 8192L & 0x3FFFL) << 34 | (b.X + 8192L & 0x3FFFL) << 19;
        return ha.CompareTo(hb);
    }

    private void AddCell(TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visData, bool isCenter)
    {
        var height = visData.Height;
        var z = visData.Z;
        var pos = _is2D
            ? IsoFromGrid(visData.X, visData.Y, 0, 0)
            : IsoFromGrid(visData.X, visData.Y, z - height, -height);

        _cells.Add(new CellData
        {
            X = visData.X,
            Y = visData.Y,
            Z = z,
            Height = height,
            CanViewThrough = visData.CanViewThrough,
            IsBlocked = pathData.Cost == -1,
            IsSelected = false,
            Highlighted = true,
            IsCenter = isCenter,
            PathData = pathData,
            VisibilityData = visData,
            IsoPos = pos,
        });
    }

    private void AddCenterCell(int x, int y, TopologyData.CellPathData pathData)
    {
        var z = pathData?.Z == short.MinValue || _is2D ? 0 : pathData?.Z ?? 0;
        var pos = IsoFromGrid(x, y, z, 0);

        _cells.Add(new CellData
        {
            X = x,
            Y = y,
            Z = z,
            Height = 0,
            CanViewThrough = false,
            IsBlocked = false,
            IsSelected = false,
            Highlighted = true,
            IsCenter = true,
            PathData = pathData,
            VisibilityData = null,
            IsoPos = pos,
        });
    }

    public void SetCenter(int x, int y)
    {
        _centerX = x;
        _centerY = y;
        UpdateCenterPosition();
    }

    public void RebuildIfNeeded()
    {
        if (!_needsMeshRebuild)
            return;
        _needsMeshRebuild = false;
        BuildMesh();
    }

    private void BuildMesh()
    {
        var st = new SurfaceTool();
        st.Begin(Mesh.PrimitiveType.Triangles);

        foreach (var cell in _cells)
        {
            var pos = cell.IsoPos;

            if (cell.IsCenter)
            {
                AddDiamondFilled(st, pos, TopColor);
                AddDiamondOutline(st, pos, Colors.Black);
                if (!_is2D)
                {
                    var cubeHeight = cell.Height == 0 ? GlobalData.ElevationStep : cell.Height * GlobalData.ElevationStep;
                    AddWall(st, pos, -1, cubeHeight, LeftColor);
                    AddWallOutline(st, pos, -1, cubeHeight, Colors.Black);
                    AddWall(st, pos, 1, cubeHeight, RightColor);
                    AddWallOutline(st, pos, 1, cubeHeight, Colors.Black);
                }
                continue;
            }

            if (cell.CanViewThrough)
                continue;

            var selected = cell.IsSelected;
            var blocked = cell.IsBlocked;
            var highlighted = cell.Highlighted;

            if (_is2D)
            {
                var color = TopFaceColor(selected, blocked, highlighted);
                AddDiamondFilled(st, pos, color);
                AddDiamondOutline(st, pos, Colors.Black);
            }
            else
            {
                var cubeHeight = cell.Height == 0 ? GlobalData.ElevationStep : cell.Height * GlobalData.ElevationStep;
                var (leftColor, rightColor) = SideColors(selected, blocked, highlighted);
                var topColor = TopFaceColor(selected, blocked, highlighted);

                AddDiamondFilled(st, pos, topColor);
                AddDiamondOutline(st, pos, Colors.Black);

                AddWall(st, pos, -1, cubeHeight, leftColor);
                AddWallOutline(st, pos, -1, cubeHeight, Colors.Black);

                AddWall(st, pos, 1, cubeHeight, rightColor);
                AddWallOutline(st, pos, 1, cubeHeight, Colors.Black);
            }
        }

        st.GenerateNormals();
        _mesh.Mesh = st.Commit();
    }

    private static void AddDiamondFilled(SurfaceTool st, Vector2 pos, Color color)
    {
        var c = new Vector3(pos.X, pos.Y, 0);
        var r = new Vector3(pos.X + HalfWidth, pos.Y, 0);
        var b = new Vector3(pos.X, pos.Y + HalfHeight, 0);
        var l = new Vector3(pos.X - HalfWidth, pos.Y, 0);
        var t = new Vector3(pos.X, pos.Y - HalfHeight, 0);

        st.SetColor(color);
        st.AddVertex(c);
        st.SetColor(color);
        st.AddVertex(r);
        st.SetColor(color);
        st.AddVertex(b);

        st.SetColor(color);
        st.AddVertex(c);
        st.SetColor(color);
        st.AddVertex(b);
        st.SetColor(color);
        st.AddVertex(l);

        st.SetColor(color);
        st.AddVertex(c);
        st.SetColor(color);
        st.AddVertex(l);
        st.SetColor(color);
        st.AddVertex(t);

        st.SetColor(color);
        st.AddVertex(c);
        st.SetColor(color);
        st.AddVertex(t);
        st.SetColor(color);
        st.AddVertex(r);
    }

    private static void AddWall(SurfaceTool st, Vector2 pos, int dir, float cubeHeight, Color color)
    {
        Vector2 a, b, c, d;
        if (dir < 0)
        {
            a = pos + new Vector2(-HalfWidth, 0);
            b = pos + new Vector2(-HalfWidth, cubeHeight);
            c = pos + new Vector2(0, HalfHeight + cubeHeight);
            d = pos + new Vector2(0, HalfHeight);
        }
        else
        {
            a = pos + new Vector2(0, HalfHeight);
            b = pos + new Vector2(0, HalfHeight + cubeHeight);
            c = pos + new Vector2(HalfWidth, cubeHeight);
            d = pos + new Vector2(HalfWidth, 0);
        }

        var va = new Vector3(a.X, a.Y, 0);
        var vb = new Vector3(b.X, b.Y, 0);
        var vc = new Vector3(c.X, c.Y, 0);
        var vd = new Vector3(d.X, d.Y, 0);

        st.SetColor(color);
        st.AddVertex(va);
        st.SetColor(color);
        st.AddVertex(vb);
        st.SetColor(color);
        st.AddVertex(vc);

        st.SetColor(color);
        st.AddVertex(va);
        st.SetColor(color);
        st.AddVertex(vc);
        st.SetColor(color);
        st.AddVertex(vd);
    }

    private static void AddEdgeQuad(SurfaceTool st, Vector2 a, Vector2 b, Color color)
    {
        var edge = b - a;
        var n = new Vector2(-edge.Y, edge.X).Normalized() * OutlineWidth;

        var v0 = new Vector3(a.X - n.X, a.Y - n.Y, 0);
        var v1 = new Vector3(a.X + n.X, a.Y + n.Y, 0);
        var v2 = new Vector3(b.X + n.X, b.Y + n.Y, 0);
        var v3 = new Vector3(b.X - n.X, b.Y - n.Y, 0);

        st.SetColor(color);
        st.AddVertex(v0);
        st.SetColor(color);
        st.AddVertex(v1);
        st.SetColor(color);
        st.AddVertex(v2);

        st.SetColor(color);
        st.AddVertex(v0);
        st.SetColor(color);
        st.AddVertex(v2);
        st.SetColor(color);
        st.AddVertex(v3);
    }

    private static void AddDiamondOutline(SurfaceTool st, Vector2 pos, Color color)
    {
        var r = pos + new Vector2(HalfWidth, 0);
        var b = pos + new Vector2(0, HalfHeight);
        var l = pos + new Vector2(-HalfWidth, 0);
        var t = pos + new Vector2(0, -HalfHeight);

        AddEdgeQuad(st, r, b, color);
        AddEdgeQuad(st, b, l, color);
        AddEdgeQuad(st, l, t, color);
        AddEdgeQuad(st, t, r, color);
    }

    private static void AddWallOutline(SurfaceTool st, Vector2 pos, int dir, float cubeHeight, Color color)
    {
        Vector2 a, b, c, d;
        if (dir < 0)
        {
            a = pos + new Vector2(-HalfWidth, 0);
            b = pos + new Vector2(-HalfWidth, cubeHeight);
            c = pos + new Vector2(0, HalfHeight + cubeHeight);
            d = pos + new Vector2(0, HalfHeight);
        }
        else
        {
            a = pos + new Vector2(0, HalfHeight);
            b = pos + new Vector2(0, HalfHeight + cubeHeight);
            c = pos + new Vector2(HalfWidth, cubeHeight);
            d = pos + new Vector2(HalfWidth, 0);
        }

        AddEdgeQuad(st, a, b, color);
        AddEdgeQuad(st, b, c, color);
        AddEdgeQuad(st, c, d, color);
        AddEdgeQuad(st, d, a, color);
    }

    public override void _Draw()
    {
        if (_needsMeshRebuild)
        {
            _needsMeshRebuild = false;
            BuildMesh();
        }

        foreach (var cell in _cells)
        {
            if (!cell.IsCenter)
                DrawPlacementBonus(cell);
        }

        DrawSelectionHighlight();
    }

    private void DrawSelectionHighlight()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count)
            return;

        var cell = _cells[_selectedIndex];
        if (cell.CanViewThrough)
            return;

        var pos = cell.IsoPos;

        if (_is2D)
        {
            var topFace = new Vector2[]
            {
                pos + new Vector2(0, -HalfHeight),
                pos + new Vector2(HalfWidth, 0),
                pos + new Vector2(0, HalfHeight),
                pos + new Vector2(-HalfWidth, 0),
            };
            DrawColoredPolygon(topFace, TopHighlightColor);
        }
        else
        {
            var cubeHeight = cell.Height == 0 ? GlobalData.ElevationStep : cell.Height * GlobalData.ElevationStep;

            var topFace = new Vector2[]
            {
                pos + new Vector2(0, -HalfHeight),
                pos + new Vector2(HalfWidth, 0),
                pos + new Vector2(0, HalfHeight),
                pos + new Vector2(-HalfWidth, 0),
            };
            var leftFace = new Vector2[]
            {
                pos + new Vector2(-HalfWidth, 0),
                pos + new Vector2(-HalfWidth, cubeHeight),
                pos + new Vector2(0, HalfHeight + cubeHeight),
                pos + new Vector2(0, HalfHeight),
            };
            var rightFace = new Vector2[]
            {
                pos + new Vector2(0, HalfHeight),
                pos + new Vector2(0, HalfHeight + cubeHeight),
                pos + new Vector2(HalfWidth, cubeHeight),
                pos + new Vector2(HalfWidth, 0),
            };

            DrawColoredPolygon(leftFace, LeftHighlightColor);
            DrawColoredPolygon(rightFace, RightHighlightColor);
            DrawColoredPolygon(topFace, TopHighlightColor);
        }
    }

    private void DrawPlacementBonus(in CellData cell)
    {
        if (_fightData == null || cell.PathData == null)
            return;

        var (placement, bonus) = _fightData.GetData(cell.X, cell.Y, cell.Z);
        if (placement != -1)
        {
            var tex = GlobalData.Instance.PlacementTextures[placement];
            if (tex != null)
                DrawTexture(tex, cell.IsoPos + new Vector2(-43, -21));
        }
        if (bonus != -1)
        {
            var tex = GlobalData.Instance.BonusTextures[bonus];
            if (tex != null)
                DrawTexture(tex, cell.IsoPos + new Vector2(-34, -17));
        }
    }

    public void Set2D(bool is2D)
    {
        if (_is2D == is2D)
            return;
        _is2D = is2D;

        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell.IsCenter)
            {
                var z = cell.PathData?.Z == short.MinValue || _is2D ? 0 : cell.PathData?.Z ?? 0;
                cell.IsoPos = IsoFromGrid(cell.X, cell.Y, z, 0);
            }
            else if (!_is2D)
            {
                cell.IsoPos = IsoFromGrid(cell.X, cell.Y, cell.Z - cell.Height, -cell.Height);
            }
            else
            {
                cell.IsoPos = IsoFromGrid(cell.X, cell.Y, 0, 0);
            }
            _cells[i] = cell;
        }

        BuildMesh();
        UpdateCenterPosition();
        QueueRedraw();
    }

    public int? GetCellIndexAt(Vector2 globalPosition)
    {
        var localPos = globalPosition - GlobalPosition;
        var halfW = (float)HalfWidth;
        var halfH = (float)HalfHeight;

        for (var i = _cells.Count - 1; i >= 0; i--)
        {
            var cell = _cells[i];
            if (!cell.Highlighted)
                continue;

            var pos = cell.IsoPos;

            var r = pos + new Vector2(halfW, 0);
            var b = pos + new Vector2(0, halfH);
            var l = pos + new Vector2(-halfW, 0);
            var t = pos + new Vector2(0, -halfH);

            if (IsPointInConvexQuad(localPos, r, b, l, t))
                return i;

            if (_is2D)
                continue;

            var cubeHeight = cell.Height == 0 ? GlobalData.ElevationStep : cell.Height * GlobalData.ElevationStep;
            var br = pos + new Vector2(0, halfH + cubeHeight);
            var bl = pos + new Vector2(0, halfH);

            var leftR = pos + new Vector2(-halfW, cubeHeight);
            if (IsPointInConvexQuad(localPos, pos + new Vector2(-halfW, 0), leftR, pos + new Vector2(0, halfH + cubeHeight), bl))
                return i;

            if (IsPointInConvexQuad(localPos, bl, br, pos + new Vector2(halfW, cubeHeight), pos + new Vector2(halfW, 0)))
                return i;
        }

        return null;
    }

    private static bool IsPointInConvexQuad(Vector2 p, Vector2 v0, Vector2 v1, Vector2 v2, Vector2 v3)
    {
        return IsLeft(v0, v1, p) && IsLeft(v1, v2, p) && IsLeft(v2, v3, p) && IsLeft(v3, v0, p);
    }

    private static bool IsLeft(Vector2 a, Vector2 b, Vector2 p)
    {
        return (b.X - a.X) * (p.Y - a.Y) - (b.Y - a.Y) * (p.X - a.X) >= 0;
    }

    public void SelectCell(int index)
    {
        if (_selectedIndex >= 0 && _selectedIndex < _cells.Count)
        {
            var old = _cells[_selectedIndex];
            old.IsSelected = false;
            _cells[_selectedIndex] = old;
        }
        _selectedIndex = index;
        if (index >= 0 && index < _cells.Count)
        {
            var sel = _cells[index];
            sel.IsSelected = true;
            _cells[index] = sel;
        }
        _needsMeshRebuild = true;
        QueueRedraw();
    }

    public void UnselectCell()
    {
        SelectCell(-1);
    }

    public Vector2 GetSelectedCellGlobalPosition()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count)
            return Vector2.Zero;
        return _cells[_selectedIndex].IsoPos + GlobalPosition;
    }

    public (TopologyData.CellPathData, TopologyData.CellVisibilityData)? GetSelectedCellData()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cells.Count)
            return null;
        var cell = _cells[_selectedIndex];
        return (cell.PathData, cell.VisibilityData);
    }

    public void UpdateCell(TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visData)
    {
        for (var i = 0; i < _cells.Count; i++)
        {
            var cell = _cells[i];
            if (cell.X != visData.X || cell.Y != visData.Y)
                continue;

            var pos = _is2D
                ? IsoFromGrid(visData.X, visData.Y, 0, 0)
                : IsoFromGrid(visData.X, visData.Y, visData.Z - visData.Height, -visData.Height);
            cell.PathData = pathData;
            cell.VisibilityData = visData;
            cell.Z = visData.Z;
            cell.Height = visData.Height;
            cell.CanViewThrough = visData.CanViewThrough;
            cell.IsBlocked = pathData.Cost == -1;
            cell.IsoPos = pos;
            _cells[i] = cell;
            _needsMeshRebuild = true;
            QueueRedraw();
            return;
        }

        AddCell(pathData, visData, false);
        _cells.Sort(CellSort);
        _needsMeshRebuild = true;
        QueueRedraw();
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
        QueueRedraw();
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
        QueueRedraw();
    }

    public void SetFightData(FightData fightData)
    {
        _fightData = fightData;
        QueueRedraw();
    }

    public int? HasCellAt(int x, int y)
    {
        for (var i = 0; i < _cells.Count; i++)
        {
            if (_cells[i].X == x && _cells[i].Y == y && !_cells[i].IsCenter)
                return i;
        }
        return null;
    }
}
