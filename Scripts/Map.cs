using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Map : Node2D
{
    [Export] private Node2D _assetContainer;
    [Export] private Camera _camera;

    public override void _Ready() { }

    public void UpdateFocus(bool hasFocus)
    {
        _camera.HasFocus = hasFocus;
    }
}