using Godot;
using System;

public partial class Tools : Control
{
	[Export] private TextureButton _selectButton;
	[Export] private TextureButton _pencilButton;
	[Export] private TextureButton _brushButton;
	[Export] private TextureButton _lineButton;
	[Export] private TextureButton _areaButton;
	
	public override void _Ready() { }

	private void _OnSelectPressed()
	{
		_selectButton.SetPressed(true);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}

	private void _OnPencilPressed()
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(true);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnBrushPressed()
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(true);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnLinePressed()
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(true);
		_areaButton.SetPressed(false);
	}
	
	private void _OnAreaPressed()
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(true);
	}
	
	private void _OnRandomizeToggled(bool toggledOn)
	{
		
	}
	
	private void _OnSizeChanged(string newSize)
	{
		
	}
	
	private void _OnLoadPressed()
	{
		
	}
}
