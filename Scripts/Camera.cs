using Godot;
using System;

public partial class Camera : Camera2D
{
	[Export] private Vector2I _topLeft;
	[Export] private Vector2I _bottomRight;
	[Export] private float _zoomMin;
	[Export] private float _zoomMax;
	
	private float _zoom;
	private bool _isDragging;
	private Vector2 _dragStart = Vector2.Inf;
	private Vector2 _dragEnd = Vector2.Inf;

	public override void _Ready()
	{
		Zoom = new Vector2(_zoomMin, _zoomMin);
		_zoom = _zoomMin;
	}
	
	public override void _Process(double delta)
	{
		_isDragging = Input.IsMouseButtonPressed(MouseButton.Right);
		_dragStart = _dragEnd;
		_dragEnd = Vector2.Inf;
		Position = Position with { X = Math.Max(Position.X, _topLeft.X), Y = Math.Max(Position.Y, _topLeft.Y) };
		Position = Position with { X = Math.Min(Position.X, _bottomRight.X), Y = Math.Min(Position.Y, _bottomRight.Y) };
	}
	
	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				_zoom = Math.Max(_zoom * 0.75f, _zoomMin);
				Zoom = new Vector2(_zoom, _zoom);
			}
			if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				_zoom = Math.Min(_zoom * 1.5f, _zoomMax);
				Zoom = new Vector2(_zoom, _zoom);
			}
		}

		if (@event is InputEventMouseMotion eventMouseMotion)
		{
			if (!_isDragging)
				return;
			_dragEnd = eventMouseMotion.Position;
			if (_dragEnd.X > 9999999 || _dragStart.X > 9999999)
				return;
			var isMoving = _dragEnd.DistanceTo(_dragStart) > 0;
			if (isMoving)
				Position += (_dragStart - _dragEnd) / _zoom;
		}
	}
}
