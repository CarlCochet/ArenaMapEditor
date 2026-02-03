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
    [Export] private CheckBox _topo2D;
    [Export] private SpinBox _topoX;
    [Export] private SpinBox _topoY;
    [Export] private SpinBox _topoZ;
    [Export] private SpinBox _topoHeight;
    [Export] private SpinBox _cost;
    [Export] private CheckBox _canMoveThrough;
    [Export] private CheckBox _canViewThrough;
    [Export] private SpinBox _murFinInfo;
    [Export] private SpinBox _miscProperties;
    [Export] private OptionButton _placement;
    [Export] private OptionButton _bonus;
    [Export] private VBoxContainer _gfxContainer;
    [Export] private VBoxContainer _topologyContainer;
    [Export] private SpinBox _centerX;
    [Export] private SpinBox _centerY;
    
    private GfxData.Element _elementData;
    private TopologyData.CellPathData _pathData;
    private TopologyData.CellVisibilityData _visibilityData;
    private FightData _fightData;
    
    private bool _suppressSignals;

    public event EventHandler<ElementUpdatedEventArgs> ElementUpdated;
    public event EventHandler<TopologyUpdatedEventArgs> TopologyUpdated;
    public event EventHandler<FightUpdatedEventArgs> FightUpdated;
    public event EventHandler<bool> Topo2DToggled;

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
        _topo2D.Toggled += _OnTopo2DToggled;
        _topoX.ValueChanged += _OnTopoXChanged;
        _topoY.ValueChanged += _OnTopoYChanged;
        _topoZ.ValueChanged += _OnTopoZChanged;
        _topoHeight.ValueChanged += _OnTopoHeightChanged;
        _cost.ValueChanged += _OnCostChanged;
        _canMoveThrough.Toggled += _OnCanMoveThroughToggled;
        _canViewThrough.Toggled += _OnCanViewThroughToggled;
        _murFinInfo.ValueChanged += _OnMurFinInfoChanged;
        _placement.ItemSelected += _OnPlacementChanged;
        _bonus.ItemSelected += _OnBonusChanged;
        _centerX.ValueChanged += _OnCenterXChanged;
        _centerY.ValueChanged += _OnCenterYChanged;
    }

    public void Reset()
    {
        _elementData = null;
        _pathData = null;
        _visibilityData = null;
        _fightData = null;
        
        _suppressSignals = true;
        
        _cellX.Value = 0;
        _cellY.Value = 0;
        _cellZ.Value = 0;
        
        _offsetX.Text = "0";
        _offsetY.Text = "0";
        _height.Value = 0;
        
        _gfxId.Text = "0";
        _order.Value = 0;
        _hashcode.Text = "0";
        _groupId.Value = 0;
        _layerIndex.Value = 0;
        _groupLayer.Value = 0;
        _properties.Text = "0";
        _sound.Text = "0";
        _slope.Text = "0";
        _walkable.ButtonPressed = false;
        
        _color.Color = Colors.White;
        _shader.Text = "0";
        _mask.Text = "0";
        _occluder.ButtonPressed = false;
        _flip.ButtonPressed = false;
        _animated.ButtonPressed = false;
        _topoX.Value = 0;
        _topoY.Value = 0;
        _topoZ.Value = 0;
        _topoHeight.Value = 0;
        _cost.Value = 0;
        _canMoveThrough.ButtonPressed = false;
        _canViewThrough.ButtonPressed = false;
        _murFinInfo.Value = 0;
        _miscProperties.Value = 0;
        
        _placement.Selected = 0;
        _bonus.Selected = 0;
        _centerX.Value = 0;
        _centerY.Value = 0;
        
        _suppressSignals = false;
    }

    public void UpdateGfx(GfxData.Element element)
    {
        _elementData = element;
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
        _walkable.ButtonPressed = element.Walkable;

        _color.Color = element.Colors.Length == 3
            ? new Color(element.Colors[0], element.Colors[1], element.Colors[2])
            : Colors.White;
        _shader.Text = element.CommonData.ShaderId.ToString();
        _mask.Text = element.CommonData.VisibilityMask.ToString();
        _occluder.ButtonPressed = element.Occluder;
        _flip.ButtonPressed = element.CommonData.Flip;
        _animated.ButtonPressed = element.CommonData.Animated;
        
        _suppressSignals = false;
    }

    public void UpdateTopology(TopologyData.CellPathData pathData, TopologyData.CellVisibilityData visibilityData)
    {
        _pathData = pathData;
        _visibilityData = visibilityData;
        _suppressSignals = true;
        
        _topoX.Value = pathData.X;
        _topoY.Value = pathData.Y;
        _topoZ.Value = pathData.Z;
        _topoHeight.Value = pathData.Height;
        _cost.Value = pathData.Cost;
        _canMoveThrough.ButtonPressed = pathData.CanMoveThrough;
        _canViewThrough.ButtonPressed = visibilityData.CanViewThrough;
        _murFinInfo.Value = pathData.MurFinInfo;
        _miscProperties.Value = pathData.MiscProperties;
        
        if (_fightData != null)
        {
            var (placement, bonus) = _fightData.GetData(_pathData.X, _pathData.Y, _pathData.Z);
            _placement.Selected = placement + 1;
            _bonus.Selected = bonus + 1;
        }

        _suppressSignals = false;
    }

    public void UpdateFight(FightData fightData)
    {
        _fightData = fightData;
        _suppressSignals = true;

        if (_pathData != null)
        {
            var (placement, bonus) = fightData.GetData(_pathData.X, _pathData.Y, _pathData.Z);
            _placement.Selected = placement + 1;
            _bonus.Selected = bonus + 1;
        }
        
        _centerX.Value = fightData.MapCenter.x;
        _centerY.Value = fightData.MapCenter.y;
        
        _suppressSignals = false;
    }

    public void SwitchToMode(Enums.Mode mode)
    {
        switch (mode)
        {
            case Enums.Mode.Gfx:
                _gfxContainer.Visible = true;
                _topologyContainer.Visible = false;
                break;
            case Enums.Mode.Topology:
                _gfxContainer.Visible = false;
                _topologyContainer.Visible = true;
                break;
        }
    }

    private void _OnXChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.CellX = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnYChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.CellY = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnZChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.CellZ = (short) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }

    private void _OnHeightChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.Height = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }

    private void _OnOrderChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.AltitudeOrder = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnGroupIdChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.GroupId = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnLayerIndexChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.LayerIndex = (sbyte) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnGroupLayerChanged(double value)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.GroupKey = (int) value;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnColorChanged(Color newColor)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.Color = newColor;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnOccluderToggled(bool toggledOn)
    {
        if (_suppressSignals || _elementData == null)
            return;

        var newElement = _elementData.Copy();
        newElement.Occluder = toggledOn;
        ElementUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }
    
    private void _OnTopo2DToggled(bool toggledOn)
    {
        Topo2DToggled?.Invoke(this, toggledOn);
    }
    
    private void _OnTopoXChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;

        _pathData.X = (int) value;
        _visibilityData.X = (int) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoYChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;
        
        _pathData.Y = (int) value;
        _visibilityData.Y = (int) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoZChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;
        
        _pathData.Z = (short) value;
        _visibilityData.Z = (short) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnTopoHeightChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;
        
        _pathData.Height = (sbyte) value;
        _visibilityData.Height = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCostChanged(double value)
    {
        if (_suppressSignals || _pathData == null) 
            return;
        
        _pathData.Cost = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCanMoveThroughToggled(bool toggledOn)
    {
        if (_suppressSignals || _pathData == null) 
            return;
        
        _pathData.CanMoveThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnCanViewThroughToggled(bool toggledOn)
    {
        if (_suppressSignals || _visibilityData == null)
            return;
        
        _visibilityData.CanViewThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnMurFinInfoChanged(double value)
    {
        if (_suppressSignals || _pathData == null)
            return;
        
        _pathData.MurFinInfo = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }
    
    private void _OnMiscPropertiesChanged(double value)
    {
        if (_suppressSignals || _pathData == null)
            return;
        
        _pathData.MiscProperties = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData));
    }

    private void _OnPlacementChanged(long id)
    {
        if (_suppressSignals || _fightData == null || _pathData == null)
            return;
        
        var oldFightData = _fightData.Copy();
        switch (id)
        {
            case 0:
                _fightData.RemovePlacement(_pathData.X, _pathData.Y, _pathData.Z);
                break;
            case 3:
                _fightData.AddCoach(_pathData.X, _pathData.Y, _pathData.Z);
                break;
            default:
                _fightData.AddStart(_pathData.X, _pathData.Y, _pathData.Z, (int) id - 1);
                break;
        }
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, _fightData));
    }

    private void _OnBonusChanged(long id)
    {
        if (_suppressSignals || _fightData == null || _pathData == null)
            return;
        
        var oldFightData = _fightData.Copy();
        if (id == 0)
            _fightData.RemoveBonus(_pathData.X, _pathData.Y, _pathData.Z);
        else
            _fightData.AddBonus(_pathData.X, _pathData.Y, _pathData.Z, (int) id + 1001);
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, _fightData));
    }

    private void _OnCenterXChanged(double value)
    {
        if (_suppressSignals || _fightData == null)
            return;
        
        var oldFightData = _fightData.Copy();
        _fightData.MapCenter.x = (int)value;
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, _fightData));
    }

    private void _OnCenterYChanged(double value)
    {
        if (_suppressSignals || _fightData == null)
            return;

        var oldFightData = _fightData.Copy();
        _fightData.MapCenter.y = (int)value;
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, _fightData));
    }

    public class ElementUpdatedEventArgs(GfxData.Element oldElement, GfxData.Element newElement) : EventArgs
    {
        public GfxData.Element OldElement => oldElement;
        public GfxData.Element NewElement => newElement;
    }

    public class TopologyUpdatedEventArgs(TopologyData.CellPathData path, TopologyData.CellVisibilityData visibility)
        : EventArgs
    {
        public TopologyData.CellPathData Path => path;
        public TopologyData.CellVisibilityData Visibility => visibility;
    }

    public class FightUpdatedEventArgs(FightData oldFightData, FightData newFightData) : EventArgs
    {
        public FightData OldFightData => oldFightData;
        public FightData NewFightData => newFightData;
    }
}
