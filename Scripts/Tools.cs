using Godot;
using System;

public partial class Tools : Control
{
	[Export] private TextureButton _selectButton;
	[Export] private TextureButton _brushButton;
	[Export] private TextureButton _lineButton;
	[Export] private TextureButton _areaButton;
	[Export] private LineEdit _sizeField;

	public event EventHandler<ColorChangedEventArgs> ColorChanged;
	public event EventHandler UndoPressed;
	public event EventHandler RedoPressed;
	public event EventHandler NewMapPressed; 
	public event EventHandler LoadMapPressed;
	public event EventHandler ExportMapPressed;
	
	public override void _Ready() { }

	private void _OnSelectPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Select;
		_selectButton.SetPressed(true);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnBrushPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Brush;
		_selectButton.SetPressed(false);
		_brushButton.SetPressed(true);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
	}
	
	private void _OnLinePressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Line;
		_selectButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(true);
		_areaButton.SetPressed(false);
	}
	
	private void _OnAreaPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Area;
		_selectButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(true);
	}

	private void _OnEraserToggled(bool toggledOn)
	{
		GlobalData.Instance.Erasing = toggledOn;
	}
	
	private void _OnSizeChanged(string newSize)
	{
		if (int.TryParse(newSize, out var size))
		{
			size = Math.Clamp(size, 1, 99);
			GlobalData.Instance.BrushSize = size;
			_sizeField.Text = size.ToString();
			return;
		}
		_sizeField.Text = GlobalData.Instance.BrushSize.ToString();
	}

	private void _OnColorChanged(Color color)
	{
		ColorChanged?.Invoke(this, new ColorChangedEventArgs(color));
	}

	private void _OnUndoPressed()
	{
		UndoPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnRedoPressed()
	{
		RedoPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnNewPressed()
	{
		NewMapPressed?.Invoke(this, EventArgs.Empty);
	}
	
	private void _OnLoadPressed()
	{
		LoadMapPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnExportPressed()
	{
		ExportMapPressed?.Invoke(this, EventArgs.Empty);
	}

	public class ColorChangedEventArgs(Color color) : EventArgs
	{
		public Color Color => color;
	}
}
