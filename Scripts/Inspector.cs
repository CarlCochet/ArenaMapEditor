using Godot;
using System;

public partial class Inspector : Control
{
    [Export] private LineEdit _cellX;
    [Export] private LineEdit _cellY;
    [Export] private LineEdit _cellZ;
    [Export] private LineEdit _offsetX;
    [Export] private LineEdit _offsetY;
    [Export] private LineEdit _height;
    [Export] private LineEdit _gfxId;
    [Export] private LineEdit _order;
    [Export] private LineEdit _hashcode;
    [Export] private LineEdit _groupId;
    [Export] private LineEdit _layerIndex;
    [Export] private LineEdit _groupLayer;
    [Export] private LineEdit _properties;
    [Export] private LineEdit _sound;
    [Export] private LineEdit _slope;
    [Export] private LineEdit _shader;
    [Export] private LineEdit _mask;
    [Export] private CheckBox _walkable;
    [Export] private CheckBox _occluder;
    [Export] private CheckBox _flip;
    [Export] private CheckBox _animated;
    [Export] private ColorPickerButton _color;
    
    public override void _Ready() { }

    public void Update(GfxData.Element element)
    {
        _cellX.Text = element.CellX.ToString();
        _cellY.Text = element.CellY.ToString();
        _cellZ.Text = element.CellZ.ToString();
        
        _offsetX.Text = element.CommonData.OriginX.ToString();
        _offsetY.Text = element.CommonData.OriginY.ToString();
        _height.Text = element.Height.ToString();
        
        _gfxId.Text = element.CommonData.GfxId.ToString();
        _order.Text = element.AltitudeOrder.ToString();
        _hashcode.Text = element.HashCode.ToString();
        _groupId.Text = element.GroupId.ToString();
        _layerIndex.Text = element.LayerIndex.ToString();
        _groupLayer.Text = element.GroupKey.ToString();
        _properties.Text = element.CommonData.PropertiesFlag.ToString();
        _sound.Text = element.CommonData.GroundSoundType.ToString();
        _slope.Text = element.CommonData.Slope.ToString();
        _walkable.ButtonPressed = element.CommonData.Walkable;

        _color.Color = element.Colors.Length == 3
            ? new Color(element.Colors[0], element.Colors[1], element.Colors[2])
            : Colors.White;
        _shader.Text = element.CommonData.ShaderId.ToString();
        _mask.Text = element.CommonData.VisibilityMask.ToString();
        _occluder.ButtonPressed = element.Occluder;
        _flip.ButtonPressed = element.CommonData.Flip;
        _animated.ButtonPressed = element.CommonData.Animated;
    }

    private void _OnXSubmitted(string newText)
    {
        
    }
    
    private void _OnYSubmitted(string newText)
    {
        
    }
    
    private void _OnZSubmitted(string newText)
    {
        
    }

    private void _OnHeightSubmitted(string newText)
    {
        
    }

    private void _OnOrderSubmitted(string newText)
    {
        
    }
    
    private void _OnGroupIdSubmitted(string newText)
    {
        
    }
    
    private void _OnLayerIndexSubmitted(string newText)
    {
        
    }
    
    private void _OnGroupLayerSubmitted(string newText)
    {
        
    }
    
    private void _OnColorChanged(Color newColor)
    {
        
    }
    
    private void _OnOccluderToggled(bool toggledOn)
    {
        
    }
}
