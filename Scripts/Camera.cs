using Godot;
using System;

public partial class Camera : Camera2D
{
	public bool HasFocus = true;
	
	private Vector2I _topLeft = new(-30000, -30000);
	private Vector2I _bottomRight = new(30000, 30000);
	
	private float _zoom;
	private bool _isDragging;
	private Vector2 _dragStart = Vector2.Inf;
	private Vector2 _dragEnd = Vector2.Inf;
	
	private float[] _zoomSteps = [0.05f, 0.07f, 0.10f, 0.14f, 0.20f, 0.28f, 0.40f, 0.56f, 0.80f, 1.12f, 1.6f, 2.24f, 3.2f, 4.48f];
	private int _currentZoomIndex = 6;

	public override void _Ready()
	{
		_zoom = _zoomSteps[_currentZoomIndex];
		Zoom = new Vector2(_zoom, _zoom);
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
		if (!HasFocus)
			return;
		
		if (@event is InputEventMouseButton eventMouseButton && @event.IsPressed())
		{
			if (eventMouseButton.ButtonIndex == MouseButton.WheelDown)
			{
				var oldMousePosition = GetGlobalMousePosition();
				_currentZoomIndex = Math.Max(0, _currentZoomIndex - 1);
				_zoom = _zoomSteps[_currentZoomIndex];
				Zoom = new Vector2(_zoom, _zoom);
				var newMousePosition = GetGlobalMousePosition();
				Position += oldMousePosition - newMousePosition;
			}
			if (eventMouseButton.ButtonIndex == MouseButton.WheelUp)
			{
				var oldMousePosition = GetGlobalMousePosition();
				_currentZoomIndex = Math.Min(_zoomSteps.Length - 1, _currentZoomIndex + 1);
				_zoom = _zoomSteps[_currentZoomIndex];
				Zoom = new Vector2(_zoom, _zoom);
				var newMousePosition = GetGlobalMousePosition();
				Position += oldMousePosition - newMousePosition;
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
