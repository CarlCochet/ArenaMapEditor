using Godot;
using System;

public partial class Tile : Sprite2D
{
    public TileData Data;
    public GfxData.Element Element;
    public TopologyData.CellPathData PathData;
    public TopologyData.CellVisibilityData VisibilityData;
    
	
    private const int CellWidth = 86;
    private const int CellHeight = 43;
    private const int ElevationStep = 10;
	
    private bool _isSelected;
    private Color _highlightColor = Colors.Green;
    private Color _baseColor = Colors.White;
    
    private Color _topColor = new(0.9f, 0.9f, 0.9f);
    private Color _leftColor = new(0.7f, 0.7f, 0.7f);
    private Color _rightColor = new(0.5f, 0.5f, 0.5f);
    
    private Color _topHighlightColor = new(0.3f, 0.9f, 0.3f);
    private Color _leftHighlightColor = new(0.2f, 0.7f, 0.2f);
    private Color _rightHighlightColor = new(0.1f, 0.5f, 0.1f);

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
        if (VisibilityData.CanViewThrough || VisibilityData.Height <= 0)
        {
            return;
        }
        
        var cubeHeight = VisibilityData.Height * ElevationStep;
        const float halfWidth = CellWidth * 0.5f;
        const float halfHeight = CellHeight * 0.5f;

        Vector2[] topFace = [new(0, -halfHeight), new(halfWidth, 0), new(0, halfHeight), new(-halfWidth, 0)];
        Vector2[] leftFace = [new(-halfWidth, 0), new(-halfWidth, cubeHeight), new(0, halfHeight + cubeHeight), new(0, halfHeight)];
        Vector2[] rightFace = [new(0, halfHeight), new(0, halfHeight + cubeHeight), new(halfWidth, cubeHeight), new(halfWidth, 0)];

        if (_isSelected)
        {
            DrawColoredPolygon(leftFace, _leftHighlightColor);
            DrawColoredPolygon(rightFace, _rightHighlightColor);
            DrawColoredPolygon(topFace, _topHighlightColor);
        }
        else
        {
            DrawColoredPolygon(leftFace, _leftColor);
            DrawColoredPolygon(rightFace, _rightColor);
            DrawColoredPolygon(topFace, _topColor);
        }

        DrawPolylineColors(topFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, true);
        DrawPolylineColors(leftFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
        DrawPolylineColors(rightFace.AsSpan(), [Colors.Black, Colors.Black, Colors.Black, Colors.Black], 1.0f, false);
    }

    public void SetElementData(GfxData.Element element)
    {
        // Data = GlobalData.Instance.Assets[element.CommonData.GfxId].Copy();
        if (!GlobalData.Instance.ValidAssets.TryGetValue(element.CommonData.GfxId, out var asset)) return;
        Data = asset.Copy();
        Element = element;
        Texture = Data.Texture;
        _baseColor = Colors.White;
        _baseColor = Element.Colors.Length < 3
            ? Colors.White
            : new Color(0.7f + 0.3f * Element.Colors[0], 0.7f + 0.3f * Element.Colors[1], 0.7f + 0.3f * Element.Colors[2]);
        // _baseColor = Element.Colors.Length < 3 ? Colors.White : new Color(Element.Colors[0], Element.Colors[1], Element.Colors[2]);
        SelfModulate = _baseColor;
    }

    public void SetPathData(TopologyData.CellPathData pathData)
    {
        if (!GlobalData.Instance.ValidAssets.TryGetValue(-1, out var asset)) return;
        if (!GlobalData.Instance.ValidAssets.TryGetValue(-2, out var asset2)) return;
        PathData = pathData;
        Data = PathData.CanMoveThrough ? asset : asset2;
        Texture = Data.Texture;
    }

    public void SetVisibilityData(TopologyData.CellVisibilityData visibilityData)
    {
        VisibilityData = visibilityData;
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
        SelfModulate = _baseColor;
        _isSelected = false;
        QueueRedraw();
    }

    public void Select()
    {
        SelfModulate = _highlightColor;
        _isSelected = true;
        QueueRedraw();
    }

    public bool IsValidPixel(Vector2 position)
    {
        var localPos = ToLocal(position);
        if (!GetRect().HasPoint(localPos))
            return false;
		
        var pos = localPos - Offset;
        var point = new Vector2I((int)pos.X, (int)pos.Y);
		
        return Data.GetImage().GetPixelv(point).A > 0.1f;
    }
}
