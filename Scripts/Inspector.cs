using Godot;
using System;

public partial class Inspector : Control
{
    [Export] private SpinBox _cellX;
    [Export] private SpinBox _cellY;
    [Export] private SpinBox _cellZ;
    [Export] private Label _offsetX;
    [Export] private Label _offsetY;
    [Export] private SpinBox _height;
    [Export] private Label _gfxId;
    [Export] private SpinBox _order;
    [Export] private Label _hashcode;
    [Export] private SpinBox _groupId;
    [Export] private SpinBox _layerIndex;
    [Export] private SpinBox _groupLayer;
    [Export] private Label _properties;
    [Export] private Label _sound;
    [Export] private Label _slope;
    [Export] private Label _shader;
    [Export] private Label _mask;
    [Export] private CheckBox _walkable;
    [Export] private CheckBox _occluder;
    [Export] private CheckBox _flip;
    [Export] private CheckBox _animated;
    [Export] private ColorPickerButton _color;
    [Export] private SpinBox _topoX;
    [Export] private SpinBox _topoY;
    [Export] private SpinBox _topoZ;
    [Export] private SpinBox _topoHeight;
    [Export] private SpinBox _cost;
    [Export] private CheckBox _canMoveThrough;
    [Export] private CheckBox _canViewThrough;
    [Export] private SpinBox _murFinInfo;
    [Export] private SpinBox _miscProperties;
    
    private GfxData.Element _elementData;
    private TopologyData.CellPathData _pathData;
    private TopologyData.CellVisibilityData _visibilityData;
    
    private bool _suppressSignals;

    public event EventHandler<ElementUpdatedEventArgs> ElementUpdated;
    public event EventHandler<TopologyUpdatedEventArgs> TopologyUpdated;

    public override void _Ready()
    {
        _cellX.ValueChanged += _OnXChanged;
        _cellY.ValueChanged += _OnYChanged;
        _cellZ.ValueChanged += _OnZChanged;
        _height.ValueChanged += _OnHeightChanged;
        _order.ValueChanged += _OnOrderChanged;
        _groupId.ValueChanged += _OnGroupIdChanged;
        _layerIndex.ValueChanged += _OnLayerIndexChanged;
        _groupLayer.ValueChanged += _OnGroupLayerChanged;
        _color.ColorChanged += _OnColorChanged;
        _occluder.Toggled += _OnOccluderToggled;
        _topoX.ValueChanged += _OnTopoXChanged;
        _topoY.ValueChanged += _OnTopoYChanged;
        _topoZ.ValueChanged += _OnTopoZChanged;
        _topoHeight.ValueChanged += _OnTopoHeightChanged;
        _cost.ValueChanged += _OnCostChanged;
        _canMoveThrough.Toggled += _OnCanMoveThroughToggled;
        _canViewThrough.Toggled += _OnCanViewThroughToggled;
        _murFinInfo.ValueChanged += _OnMurFinInfoChanged;
    }

    public void Update(GfxData.Element element, TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visibilityData)
    {
        _elementData = element;
        _pathData = pathData;
        _visibilityData = visibilityData;
        
        _suppressSignals = true;
        
        _cellX.Value = element.CellX;
        _cellY.Value = element.CellY;
        _cellZ.Value = element.CellZ;
        
        _offsetX.Text = element.CommonData.OriginX.ToString();
        _offsetY.Text = element.CommonData.OriginY.ToString();
        _height.Value = element.Height;
        
        _gfxId.Text = element.CommonData.GfxId.ToString();
        _order.Value = element.AltitudeOrder;
        _hashcode.Text = element.HashCode.ToString();
        _groupId.Value = element.GroupId;
        _layerIndex.Value = element.LayerIndex;
        _groupLayer.Value = element.GroupKey;
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
        
        _topoX.Value = pathData.X;
        _topoY.Value = pathData.Y;
        _topoZ.Value = pathData.Z;
        _topoHeight.Value = pathData.Height;
        _cost.Value = pathData.Cost;
        _canMoveThrough.ButtonPressed = pathData.CanMoveThrough;
        _canViewThrough.ButtonPressed = visibilityData.CanViewThrough;
        _murFinInfo.Value = pathData.MurFinInfo;
        _miscProperties.Value = pathData.MiscProperties;
        
        _suppressSignals = false;
    }

    private void _OnXChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.CellX = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnYChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.CellY = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnZChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.CellZ = (short) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }

    private void _OnHeightChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.Height = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }

    private void _OnOrderChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.AltitudeOrder = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnGroupIdChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.GroupId = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnLayerIndexChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.LayerIndex = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnGroupLayerChanged(double value)
    {
        if (_suppressSignals) return;
        _elementData.GroupKey = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnColorChanged(Color newColor)
    {
        if (_suppressSignals) return;
        _elementData.Color = newColor;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnOccluderToggled(bool toggledOn)
    {
        if (_suppressSignals) return;
        _elementData.Occluder = toggledOn;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData));
    }
    
    private void _OnTopoXChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.X = (int) value;
        _visibilityData.X = (int) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoYChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.Y = (int) value;
        _visibilityData.Y = (int) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoZChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.Z = (short) value;
        _visibilityData.Z = (short) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoHeightChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.Height = (sbyte) value;
        _visibilityData.Height = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCostChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.Cost = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCanMoveThroughToggled(bool toggledOn)
    {
        if (_suppressSignals) return;
        _pathData.CanMoveThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCanViewThroughToggled(bool toggledOn)
    {
        if (_suppressSignals) return;
        _visibilityData.CanViewThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnMurFinInfoChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.MurFinInfo = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnMiscPropertiesChanged(double value)
    {
        if (_suppressSignals) return;
        _pathData.MiscProperties = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }

    public class ElementUpdatedEventArgs(GfxData.Element element) : EventArgs
    {
        public GfxData.Element Element => element;
    }

    public class TopologyUpdatedEventArgs(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
        : EventArgs
    {
        public TopologyData.CellPathData Path => path;
        public TopologyData.CellVisibilityData Visibility => visibility;
    }
}
