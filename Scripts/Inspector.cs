using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class Inspector : Control
{
    [ExportGroup("GFX")]
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
    
    [ExportGroup("Topography")]
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
    [Export] private Label _topoLayer;
    [Export] private OptionButton _placement;
    [Export] private OptionButton _bonus;
    [Export] private VBoxContainer _gfxContainer;
    [Export] private VBoxContainer _topologyContainer;
    [Export] private SpinBox _centerX;
    [Export] private SpinBox _centerY;

    [ExportGroup("Environment")] 
    [Export] private Button _addElement;
    [Export] private Button _removeElement;
    [Export] private SpinBox _elements;
    [Export] private VBoxContainer _environmentContainer;
    [Export] private SpinBox _envX;
    [Export] private SpinBox _envY;
    [Export] private SpinBox _envZ;
    [Export] private VBoxContainer _particleContainer;
    [Export] private SpinBox _particleSystemId;
    [Export] private SpinBox _particleLevel;
    [Export] private SpinBox _particleOffsetX;
    [Export] private SpinBox _particleOffsetY;
    [Export] private SpinBox _particleOffsetZ;
    [Export] private SpinBox _particleLoD;
    [Export] private VBoxContainer _soundContainer;
    [Export] private SpinBox _soundSoundId;
    [Export] private VBoxContainer _interactiveContainer;
    [Export] private SpinBox _interactiveId;
    [Export] private SpinBox _interactiveType;
    [Export] private LineEdit _interactiveViews;
    [Export] private LineEdit _interactiveData;
    [Export] private CheckBox _interactiveClientOnly;
    [Export] private SpinBox _interactiveLandmarkType;
    [Export] private VBoxContainer _dynamicContainer;
    [Export] private SpinBox _dynamicId;
    [Export] private SpinBox _dynamicGfxId;
    [Export] private SpinBox _dynamicType;
    [Export] private SpinBox _dynamicDirection;
    
    private GfxData.Element _elementData;
    private TopologyData.CellPathData _pathData;
    private TopologyData.CellVisibilityData _visibilityData;
    private FightData _fightData;
    private int _topologyLayerIndex;

    private List<EnvData.Element> _envElements;
    private int _currentEnvIndex;
    private int _selectedGlobalX;
    private int _selectedGlobalY;
    
    private bool _suppressSignals;

    public event EventHandler<ElementUpdatedEventArgs> ElementUpdated;
    public event EventHandler<ElementUpdatedEventArgs> ElementColorUpdated;
    public event EventHandler<TopologyUpdatedEventArgs> TopologyUpdated;
    public event EventHandler<FightUpdatedEventArgs> FightUpdated;
    public event EventHandler<bool> Topo2DToggled;
    public event EventHandler<EnvElementUpdatedEventArgs> EnvElementUpdated;

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
        _walkable.Toggled += _OnWalkableToggled;
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
        _miscProperties.ValueChanged += _OnMiscPropertiesChanged;
        _placement.ItemSelected += _OnPlacementChanged;
        _bonus.ItemSelected += _OnBonusChanged;
        _centerX.ValueChanged += _OnCenterXChanged;
        _centerY.ValueChanged += _OnCenterYChanged;
        
        _elements.ValueChanged += _OnElementsIndexChanged;
        _addElement.Pressed += _OnEnvAddElement;
        _removeElement.Pressed += _OnEnvRemoveElement;
        _envX.ValueChanged += _OnEnvXChanged;
        _envY.ValueChanged += _OnEnvYChanged;
        _envZ.ValueChanged += _OnEnvZChanged;
        _particleSystemId.ValueChanged += _OnParticleSystemIdChanged;
        _particleLevel.ValueChanged += _OnParticleLevelChanged;
        _particleOffsetX.ValueChanged += _OnParticleOffsetXChanged;
        _particleOffsetY.ValueChanged += _OnParticleOffsetYChanged;
        _particleOffsetZ.ValueChanged += _OnParticleOffsetZChanged;
        _particleLoD.ValueChanged += _OnParticleLoDChanged;
        _soundSoundId.ValueChanged += _OnSoundSoundIdChanged;
        _interactiveId.ValueChanged += _OnInteractiveIdChanged;
        _interactiveType.ValueChanged += _OnInteractiveTypeChanged;
        _interactiveViews.TextChanged += _OnInteractiveViewsChanged;
        _interactiveData.TextChanged += _OnInteractiveDataChanged;
        _interactiveClientOnly.Toggled += _OnInteractiveClientOnlyToggled;
        _interactiveLandmarkType.ValueChanged += _OnInteractiveLandmarkTypeChanged;
        _dynamicId.ValueChanged += _OnDynamicIdChanged;
        _dynamicGfxId.ValueChanged += _OnDynamicGfxIdChanged;
        _dynamicType.ValueChanged += _OnDynamicTypeChanged;
        _dynamicDirection.ValueChanged += _OnDynamicDirectionChanged;
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event.IsActionPressed("cost") && _pathData != null && !@event.IsActionPressed("copy"))
            _OnCostChanged(_pathData.Cost == 0 ? -1 : 0);
        if (@event.IsActionPressed("hole") && _pathData != null && !@event.IsActionPressed("cut"))
            _OnTopoZChanged(short.MinValue);

        if (@event.IsActionPressed("up"))
        {
            if (_elementData != null)
                _OnZChanged(_elementData.CellZ + 1);
            if (_pathData != null)
                _OnTopoZChanged(_pathData.Z + 1);
        }
        if (@event.IsActionPressed("down"))
        {
            if (_elementData != null)
                _OnZChanged(_elementData.CellZ - 1);
            if (_pathData != null)
                _OnTopoZChanged(_pathData.Z - 1);
        }
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
        
        _topo2D.ButtonPressed = false;
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
        
        SwitchToMode(Enums.Mode.Gfx);
        
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

        _color.Color = (element.TypeMask & GfxData.Element.TeintMask) != 0 && element.Colors.Length >= 3
            ? new Color(element.Colors[0], element.Colors[1], element.Colors[2])
            : Colors.White;
        _shader.Text = element.CommonData.ShaderId.ToString();
        _mask.Text = element.CommonData.VisibilityMask.ToString();
        _occluder.ButtonPressed = element.Occluder;
        _flip.ButtonPressed = element.CommonData.Flip;
        _animated.ButtonPressed = element.CommonData.Animated;
        
        _suppressSignals = false;
    }

    public void UpdateTopology(TopologyData.CellPathData pathData,
        TopologyData.CellVisibilityData visibilityData, int layerIndex)
    {
        _pathData = pathData;
        _visibilityData = visibilityData;
        _topologyLayerIndex = layerIndex;
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
        _topoLayer.Text = layerIndex.ToString();
        
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

        if (fightData != null)
        {
            if (_pathData != null)
            {
                var (placement, bonus) = fightData.GetData(_pathData.X, _pathData.Y, _pathData.Z);
                _placement.Selected = placement + 1;
                _bonus.Selected = bonus + 1;
            }
            
            _centerX.Value = fightData.MapCenter.x;
            _centerY.Value = fightData.MapCenter.y;
        }
        
        _suppressSignals = false;
    }

    public void SwitchToMode(Enums.Mode mode)
    {
        switch (mode)
        {
            case Enums.Mode.Gfx:
                _gfxContainer.Visible = true;
                _topologyContainer.Visible = false;
                _environmentContainer.Visible = false;
                break;
            case Enums.Mode.Topology:
                _gfxContainer.Visible = false;
                _topologyContainer.Visible = true;
                _environmentContainer.Visible = false;
                break;
            case Enums.Mode.Environment:
                _gfxContainer.Visible = false;
                _topologyContainer.Visible = false;
                _environmentContainer.Visible = true;
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
        newElement.Colors = [newColor.R, newColor.G, newColor.B];
        newElement.TypeMask = GfxData.Element.GetColorType(newElement.Colors);
        ElementColorUpdated?.Invoke(this, new ElementUpdatedEventArgs(_elementData, newElement));
    }

    private void _OnWalkableToggled(bool toggledOn)
    {
        if (_suppressSignals || _elementData == null)
            return;
        
        var newElement = _elementData.Copy();
        newElement.Walkable = toggledOn;
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
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnTopoYChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;
        
        _pathData.Y = (int) value;
        _visibilityData.Y = (int) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnTopoZChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;

        var newPathData = new TopologyData.CellPathData(_pathData)
        {
            Z = (short)value
        };
        var newVisibilityData = new TopologyData.CellVisibilityData
        {
            X = _visibilityData.X,
            Y = _visibilityData.Y,
            Z = (short)value,
            Height = _visibilityData.Height,
            CanViewThrough = _visibilityData.CanViewThrough
        };
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(newPathData, newVisibilityData, _topologyLayerIndex));
    }
    
    private void _OnTopoHeightChanged(double value)
    {
        if (_suppressSignals || _pathData == null || _visibilityData == null)
            return;
        
        _pathData.Height = (sbyte) value;
        _visibilityData.Height = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnCostChanged(double value)
    {
        if (_suppressSignals || _pathData == null) 
            return;
        
        _pathData.Cost = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnCanMoveThroughToggled(bool toggledOn)
    {
        if (_suppressSignals || _pathData == null) 
            return;
        
        _pathData.CanMoveThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnCanViewThroughToggled(bool toggledOn)
    {
        if (_suppressSignals || _visibilityData == null)
            return;
        
        _visibilityData.CanViewThrough = toggledOn;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnMurFinInfoChanged(double value)
    {
        if (_suppressSignals || _pathData == null)
            return;
        
        _pathData.MurFinInfo = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }
    
    private void _OnMiscPropertiesChanged(double value)
    {
        if (_suppressSignals || _pathData == null)
            return;
        
        _pathData.MiscProperties = (sbyte) value;
        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_pathData, _visibilityData, _topologyLayerIndex));
    }

    private void _OnPlacementChanged(long id)
    {
        if (_suppressSignals || _fightData == null || _pathData == null)
            return;
        
        var oldFightData = _fightData;
        var newFightData = _fightData.Copy();
        switch (id)
        {
            case 0:
                newFightData.RemovePlacement(_pathData.X, _pathData.Y, _pathData.Z);
                break;
            case 3:
                newFightData.AddCoach(_pathData.X, _pathData.Y, _pathData.Z);
                break;
            default:
                newFightData.AddStart(_pathData.X, _pathData.Y, _pathData.Z, (int) id - 1);
                break;
        }
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, newFightData));
    }

    private void _OnBonusChanged(long id)
    {
        if (_suppressSignals || _fightData == null || _pathData == null)
            return;
        
        var oldFightData = _fightData;
        var newFightData = _fightData.Copy();
        if (id == 0)
            newFightData.RemoveBonus(_pathData.X, _pathData.Y, _pathData.Z);
        else
            newFightData.AddBonus(_pathData.X, _pathData.Y, _pathData.Z, (int) id + 1001);
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, newFightData));
    }

    private void _OnCenterXChanged(double value)
    {
        if (_suppressSignals || _fightData == null)
            return;
        
        var oldFightData = _fightData;
        var newFightData = _fightData.Copy();
        newFightData.MapCenter.x = (int)value;
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, newFightData));
    }

    private void _OnCenterYChanged(double value)
    {
        if (_suppressSignals || _fightData == null)
            return;

        var oldFightData = _fightData;
        var newFightData = _fightData.Copy();
        newFightData.MapCenter.y = (int)value;
        FightUpdated?.Invoke(this, new FightUpdatedEventArgs(oldFightData, newFightData));
    }

    private void SwitchCost()
    {
        if (_pathData == null) 
            return;
        _OnCostChanged(_pathData.Cost == 0 ? -1 : 0);
    }

    private void MakeHole()
    {
        if (_pathData == null) 
            return;
        _OnZChanged(short.MinValue);
    }

    public void UpdateEnv(List<EnvData.Element> elements, int globalX, int globalY, int index)
    {
        _envElements = elements;
        _selectedGlobalX = globalX;
        _selectedGlobalY = globalY;
        _suppressSignals = true;

        if (elements.Count > 0)
        {
            _elements.MinValue = 0;
            _elements.MaxValue = elements.Count - 1;
            _elements.Value = index;
        }

        _currentEnvIndex = index;
        DisplayEnvElement();
        _suppressSignals = false;
    }

    private void DisplayEnvElement()
    {
        if (_envElements == null || _envElements.Count == 0 || _currentEnvIndex >= _envElements.Count)
        {
            ClearEnvDisplay();
            return;
        }

        var element = _envElements[_currentEnvIndex];
        _envX.Value = _selectedGlobalX;
        _envY.Value = _selectedGlobalY;
        _envZ.Value = element.Z;

        _particleContainer.Visible = false;
        _soundContainer.Visible = false;
        _interactiveContainer.Visible = false;
        _dynamicContainer.Visible = false;

        switch (element)
        {
            case EnvData.ParticleDef p:
                _particleContainer.Visible = true;
                _particleSystemId.Value = p.SystemId;
                _particleLevel.Value = p.Level;
                _particleOffsetX.Value = p.OffsetX;
                _particleOffsetY.Value = p.OffsetY;
                _particleOffsetZ.Value = p.OffsetZ;
                _particleLoD.Value = p.LoD;
                break;
            case EnvData.Sound s:
                _soundContainer.Visible = true;
                _soundSoundId.Value = s.SoundId;
                break;
            case EnvData.InteractiveElement ie:
                _interactiveContainer.Visible = true;
                _interactiveId.Value = ie.Id;
                _interactiveType.Value = ie.Type;
                _interactiveViews.Text = string.Join(",", ie.Views ?? []);
                _interactiveData.Text = string.Join(",", (ie.Data ?? []).Select(b => b.ToString()));
                _interactiveClientOnly.ButtonPressed = ie.ClientOnly;
                _interactiveLandmarkType.Value = ie.LandmarkType;
                break;
            case EnvData.DynamicElement de:
                _dynamicContainer.Visible = true;
                _dynamicId.Value = de.Id;
                _dynamicGfxId.Value = de.GfxId;
                _dynamicType.Value = de.Type;
                _dynamicDirection.Value = de.Direction;
                break;
        }
    }

    private void ClearEnvDisplay()
    {
        _selectedGlobalX = 0;
        _selectedGlobalY = 0;
        _envX.Value = 0;
        _envY.Value = 0;
        _envZ.Value = 0;
        _elements.MinValue = 0;
        _elements.MaxValue = 0;
        _elements.Value = 0;
        _particleContainer.Visible = false;
        _soundContainer.Visible = false;
        _interactiveContainer.Visible = false;
        _dynamicContainer.Visible = false;
    }

    private void FireEnvUpdate(EnvData.Element oldElement, EnvData.Element newElement)
    {
        EnvElementUpdated?.Invoke(this, new EnvElementUpdatedEventArgs(oldElement, newElement, _currentEnvIndex));
    }

    private EnvData.Element CopyEnvElement(EnvData.Element source)
    {
        switch (source)
        {
            case EnvData.ParticleDef p:
                return new EnvData.ParticleDef
                {
                    X = p.X, Y = p.Y, Z = p.Z,
                    SystemId = p.SystemId, Level = p.Level,
                    OffsetX = p.OffsetX, OffsetY = p.OffsetY, OffsetZ = p.OffsetZ, LoD = p.LoD
                };
            case EnvData.Sound s:
                return new EnvData.Sound { X = s.X, Y = s.Y, Z = s.Z, SoundId = s.SoundId };
            case EnvData.InteractiveElement ie:
                return new EnvData.InteractiveElement
                {
                    X = ie.X, Y = ie.Y, Z = ie.Z,
                    Id = ie.Id, Type = ie.Type,
                    Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                    Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                    ClientOnly = ie.ClientOnly, LandmarkType = ie.LandmarkType
                };
            case EnvData.DynamicElement de:
                return new EnvData.DynamicElement
                {
                    X = de.X, Y = de.Y, Z = de.Z,
                    Id = de.Id, GfxId = de.GfxId, Type = de.Type, Direction = de.Direction
                };
            default:
                return null;
        }
    }

    private void _OnElementsIndexChanged(double value)
    {
        if (_suppressSignals || _envElements == null)
            return;
        _currentEnvIndex = (int)value;
        _suppressSignals = true;
        DisplayEnvElement();
        _suppressSignals = false;
    }

    private void _OnEnvAddElement()
    {
        EnvElementUpdated?.Invoke(this, new EnvElementUpdatedEventArgs(null, null, -1));
    }

    private void _OnEnvRemoveElement()
    {
        if (_envElements == null || _currentEnvIndex >= _envElements.Count)
            return;
        EnvElementUpdated?.Invoke(this, new EnvElementUpdatedEventArgs(
            _envElements[_currentEnvIndex], null, _currentEnvIndex));
    }

    private void _OnEnvXChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count)
            return;
        var element = _envElements[_currentEnvIndex];
        EnvElementUpdated?.Invoke(this, new EnvElementUpdatedEventArgs(
            element, null, _currentEnvIndex, moveToGlobalX: (int)value, moveToGlobalY: _selectedGlobalY));
    }

    private void _OnEnvYChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count)
            return;
        var element = _envElements[_currentEnvIndex];
        EnvElementUpdated?.Invoke(this, new EnvElementUpdatedEventArgs(
            element, null, _currentEnvIndex, moveToGlobalX: _selectedGlobalX, moveToGlobalY: (int)value));
    }

    private void _OnEnvZChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count)
            return;
        var newElement = CopyEnvElement(_envElements[_currentEnvIndex]);
        if (newElement == null) return;
        newElement.Z = (short)value;
        FireEnvUpdate(_envElements[_currentEnvIndex], newElement);
    }

    private void _OnParticleSystemIdChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.Element newElement;
        if (current is EnvData.ParticleDef p)
        {
            newElement = new EnvData.ParticleDef
            {
                X = p.X, Y = p.Y, Z = p.Z, SystemId = (int)value,
                Level = p.Level, OffsetX = p.OffsetX, OffsetY = p.OffsetY, OffsetZ = p.OffsetZ, LoD = p.LoD
            };
        }
        else return;
        FireEnvUpdate(current, newElement);
    }

    private void _OnParticleLevelChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        if (current is not EnvData.ParticleDef p) return;
        var newElement = new EnvData.ParticleDef
        {
            X = p.X, Y = p.Y, Z = p.Z, SystemId = p.SystemId,
            Level = (sbyte)value, OffsetX = p.OffsetX, OffsetY = p.OffsetY, OffsetZ = p.OffsetZ, LoD = p.LoD
        };
        FireEnvUpdate(current, newElement);
    }

    private void _OnParticleOffsetXChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        if (current is not EnvData.ParticleDef p) return;
        var newElement = new EnvData.ParticleDef
        {
            X = p.X, Y = p.Y, Z = p.Z, SystemId = p.SystemId,
            Level = p.Level, OffsetX = (sbyte)value, OffsetY = p.OffsetY, OffsetZ = p.OffsetZ, LoD = p.LoD
        };
        FireEnvUpdate(current, newElement);
    }

    private void _OnParticleOffsetYChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        if (current is not EnvData.ParticleDef p) return;
        var newElement = new EnvData.ParticleDef
        {
            X = p.X, Y = p.Y, Z = p.Z, SystemId = p.SystemId,
            Level = p.Level, OffsetX = p.OffsetX, OffsetY = (sbyte)value, OffsetZ = p.OffsetZ, LoD = p.LoD
        };
        FireEnvUpdate(current, newElement);
    }

    private void _OnParticleOffsetZChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        if (current is not EnvData.ParticleDef p) return;
        var newElement = new EnvData.ParticleDef
        {
            X = p.X, Y = p.Y, Z = p.Z, SystemId = p.SystemId,
            Level = p.Level, OffsetX = p.OffsetX, OffsetY = p.OffsetY, OffsetZ = (sbyte)value, LoD = p.LoD
        };
        FireEnvUpdate(current, newElement);
    }

    private void _OnParticleLoDChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        if (current is not EnvData.ParticleDef p) return;
        var newElement = new EnvData.ParticleDef
        {
            X = p.X, Y = p.Y, Z = p.Z, SystemId = p.SystemId,
            Level = p.Level, OffsetX = p.OffsetX, OffsetY = p.OffsetY, OffsetZ = p.OffsetZ, LoD = (sbyte)value
        };
        FireEnvUpdate(current, newElement);
    }

    private void _OnSoundSoundIdChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.Element newElement;
        if (current is EnvData.Sound s)
        {
            newElement = new EnvData.Sound { X = s.X, Y = s.Y, Z = s.Z, SoundId = (int)value };
        }
        else if (current is EnvData.ParticleDef)
        {
            newElement = new EnvData.Sound { X = current.X, Y = current.Y, Z = current.Z, SoundId = (int)value };
        }
        else return;
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveIdChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = (long)value,
                Type = ie.Type,
                Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                ClientOnly = ie.ClientOnly, LandmarkType = ie.LandmarkType
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Id = (long)value, Views = new int[0], Data = new sbyte[0] };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveTypeChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = ie.Id,
                Type = (short)value,
                Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                ClientOnly = ie.ClientOnly, LandmarkType = ie.LandmarkType
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Type = (short)value, Views = new int[0], Data = new sbyte[0] };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveViewsChanged(string newText)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        var parts = newText.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var views = new int[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            int.TryParse(parts[i].Trim(), out views[i]);
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = ie.Id,
                Type = ie.Type, Views = views,
                Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                ClientOnly = ie.ClientOnly, LandmarkType = ie.LandmarkType
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Views = views, Data = new sbyte[0] };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveDataChanged(string newText)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        var parts = newText.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var data = new sbyte[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            sbyte.TryParse(parts[i].Trim(), out data[i]);
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = ie.Id,
                Type = ie.Type,
                Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                Data = data, ClientOnly = ie.ClientOnly, LandmarkType = ie.LandmarkType
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Views = new int[0], Data = data };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveClientOnlyToggled(bool toggledOn)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = ie.Id,
                Type = ie.Type,
                Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                ClientOnly = toggledOn, LandmarkType = ie.LandmarkType
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Views = new int[0], Data = new sbyte[0], ClientOnly = toggledOn };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnInteractiveLandmarkTypeChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.InteractiveElement newElement;
        if (current is EnvData.InteractiveElement ie)
        {
            newElement = new EnvData.InteractiveElement
            {
                X = ie.X, Y = ie.Y, Z = ie.Z, Id = ie.Id,
                Type = ie.Type,
                Views = ie.Views != null ? (int[])ie.Views.Clone() : new int[0],
                Data = ie.Data != null ? (sbyte[])ie.Data.Clone() : new sbyte[0],
                ClientOnly = ie.ClientOnly, LandmarkType = (short)value
            };
        }
        else
        {
            newElement = new EnvData.InteractiveElement
                { X = current.X, Y = current.Y, Z = current.Z, Views = new int[0], Data = new sbyte[0], LandmarkType = (short)value };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnDynamicIdChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.DynamicElement newElement;
        if (current is EnvData.DynamicElement de)
        {
            newElement = new EnvData.DynamicElement
            {
                X = de.X, Y = de.Y, Z = de.Z, Id = (int)value,
                GfxId = de.GfxId, Type = de.Type, Direction = de.Direction
            };
        }
        else
        {
            newElement = new EnvData.DynamicElement
                { X = current.X, Y = current.Y, Z = current.Z, Id = (int)value };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnDynamicGfxIdChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.DynamicElement newElement;
        if (current is EnvData.DynamicElement de)
        {
            newElement = new EnvData.DynamicElement
            {
                X = de.X, Y = de.Y, Z = de.Z, Id = de.Id,
                GfxId = (int)value, Type = de.Type, Direction = de.Direction
            };
        }
        else
        {
            newElement = new EnvData.DynamicElement
                { X = current.X, Y = current.Y, Z = current.Z, GfxId = (int)value };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnDynamicTypeChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.DynamicElement newElement;
        if (current is EnvData.DynamicElement de)
        {
            newElement = new EnvData.DynamicElement
            {
                X = de.X, Y = de.Y, Z = de.Z, Id = de.Id,
                GfxId = de.GfxId, Type = (short)value, Direction = de.Direction
            };
        }
        else
        {
            newElement = new EnvData.DynamicElement
                { X = current.X, Y = current.Y, Z = current.Z, Type = (short)value };
        }
        FireEnvUpdate(current, newElement);
    }

    private void _OnDynamicDirectionChanged(double value)
    {
        if (_suppressSignals || _envElements == null || _currentEnvIndex >= _envElements.Count) return;
        var current = _envElements[_currentEnvIndex];
        EnvData.DynamicElement newElement;
        if (current is EnvData.DynamicElement de)
        {
            newElement = new EnvData.DynamicElement
            {
                X = de.X, Y = de.Y, Z = de.Z, Id = de.Id,
                GfxId = de.GfxId, Type = de.Type, Direction = (sbyte)value
            };
        }
        else
        {
            newElement = new EnvData.DynamicElement
                { X = current.X, Y = current.Y, Z = current.Z, Direction = (sbyte)value };
        }
        FireEnvUpdate(current, newElement);
    }

    public class ElementUpdatedEventArgs(GfxData.Element oldElement, GfxData.Element newElement) : EventArgs
    {
        public GfxData.Element OldElement => oldElement;
        public GfxData.Element NewElement => newElement;
    }

    public class TopologyUpdatedEventArgs(TopologyData.CellPathData path,
        TopologyData.CellVisibilityData visibility, int layerIndex)
        : EventArgs
    {
        public TopologyData.CellPathData Path => path;
        public TopologyData.CellVisibilityData Visibility => visibility;
        public int LayerIndex => layerIndex;
    }

    public class FightUpdatedEventArgs(FightData oldFightData, FightData newFightData) : EventArgs
    {
        public FightData OldFightData => oldFightData;
        public FightData NewFightData => newFightData;
    }

    public class EnvElementUpdatedEventArgs(EnvData.Element oldElement, EnvData.Element newElement, int index,
        int? moveToGlobalX = null, int? moveToGlobalY = null) : EventArgs
    {
        public EnvData.Element OldElement => oldElement;
        public EnvData.Element NewElement => newElement;
        public int Index => index;
        public int? MoveToGlobalX => moveToGlobalX;
        public int? MoveToGlobalY => moveToGlobalY;
    }
}
