using Godot;
using System;
using System.IO;

public partial class Tile : Sprite2D
{
    [Export] private Sprite2D _placement;
    [Export] private Sprite2D _bonus;
    [Export] private Label _zLabel;
    
    public TileData Data;
    public GfxData.Element Element;
    public TopologyData.CellPathData PathData;
    public TopologyData.CellVisibilityData VisibilityData;
    
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public bool Highlighted = true;
    public Enums.Mode Mode;

    private bool _is2D;
    
    private const float HalfWidth = GlobalData.CellWidth * 0.5f;
    private const float HalfHeight = GlobalData.CellHeight * 0.5f;
    
    private bool _isSelected;
    private Color _highlightColor = Colors.Green;
    private Color _baseColor = Colors.White;
    
    private Color _topColor = new(0.9f, 0.9f, 0.9f);
    private Color _leftColor = new(0.8f, 0.8f, 0.8f);
    private Color _rightColor = new(0.6f, 0.6f, 0.6f);
    
    private Color _topObstacleColor = new(0.7f, 0.4f, 0.4f, 0.75f);
    private Color _leftObstacleColor = new(0.6f, 0.3f, 0.3f, 0.75f);
    private Color _rightObstacleColor = new(0.5f, 0.2f, 0.2f, 0.75f);
    
    private Color _topHighlightColor = new(0.3f, 1.0f, 0.3f);
    private Color _leftHighlightColor = new(0.2f, 0.8f, 0.2f);
    private Color _rightHighlightColor = new(0.1f, 0.6f, 0.1f);

    private Vector2[] _topFace;
    private Vector2[] _leftFace;
    private Vector2[] _rightFace;

    public override void _Ready() { }
	
    public override void _Draw()
    {
        if (_isSelected)
        {
            DrawRect(GetRect(), Colors.Red, false);
        }

        if (VisibilityData != null && PathData != null && !_is2D)
        {
            DrawIsometricCube();
        }

        if (VisibilityData != null && PathData != null && _is2D)
        {
            DrawPlane();
        }
    }

    private void DrawIsometricCube()
    {
        if (VisibilityData.CanViewThrough)
            return;
        
        var cubeHeight = VisibilityData.Height == 0 ? GlobalData.ElevationStep : VisibilityData.Height * GlobalData.ElevationStep;

        _topFace = [
            new Vector2(0, -HalfHeight),
            new Vector2(HalfWidth, 0),
            new Vector2(0, HalfHeight), 
            new Vector2(-HalfWidth, 0), 
            new Vector2(0, -HalfHeight)
        ];
        _leftFace = [
            new Vector2(-HalfWidth, 0), 
            new Vector2(-HalfWidth, cubeHeight), 
            new Vector2(0, HalfHeight + cubeHeight), 
            new Vector2(0, HalfHeight), 
            new Vector2(-HalfWidth, 0)
        ];
        _rightFace = [
            new Vector2(0, HalfHeight),
            new Vector2(0, HalfHeight + cubeHeight), 
            new Vector2(HalfWidth, cubeHeight), 
            new Vector2(HalfWidth, 0), 
            new Vector2(0, HalfHeight)
        ];

        if (_isSelected)
        {
            DrawColoredPolygon(_leftFace, _leftHighlightColor);
            DrawColoredPolygon(_rightFace, _rightHighlightColor);
            DrawColoredPolygon(_topFace, _topHighlightColor);
        }
        if (!_isSelected && PathData.Cost == -1)
        {
            DrawColoredPolygon(_leftFace, _leftObstacleColor);
            DrawColoredPolygon(_rightFace, _rightObstacleColor);
            DrawColoredPolygon(_topFace, _topObstacleColor);
        }
        if (!_isSelected && PathData.Cost != -1)
        {
            DrawColoredPolygon(_leftFace, _leftColor);
            DrawColoredPolygon(_rightFace, _rightColor);
            DrawColoredPolygon(_topFace, _topColor);
        }

        DrawPolylineColors(_topFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 0.5f, true);
        DrawPolylineColors(_leftFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
        DrawPolylineColors(_rightFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
    }

    private void DrawPlane()
    {
        _topFace = [
            new Vector2(0, -HalfHeight),
            new Vector2(HalfWidth, 0),
            new Vector2(0, HalfHeight), 
            new Vector2(-HalfWidth, 0),
            new Vector2(0, -HalfHeight)
        ];
        _rightFace = null;
        _leftFace = null;
        
        if (_isSelected)
            DrawColoredPolygon(_topFace, _topHighlightColor);
        if (!_isSelected && PathData.Cost == -1)
            DrawColoredPolygon(_topFace, VisibilityData.Z == short.MinValue ? Colors.Black : _topObstacleColor);
        if (!_isSelected && PathData.Cost != -1)
            DrawColoredPolygon(_topFace, _topColor);
        
        var color = VisibilityData.Z == short.MinValue ? Colors.White : Colors.Black;
        DrawPolylineColors(_topFace.AsSpan(), [color, color, color, color], 0.5f, true);
    }

    public void SetElementData(GfxData.Element element)
    {
        Mode = Enums.Mode.Gfx;
        if (!GlobalData.Instance.ValidAssets.TryGetValue(element.CommonData.GfxId, out var asset)) return;
        Data = asset.Copy();
        Element = element;
        Texture = Data.Texture;
        _baseColor = new Color(0.5f + 0.5f * Element.Color[0], 0.5f + 0.5f * Element.Color[1], 0.5f + 0.5f * Element.Color[2]);
        // _baseColor = Element.Color;
        SelfModulate = _baseColor;
        PositionToIso(element.CellX, element.CellY, element.CellZ, element.Height, element.CommonData.OriginX, element.CommonData.OriginY);
        X = element.CellX;
        Y = element.CellY;
        Z = element.CellZ;
        FlipH = element.CommonData.Flip;
        Name = element.HashCode.ToString();
    }

    public void SetTopology(TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visibilityData)
    {
        Mode = Enums.Mode.Topology;
        PathData = pathData;
        VisibilityData = visibilityData;
        X = VisibilityData.X;
        Y = VisibilityData.Y;
        Z = VisibilityData.Z;
        Name = $"{X}_{Y}";
        _zLabel.Text = Z != short.MinValue ? Z.ToString() : "";
        
        if (_is2D)
            Render2D();
        else
            Render3D();
    }

    public void Render3D()
    {
        _is2D = false;
        _zLabel.Visible = false;
        if (VisibilityData == null) 
            return;
        
        PositionToIso(VisibilityData.X, VisibilityData.Y, VisibilityData.Z - VisibilityData.Height, -VisibilityData.Height, 0, 0);
        QueueRedraw();
    }

    public void Render2D()
    {
        _is2D = true;
        _zLabel.Visible = true;
        if (VisibilityData == null) 
            return;
        
        PositionToIso(VisibilityData.X, VisibilityData.Y, 0, 0, 0, 0);
        QueueRedraw();
    }

    public void SetFightData(FightData fightData)
    {
        if (PathData == null) 
            return;
        
        var (placement, bonus) = fightData.GetData(PathData.X, PathData.Y, PathData.Z);
        _placement.Texture = placement != -1 ? GlobalData.Instance.PlacementTextures[placement] : null;
        _bonus.Texture = bonus != -1 ? GlobalData.Instance.BonusTextures[bonus] : null;
    }

    public void SetCenter(int x, int y, TopologyData.CellPathData pathData)
    {
        Mode = Enums.Mode.Topology;
        Texture = GlobalData.Instance.PlacementTextures[3];
        X = x;
        Y = y;
        Z = pathData?.Z == short.MinValue || _is2D ? 0 : pathData?.Z ?? 0;
        PositionToIso(x, y, Z, 0, 43, 21);
        Name = "Center";
    }
	
    public void PositionToIso(int x, int y, int z, int height, int originX, int originY)
    {
        var newX = (x - y) * GlobalData.CellWidth * 0.5f;
        var newY = (x + y) * GlobalData.CellHeight * 0.5f - (z - height) * GlobalData.ElevationStep;
        Offset = Offset with { X = -originX, Y = -originY };
        Position = new Vector2(newX, newY);
    }

    public void Unselect()
    {
        if (!_isSelected)
            return;
        SelfModulate = _baseColor;
        _isSelected = false;
        QueueRedraw();
    }

    public void Select()
    {
        if (_isSelected)
            return;
        SelfModulate = _highlightColor;
        _isSelected = true;
        QueueRedraw();
    }

    public void Highlight()
    {
        SelfModulate = _baseColor with { A = 1.0f };
        Highlighted = true;
    }

    public void RemoveHighlight()
    {
        SelfModulate = _baseColor with { A = 0.25f };
        Highlighted = false;
    }
 
    public bool IsValidPixel(Vector2 position)
    {
        if (!IsInsideTree() || IsQueuedForDeletion())
            return false;
        if (!Highlighted)
            return false;
        
        var localPos = ToLocal(position);
        if (Mode == Enums.Mode.Topology)
            return IsInsideTopologyRender(localPos);
        if (!GetRect().HasPoint(localPos))
            return false;
		
        var pos = localPos - Offset;
        var point = new Vector2I((int)pos.X, (int)pos.Y);
		
        return Data.GetImage().GetPixelv(point).A > 0.1f;
    }

    private bool IsInsideTopologyRender(Vector2 localPos)
    {
        if (VisibilityData == null)
            return false;

        return (_topFace != null && Geometry2D.IsPointInPolygon(localPos, _topFace)) ||
                (_leftFace != null && Geometry2D.IsPointInPolygon(localPos, _leftFace)) || 
                (_rightFace != null && Geometry2D.IsPointInPolygon(localPos, _rightFace));
    }

    public long GetHash()
    {
        return (Y + 8192L & 0x3FFFL) << 34 |
               (X + 8192L & 0x3FFFL) << 19;
    }
}
