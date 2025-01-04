using Godot;
using System;

public partial class Overlay : Control
{
	[Export] private TextureRect _preview;
	[Export] private Label _position;

	public event EventHandler<PreviewChangedEventArgs> PreviewChangePressed;
	public event EventHandler<OffsetChangedEventArgs> OffsetChangeUp;
	public event EventHandler<OffsetChangedEventArgs> OffsetChangeDown;
	public event EventHandler CenterPressed;
	public event EventHandler<HeightChangedEventArgs> HeightChangePressed;
	public event EventHandler HighlightHeightPressed;
	
	
	public override void _Ready() { }

	public void UpdatePosition(int x, int y, int z)
	{
		_position.Text = $"({x}, {y}, {z})";
	}

	public void UpdatePreview()
	{
		
	}

	public void Update(GfxData.Element element)
	{
		
	}

	private void _OnPreviousPressed()
	{
		PreviewChangePressed?.Invoke(this, new PreviewChangedEventArgs(Enums.Direction.Left));
	}

	private void _OnNextPressed()
	{
		PreviewChangePressed?.Invoke(this, new PreviewChangedEventArgs(Enums.Direction.Right));
	}

	private void _OnOffsetUpDown()
	{
		OffsetChangeDown?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Up));
	}

	private void _OnOffsetUpUp()
	{
		OffsetChangeUp?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Up));
	}
	
	private void _OnOffsetLeftDown()
	{
		OffsetChangeDown?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Left));
	}

	private void _OnOffsetLeftUp()
	{
		OffsetChangeUp?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Left));
	}
	
	private void _OnOffsetRightDown()
	{
		OffsetChangeDown?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Right));
	}

	private void _OnOffsetRightUp()
	{
		OffsetChangeUp?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Right));
	}
	
	private void _OnOffsetDownDown()
	{
		OffsetChangeDown?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Down));	
	}

	private void _OnOffsetDownUp()
	{
		OffsetChangeUp?.Invoke(this, new OffsetChangedEventArgs(Enums.Direction.Down));
	}

	private void _OnCenterPressed()
	{
		CenterPressed?.Invoke(this, EventArgs.Empty);
	}

	private void _OnHeightUpPressed()
	{
		HeightChangePressed?.Invoke(this, new HeightChangedEventArgs(Enums.Direction.Up));
	}

	private void _OnHeightDownPressed()
	{
		HeightChangePressed?.Invoke(this, new HeightChangedEventArgs(Enums.Direction.Down));
	}

	private void _OnHighlightHeightPressed()
	{
		HighlightHeightPressed?.Invoke(this, EventArgs.Empty);
	}

	public class PreviewChangedEventArgs(Enums.Direction direction) : EventArgs
	{
		public Enums.Direction Direction => direction;
	}

	public class OffsetChangedEventArgs(Enums.Direction direction) : EventArgs
	{
		public Enums.Direction Direction => direction;
	}

	public class HeightChangedEventArgs(Enums.Direction direction) : EventArgs
	{
		public Enums.Direction Direction => direction;
	}
}
