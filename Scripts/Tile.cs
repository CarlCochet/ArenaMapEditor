using Godot;
using System;

public partial class Tile : Sprite2D
{
	public TileData Data;
	public GfxData.Element Element;
	
	private const int CellWidth = 86;
	private const int CellHeight = 43;
	private const int ElevationStep = 10;
	
	private bool _isSelected;
	private Color _highlightColor = Colors.Green;
	private Color _baseColor = Colors.White;

	public override void _Ready()
	{
	}
	
	public override void _Draw()
	{
		if (_isSelected)
		{
            DrawRect(GetRect(), Colors.Red, false);
		}
	}

	public void SetData(GfxData.Element element)
	{
		Data = GlobalData.Instance.Assets[element.CommonData.GfxId].Copy();
		Element = element;
		Texture = Data.Texture;
		_baseColor = Colors.White;
		// _baseColor = Element.Colors.Length < 3 ? Colors.White : new Color(
		// 	0.7f + 0.3f * Element.Colors[0], 
		// 	0.7f + 0.3f * Element.Colors[1], 
		// 	0.7f + 0.3f * Element.Colors[2]);
		// SelfModulate = _baseColor;
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
	}

	public void Select()
	{
		SelfModulate = _highlightColor;
		_isSelected = true;
	}

	public bool IsValidPixel(Vector2 position)
	{
		var localPos = ToLocal(position);
		if (!GetRect().HasPoint(localPos))
			return false;

		var img = Texture.GetImage();
		var pos = localPos - Offset;
		var point = new Vector2I((int)pos.X, (int)pos.Y);
		
		return img.GetPixelv(point).A > 0.1f;
	}
}
