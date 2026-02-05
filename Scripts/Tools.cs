using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Tools : Control
{
	[Export] private TextureButton _selectButton;
	[Export] private TextureButton _brushButton;
	[Export] private TextureButton _lineButton;
	[Export] private TextureButton _areaButton;
	[Export] private TextureButton _eraserButton;
	[Export] private TextureButton _flipButton;
	[Export] private SpinBox _sizeField;
	[Export] private OptionButton _loadButton;
	[Export] private ColorPickerButton _colorPickerButton;
	[Export] private TextureButton _undoButton;
	[Export] private TextureButton _redoButton;
	[Export] private TextureButton _newMapButton;
	[Export] private TextureButton _locateButton;
	[Export] private TextureButton _exportButton;
	[Export] private OptionButton _loadMapButton;
	[Export] private PopupPanel _createMapContainer;
	[Export] private SpinBox _mapIdInput;
	[Export] private Button _validateButton;
	[Export] private Button _cancelButton;

	public event EventHandler FlipPressed;
	public event EventHandler<ColorChangedEventArgs> ColorChanged;
	public event EventHandler UndoPressed;
	public event EventHandler RedoPressed;
	public event EventHandler LocateArenaPressed;
	public event EventHandler<MapSelectedEventArgs> MapSelected;
	public event EventHandler ExportMapPressed;
	public event EventHandler<ToolSelectedEventArgs> ToolSelected;
	public event EventHandler<NewMapPressedEventArgs> NewMapPressed;

	public override void _Ready()
	{
		_selectButton.Pressed += _OnSelectPressed;
		_brushButton.Pressed += _OnBrushPressed;
		_lineButton.Pressed += _OnLinePressed;
		_areaButton.Pressed += _OnAreaPressed;
		_eraserButton.Pressed += _OnEraserPressed;
		_flipButton.Pressed += _OnFlipPressed;
		_sizeField.ValueChanged += _OnSizeChanged;
		_colorPickerButton.ColorChanged += _OnColorChanged;
		_undoButton.Pressed += _OnUndoPressed;
		_redoButton.Pressed += _OnRedoPressed;
		_newMapButton.Pressed += _OnNewPressed;
		_locateButton.Pressed += _OnLocatePressed;
		_exportButton.Pressed += _OnExportPressed;
		_loadMapButton.ItemSelected += _OnMapSelected;
		_validateButton.Pressed += _OnValidatePressed;
		_cancelButton.Pressed += _OnCancelPressed;
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("select"))
			_OnSelectPressed();
		else if (@event.IsActionPressed("paint"))
			_OnBrushPressed();
		else if (@event.IsActionPressed("eraser"))
			_OnEraserPressed();
		else if (@event.IsActionPressed("flip"))
			_OnFlipPressed();
	}

	public void SetMapOptions(List<string> mapNames)
	{
		_loadButton.Clear();
		var orderedNames = mapNames
			.OrderBy(n => int.TryParse(n, out _))
			.ThenBy(n => int.TryParse(n, out var num) ? num : int.MaxValue)
			.ToList();
		for (var i = 0; i < orderedNames.Count; i++)
		{
			_loadButton.AddItem(orderedNames[i], i);
		}
		_loadButton.Selected = -1;
	}

	public void Update(GfxData.Element element)
	{
		_colorPickerButton.Color = element.Color;
	}
	
	private void _OnSelectPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Select;
		ResetTools();
		_selectButton.SetPressed(true);
		ToolSelected?.Invoke(this, new ToolSelectedEventArgs(Enums.Tool.Select));
	}
	
	private void _OnBrushPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Brush;
		ResetTools();
		_brushButton.SetPressed(true);
		ToolSelected?.Invoke(this, new ToolSelectedEventArgs(Enums.Tool.Brush));
	}
	
	private void _OnLinePressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Line;
		ResetTools();
		_lineButton.SetPressed(true);
		ToolSelected?.Invoke(this, new ToolSelectedEventArgs(Enums.Tool.Line));
	}
	
	private void _OnAreaPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Area;
		ResetTools();
		_areaButton.SetPressed(true);
		ToolSelected?.Invoke(this, new ToolSelectedEventArgs(Enums.Tool.Area));
	}

	private void _OnEraserPressed()
	{
		GlobalData.Instance.SelectedTool = Enums.Tool.Erase;
		ResetTools();
		_eraserButton.SetPressed(true);
		ToolSelected?.Invoke(this, new ToolSelectedEventArgs(Enums.Tool.Erase));
	}

	private void ResetTools()
	{
		_selectButton.SetPressed(false);
		_brushButton.SetPressed(false);
		_lineButton.SetPressed(false);
		_areaButton.SetPressed(false);
		_eraserButton.SetPressed(false);
	}

	private void _OnFlipPressed()
	{
		FlipPressed?.Invoke(this, EventArgs.Empty);
	}
	
	private void _OnSizeChanged(double newSize)
	{
		GlobalData.Instance.BrushSize = (int)newSize;
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
		_createMapContainer.Visible = true;
	}

	private void _OnLocatePressed()
	{
		LocateArenaPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnMapSelected(long index)
	{
		var name = _loadButton.GetItemText((int)index);
		MapSelected?.Invoke(this, new MapSelectedEventArgs(name));
	}

	private void _OnExportPressed()
	{
		ExportMapPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnValidatePressed()
	{
		_createMapContainer.Visible = false;
		NewMapPressed?.Invoke(this, new NewMapPressedEventArgs((int)_mapIdInput.Value));
	}

	private void _OnCancelPressed()
	{
		_createMapContainer.Visible = false;
	}

	public class ColorChangedEventArgs(Color color) : EventArgs
	{
		public Color Color => color;
	}

	public class MapSelectedEventArgs(string mapName) : EventArgs
	{
		public string MapName => mapName;
	}

	public class ToolSelectedEventArgs(Enums.Tool tool) : EventArgs
	{
		public Enums.Tool Tool => tool;
	}

	public class NewMapPressedEventArgs(int id) : EventArgs
	{
		public int Id => id;
	}
}
