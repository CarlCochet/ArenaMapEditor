using Godot;
using System;

public partial class Tile : Sprite2D
{
	private TileData _data;
	private const int CellWidth = 86;
	private const int CellHeight = 43;
	private const int ElevationStep = 10;
	
	public override void _Ready() { }

	public void SetData(TileData data)
	{
		_data = data.Copy();
		Texture = _data.Texture;
	}

	public void PositionToIso(int x, int y, int z, int height, int originX, int originY)
	{
		var newX = (x - y) * CellWidth / 2;
		var newY = (x + y) * CellHeight / 2 - (z - height) * ElevationStep;
		Offset = Offset with { X = -originX, Y = -originY };
		Position = new Vector2(newX, newY);
	}
}
