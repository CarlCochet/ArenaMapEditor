using Godot;
using System;

public partial class Tile : Sprite2D
{
    public TileData Data;
    public GfxData.Element Element;
    public TopologyData.CellPathData PathData;
    public TopologyData.CellVisibilityData VisibilityData;
    
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }

    public bool Highlighted = true;
    public Enums.Mode Mode;
	
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;
	
    private bool _isSelected;
    private Color _highlightColor = Colors.Green;
    private Color _baseColor = Colors.White;
    
    private Color _topColor = new(0.9f, 0.9f, 0.9f);
    private Color _leftColor = new(0.7f, 0.7f, 0.7f);
    private Color _rightColor = new(0.5f, 0.5f, 0.5f);
    
    private Color _topObstacleColor = new(0.4f, 0.4f, 0.4f, 0.75f);
    private Color _leftObstacleColor = new(0.3f, 0.3f, 0.3f, 0.75f);
    private Color _rightObstacleColor = new(0.2f, 0.2f, 0.2f, 0.75f);
    
    private Color _topHighlightColor = new(0.3f, 1.0f, 0.3f);
    private Color _leftHighlightColor = new(0.2f, 0.8f, 0.2f);
    private Color _rightHighlightColor = new(0.1f, 0.6f, 0.1f);

    public override void _Ready()
    {
    }
	
    public override void _Draw()
    {
        if (_isSelected)
        {
            DrawRect(GetRect(), Colors.Red, false);
        }

        if (VisibilityData != null)
        {
            DrawIsometricCube();
        }
    }

    private void DrawIsometricCube()
    {
        if (VisibilityData.CanViewThrough)
            return;
        
        var cubeHeight = VisibilityData.Height * ElevationStep;
        const float halfWidth = CellWidth * 0.5f;
        const float halfHeight = CellHeight * 0.5f;

        Vector2[] topFace = [new(0, -halfHeight), new(halfWidth, 0), new(0, halfHeight), new(-halfWidth, 0), new(0, -halfHeight)];
        Vector2[] leftFace = [new(-halfWidth, 0), new(-halfWidth, cubeHeight), new(0, halfHeight + cubeHeight), new(0, halfHeight), new(-halfWidth, 0)];
        Vector2[] rightFace = [new(0, halfHeight), new(0, halfHeight + cubeHeight), new(halfWidth, cubeHeight), new(halfWidth, 0), new(0, halfHeight)];

        if (_isSelected)
        {
            DrawColoredPolygon(leftFace, _leftHighlightColor);
            DrawColoredPolygon(rightFace, _rightHighlightColor);
            DrawColoredPolygon(topFace, _topHighlightColor);
        }
        if (!_isSelected && PathData.Cost == -1)
        {
            DrawColoredPolygon(leftFace, _leftObstacleColor);
            DrawColoredPolygon(rightFace, _rightObstacleColor);
            DrawColoredPolygon(topFace, _topObstacleColor);
        }
        if (!_isSelected && PathData.Cost != -1)
        {
            DrawColoredPolygon(leftFace, _leftColor);
            DrawColoredPolygon(rightFace, _rightColor);
            DrawColoredPolygon(topFace, _topColor);
        }

        DrawPolylineColors(topFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 0.5f, true);
        DrawPolylineColors(leftFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
        DrawPolylineColors(rightFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
    }

    public void SetElementData(GfxData.Element element)
    {
        Mode = Enums.Mode.Gfx;
        // Data = GlobalData.Instance.Assets[element.CommonData.GfxId].Copy();
        if (!GlobalData.Instance.ValidAssets.TryGetValue(element.CommonData.GfxId, out var asset)) return;
        Data = asset.Copy();
        Element = element;
        Texture = Data.Texture;
        _baseColor = Element.Colors.Length < 3
            ? Colors.White
            : new Color(0.7f + 0.3f * Element.Colors[0], 0.7f + 0.3f * Element.Colors[1], 0.7f + 0.3f * Element.Colors[2]);
        // _baseColor = Element.Colors.Length < 3 ? Colors.White : new Color(Element.Colors[0], Element.Colors[1], Element.Colors[2]);
        SelfModulate = _baseColor;
        PositionToIso(element.CellX, element.CellY, element.CellZ, element.Height, element.CommonData.OriginX, element.CommonData.OriginY);
        X = element.CellX;
        Y = element.CellY;
        Z = element.CellZ;
        FlipH = element.CommonData.Flip;
    }

    public void SetTopology(TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visibilityData)
    {
        Mode = Enums.Mode.Topology;
        PathData = pathData;
        VisibilityData = visibilityData;
        PositionToIso(VisibilityData.X, VisibilityData.Y, VisibilityData.Z - VisibilityData.Height, -VisibilityData.Height, 0, 0);
        X = VisibilityData.X;
        Y = VisibilityData.Y;
        Z = VisibilityData.Z;
        QueueRedraw();
    }
	
    public void PositionToIso(int x, int y, int z, int height, int originX, int originY)
    {
        var newX = (x - y) * CellWidth * 0.5f;
        var newY = (x + y) * CellHeight * 0.5f - (z - height) * ElevationStep;
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
        if (!Highlighted)
            return false;
        
        var localPos = ToLocal(position);
        if (!GetRect().HasPoint(localPos))
            return false;
		
        var pos = localPos - Offset;
        var point = new Vector2I((int)pos.X, (int)pos.Y);
		
        return Data.GetImage().GetPixelv(point).A > 0.1f;
    }
}
