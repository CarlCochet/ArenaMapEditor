using Godot;
using System;

public partial class Tile : Sprite2D
{
	[Export] private CollisionShape2D _collisionShape;
	
	public TileData Data;
	public GfxData.Element Element;
	
	private const int CellWidth = 86;
	private const int CellHeight = 43;
	private const int ElevationStep = 10;
	
	private ColorRect _debugDot;

	public override void _Ready()
	{
		_debugDot = new ColorRect
		{
			Size = new Vector2(4, 4),
			Color = Colors.Red
		};
		AddChild(_debugDot);
		_debugDot.Visible = false;
		_debugDot.ZIndex = 1000;
	}

	public void SetData(GfxData.Element element)
	{
		Data = GlobalData.Instance.Assets[element.CommonData.GfxId].Copy();
		Element = element;
		Texture = Data.Texture;
		// SelfModulate = _element.Colors.Length < 3 ? Colors.White : new Color(
		// 	0.7f + 0.3f * _element.Colors[0], 
		// 	0.7f + 0.3f * _element.Colors[1], 
		// 	0.7f + 0.3f * _element.Colors[2]);
		SelfModulate = new Color(Element.Colors[0], Element.Colors[1], Element.Colors[2]);
	}
	
	public void PositionToIso(int x, int y, int z, int height, int originX, int originY)
	{
		var newX = (x - y) * CellWidth * 0.5f;
		var newY = (x + y) * CellHeight * 0.5f - (z - height) * ElevationStep;
		Offset = Offset with { X = -originX, Y = -originY };
		Position = new Vector2(newX, newY);
	}

	public void ResetColor()
	{
		SelfModulate = new Color(Element.Colors[0], Element.Colors[1], Element.Colors[2]);
		_debugDot.Visible = false;
	}

	public bool IsValidPixel(Vector2 position)
	{
		var localPos = ToLocal(position);
		if (!GetRect().HasPoint(localPos))
			return false;

		var img = Texture.GetImage();
		var pos = localPos - Offset + Texture.GetSize() * 0.5f;
		
		// if (x < 0 || y < 0 || x >= img.GetWidth() || y >= img.GetHeight()) 
		// 	return false;

		return img.GetPixelv(new Vector2I((int)pos.X, (int)pos.Y)).A > 0.1f;
	}
}
