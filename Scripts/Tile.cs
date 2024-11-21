using Godot;
using System;

public partial class Tile : Sprite2D
{
	private TileData _data;
	
	public override void _Ready() { }

	public void SetData(TileData data)
	{
		_data = data.Copy();
		Texture = _data.Texture;
	}
}
