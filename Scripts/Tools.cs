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

	private void _OnSelectToggled(bool toggledOn)
	{
		_selectButton.SetPressed(true);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}

	private void _OnPencilToggled(bool toggledOn)
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(true);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnBrushToggled(bool toggledOn)
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(true);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnLineToggled(bool toggledOn)
	{
		_selectButton.SetPressed(false);
		_pencilButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(true);
		_areaButton.SetPressed(false);
	}
	
	private void _OnAreaToggled(bool toggledOn)
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
