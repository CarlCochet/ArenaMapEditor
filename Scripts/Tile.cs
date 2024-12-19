using Godot;
using System;

public partial class Tile : Sprite2D
{
	private TileData _data;
	private GfxData.Element _element;
	
	private const int CellWidth = 86;
	private const int CellHeight = 43;
	private const int ElevationStep = 10;
	
	public override void _Ready() { }

	public void SetData(GfxData.Element element)
	{
		_data = GlobalData.Instance.Assets[element.CommonData.GfxId].Copy();
		_element = element;
		Texture = _data.Texture;
		SelfModulate = _element.Colors.Length < 3 ? Colors.White : new Color(
			0.7f + 0.3f * _element.Colors[0], 
			0.7f + 0.3f * _element.Colors[1], 
			0.7f + 0.3f * _element.Colors[2]);
	}
	
	public void PositionToIso(int x, int y, int z, int height, int originX, int originY)
	{
		var newX = (x - y) * CellWidth / 2;
		var newY = (x + y) * CellHeight / 2 - (z - height) * ElevationStep;
		Offset = Offset with { X = -originX, Y = -originY };
		Position = new Vector2(newX, newY);
	}
}
