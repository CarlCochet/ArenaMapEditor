using Godot;
using System;

public partial class Overlay : Control
{
	[Export] private TextureRect _preview;
	[Export] private Label _position;
	[Export] private TextureButton _previousButton;
	[Export] private TextureButton _nextButton;
	[Export] private Button _generateButton;
	[Export] private TextureButton _upButton;
	[Export] private TextureButton _downButton;
	[Export] private TextureButton _highlightButton;
	[Export] private MarginContainer _previewContainer;
	[Export] private MarginContainer _generateContainer;
	[Export] private MarginContainer _heightContainer;

	public event EventHandler InterfaceEntered;
	public event EventHandler InterfaceExited;
	public event EventHandler<PreviewChangedEventArgs> PreviewChanged;
	public event EventHandler<HeightChangedEventArgs> HeightChanged;
	public event EventHandler<HighlightHeightToggledEventArgs> HighlightHeightToggled;
	public event EventHandler GenerateTopologyPressed;

	public override void _Ready()
	{
		_previewContainer.MouseEntered += _OnInterfaceEntered;
		_previewContainer.MouseExited += _OnInterfaceExited;
		_generateContainer.MouseEntered += _OnInterfaceEntered;
		_generateContainer.MouseExited += _OnInterfaceExited;
		_heightContainer.MouseEntered += _OnInterfaceEntered;
		_heightContainer.MouseExited += _OnInterfaceExited;
		_previousButton.Pressed += _OnPreviousPressed;
		_nextButton.Pressed += _OnNextPressed;
		_generateButton.Pressed += _OnGeneratePressed;
		_upButton.Pressed += _OnUpPressed;
		_downButton.Pressed += _OnDownPressed;
		_highlightButton.Toggled += _OnHighlightToggled;
	}

	public void UpdatePosition(int x, int y, int z)
	{
		_position.Text = $"({x}, {y}, {z})";
	}

	public void UpdatePreview()
	{
		
	}

	public void Update(GfxData.Element element)
	{
		if (GlobalData.Instance.ValidAssets.TryGetValue(element.CommonData.GfxId, out var asset))
			_preview.Texture = asset.Texture;
		_position.Text = $"({element.CellX}, {element.CellY}, {element.CellZ})";
	}

	private void _OnInterfaceEntered()
	{
		InterfaceEntered?.Invoke(this, EventArgs.Empty);
	}

	private void _OnInterfaceExited()
	{
		InterfaceExited?.Invoke(this, EventArgs.Empty);
	}

	private void _OnPreviousPressed()
	{
		PreviewChanged?.Invoke(this, new PreviewChangedEventArgs(Enums.Direction.Left));
	}

	private void _OnNextPressed()
	{
		PreviewChanged?.Invoke(this, new PreviewChangedEventArgs(Enums.Direction.Right));
	}

	private void _OnGeneratePressed()
	{
		GenerateTopologyPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnUpPressed()
	{
		HeightChanged?.Invoke(this, new HeightChangedEventArgs(Enums.Direction.Up));
	}

	private void _OnDownPressed()
	{
		HeightChanged?.Invoke(this, new HeightChangedEventArgs(Enums.Direction.Down));
	}

	private void _OnHighlightToggled(bool toggledOn)
	{
		HighlightHeightToggled?.Invoke(this, new HighlightHeightToggledEventArgs(toggledOn));
	}

	public class PreviewChangedEventArgs(Enums.Direction direction) : EventArgs
	{
		public Enums.Direction Direction => direction;
	}

	public class HeightChangedEventArgs(Enums.Direction direction) : EventArgs
	{
		public Enums.Direction Direction => direction;
	}

	public class HighlightHeightToggledEventArgs(bool toggledOn) : EventArgs
	{
		public bool ToggledOn => toggledOn;
	}
}
