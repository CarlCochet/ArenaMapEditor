using Godot;
using System;

public partial class Gizmo : Control
{
    // [Export] private TextureButton _xButton;
    // [Export] private TextureButton _yButton;
    // [Export] private TextureButton _zButton;
    //
    // private Image _xImage;
    // private Image _yImage;
    // private Image _zImage;
    //
    // private bool _isDragging;
    //
    // public override void _Ready()
    // {
    //     _xImage = _xButton.TextureNormal.GetImage();
    //     _yImage = _yButton.TextureNormal.GetImage();
    //     _zImage = _zButton.TextureNormal.GetImage();
    // }
    //
    // public override void _Process(double delta)
    // {
    //     _isDragging = Input.IsMouseButtonPressed(MouseButton.Left);
    //     
    // }
    //
    // public override void _Input(InputEvent @event)
    // {
    //     if (@event is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } eventMouseButton)
    //     {
    //         if (eventMouseButton.ButtonIndex != MouseButton.Left)
    //             return;
    //
    //         if (eventMouseButton.IsPressed())
    //         {
    //             GD.Print("Pressed");
    //         }
    //
    //         if (eventMouseButton.IsReleased())
    //         {
    //             GD.Print("Released");
    //         }
    //     }
    // }
    //
    // private void _OnXPressed()
    // {
    //     GD.Print("X pressed");
    // }
    //
    // private void _OnZPressed()
    // {
    //     GD.Print("Z pressed");
    // }
    //
    // private void _OnYDown()
    // {
    //     GD.Print("Y down");
    // }
    //
    // private void _OnYUp()
    // {
    //     GD.Print("Y up");
    // }
    //
    // public class AxisPressedEventArgs(Enums.Direction direction) : EventArgs
    // {
    //     public Enums.Direction Direction => direction;
    // }
}

