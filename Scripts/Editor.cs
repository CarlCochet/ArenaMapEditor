using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public partial class Editor : Node2D
{
	[Export] private Map _map;
	[Export] private Filter _filter;
	[Export] private AssetsPreview _assetsPreview;
	[Export] private Tools _tools;
	[Export] private Overlay _overlay;
	[Export] private FileDialog _openDialog;
	[Export] private FileDialog _saveDialog;
	[Export] private Inspector _inspector;
	[Export] private Gizmo _gizmo;

	private string _mapPath;
	private string _contentPath;
	private int _x;
	private int _y;
	private int _z; 
	private string _lastDir;
	private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
	
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(1200, 600));

		_map.SetUiHitTest(IsPointOverUi);

		_filter.FilterUpdated += (_, _) => _assetsPreview.DisplayAssets(_filter.Biome, _filter.Category, _filter.Mode);
		_filter.ModeUpdated += _OnModeUpdated;
		_filter.MouseEntered += () => _map.UpdateFocus(false);
		_filter.MouseExited += () => _map.UpdateFocus(true);

		_tools.MouseEntered += () => _map.UpdateFocus(false);
		_tools.MouseExited += () => _map.UpdateFocus(true);
		_tools.MapSelected += _OnMapSelected;
		_tools.LocateArenaPressed += (_, _) => _openDialog.Visible = true;
		_tools.ExportMapPressed += (_, _) => _saveDialog.Visible = true;
		_tools.ToolSelected += (_, e) => _map.ShowPlacementPreview(e);
		_tools.FlipPressed += _map.FlipPlacementPreview;
		_tools.UndoPressed += _map.Undo;
		_tools.RedoPressed += _map.Redo;
		_tools.NewMapPressed += _OnNewMapPressed;
		_tools.ColorChanged += (_, e) => GlobalData.Instance.SelectedColor = e.Color;

		_overlay.InterfaceEntered += (_, _) => _map.UpdateFocus(false);
		_overlay.InterfaceExited += (_, _) => _map.UpdateFocus(true);
		_overlay.PreviewChanged += _OnPreviewChanged;
		_overlay.HeightChanged += _OnHeightChanged;
		_overlay.HighlightHeightToggled += OnHighlightHeightToggled;
		_overlay.GenerateTopologyPressed += _OnGenerateTopologyPressed;
	
		_assetsPreview.MouseEntered += () => _map.UpdateFocus(false);
		_assetsPreview.MouseExited += () => _map.UpdateFocus(true);
		_assetsPreview.AssetSelected += (_, e) => _map.UpdatePreview(e.Element);

		_inspector.ElementUpdated += (_, e) => _map.RegisterUpdateElement(e.OldElement, e.NewElement);
		_inspector.TopologyUpdated += (_, e) => _map.RegisterUpdateTopologyCell(e.Path, e.Visibility);
		_inspector.FightUpdated += (_, e) => _map.RegisterUpdateFight(e.OldFightData, e.NewFightData);
		_inspector.MouseEntered += () => _map.UpdateFocus(false);
		_inspector.MouseExited += () => _map.UpdateFocus(true);
		_inspector.Topo2DToggled += (_, is2D) => _map.ToggleTopologyRender(is2D);
		
		_map.GfxTileSelected += OnGfxTileSelected;
		_map.TopologyTileSelected += OnTopologyTileSelected;
		_map.EnvTileSelected += OnEnvTileSelected;
		
		_inspector.EnvElementUpdated += (_, e) =>
		{
			if (e.MoveToGlobalX.HasValue)
				_map.RegisterMoveEnvElement(e.OldElement, e.MoveToGlobalX.Value, e.MoveToGlobalY!.Value, e.Index);
			else if (e.OldElement == null && e.NewElement == null)
				_map.RegisterAddEnvElement();
			else if (e.NewElement == null)
				_map.RegisterRemoveEnvElement(e.Index);
			else
				_map.RegisterUpdateEnvElement(e.OldElement, e.NewElement, e.Index);
		};
		
		GlobalData.Instance.LoadAssets();
		_assetsPreview.DisplayAssets(_filter.Biome, _filter.Category, _filter.Mode);
		
		GlobalData.Instance.LoadSettings();
		if (GlobalData.Instance.Settings != null)
			_OnDirectorySelected(GlobalData.Instance.Settings.ArenaPath);
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("save"))
		{
			if (_lastDir != null)
				SaveMap(_lastDir);
			else
				_saveDialog.Visible = true;
		}
		
		if (_map.SelectedTile == null && _map.SelectedElement == null)
			return;
		_gizmo.Position = _map.SelectedTileGlobalPosition;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			(_x, _y) = _map.PositionToCoord(GetGlobalMousePosition(), _z);
			_overlay.UpdatePosition(_x, _y, _z);
		}
	}
	
	private void OnGfxTileSelected(object sender, Map.GfxTileSelectedEventArgs e)
	{
		_inspector.UpdateGfx(e.Element);
		_assetsPreview.Update(e.Element);
		_tools.Update(e.Element);
		_overlay.Update(e.Element);
		_z = e.Element.CellZ;
	}
	
	private void OnTopologyTileSelected(object sender, Map.TopologyTileSelectedEventArgs e)
	{
		_inspector.UpdateTopology(e.PathData, e.VisibilityData);
		_z = e.PathData.Z;
	}
	
	private void OnEnvTileSelected(object sender, Map.EnvTileSelectedEventArgs e)
	{
		_inspector.UpdateEnv(e.Elements, e.X, e.Y, e.CurrentIndex);
	}

	private void OnHighlightHeightToggled(object sender, Overlay.HighlightHeightToggledEventArgs e)
	{
		_map.ToggleHeightHighlight(e.ToggledOn, _z);
	}

	private void _OnGenerateTopologyPressed(object sender, EventArgs e)
	{
		_map.GenerateTopology();
	}

	private void _OnHeightChanged(object sender, Overlay.HeightChangedEventArgs e)
	{
		switch (e.Direction)
		{
			case Enums.Direction.Up:
				_z++;
				_map.UpdateHeight(_z);
				_overlay.UpdatePosition(_x, _y, _z);
				break;
			case Enums.Direction.Down:
				_z--;
				_map.UpdateHeight(_z);
				_overlay.UpdatePosition(_x, _y, _z);
				break;
		}
	}

	private void _OnPreviewChanged(object sender, Overlay.PreviewChangedEventArgs e)
	{
		
	}

	private void _OnModeUpdated(object sender, EventArgs e)
	{
		_map.UpdateDisplay(_filter.Mode);
		_inspector.SwitchToMode(_filter.Mode);
	}

	private void _OnMapSelected(object sender, Tools.MapSelectedEventArgs eventArgs)
	{
		var mapData = GlobalData.Instance.Maps[eventArgs.MapName];
		if (mapData == null)
			return;
		_map.Load(mapData);
		_filter.UpdateBiome(Enums.Biome.Global);
		_filter.UpdateCategory(Enums.Category.Global);
		_filter.UpdateMode(Enums.Mode.Gfx);
		_inspector.Reset();
		_inspector.UpdateFight(mapData.Fight);
	}

    private void _OnNewMapPressed(object sender, Tools.NewMapPressedEventArgs e)
    {
        _map.CreateNewMap(e.Id);
        _tools.SetMapOptions([..GlobalData.Instance.Maps.Keys]);
        _filter.UpdateBiome(Enums.Biome.Global);
        _filter.UpdateCategory(Enums.Category.Global);
        _filter.UpdateMode(Enums.Mode.Gfx);
        var mapData = GlobalData.Instance.Maps[$"{e.Id}"];
        _inspector.Reset();
        _inspector.UpdateFight(mapData?.Fight);
    }

	private void _OnDirectorySelected(string dir)
	{
		LoadMaps(dir);
	}

	private void _OnSaveDirectorySelected(string dir)
	{
		SaveMap(dir);
	}

	private void LoadMaps(string dir)
	{
		GlobalData.Instance.Settings ??= new Settings();
		using var dirAccess = DirAccess.Open(dir);
		if (dirAccess.DirExists("ArenaReturnsClient/game/contents/maps/env"))
			GlobalData.Instance.Settings.ArenaPath = $"{dir}/ArenaReturnsClient/game/contents";
		else if (dirAccess.DirExists("game/contents/maps/env"))
			GlobalData.Instance.Settings.ArenaPath = $"{dir}/game/contents";
		else if (dirAccess.DirExists("contents/maps/env"))
			GlobalData.Instance.Settings.ArenaPath = $"{dir}/contents";
		else if (dirAccess.DirExists("maps/env"))
			GlobalData.Instance.Settings.ArenaPath = dir;
		else
		{
			GD.PrintErr("Selected directory is not a valid Arena Client folder.");
			return;
		}
		
		GlobalData.Instance.LoadElements($"{GlobalData.Instance.Settings.ArenaPath}/maps");
		// GlobalData.Instance.LoadPlaylists($"{GlobalData.Instance.Settings.ArenaPath}/maps_sounds");

		if (!dirAccess.DirExists($"{GlobalData.Instance.Settings.ArenaPath}/maps/gfx"))
			return;

		List<string> mapNames = [];
		GlobalData.Instance.Maps.Clear();

		using var gfxDir = DirAccess.Open($"{GlobalData.Instance.Settings.ArenaPath}/maps/gfx");
		gfxDir.ListDirBegin();
		var name = gfxDir.GetNext();
		while (name != "")
		{
			var mapName = name.EndsWith(".jar") ? name.Split(".")[0] : name;
			mapNames.Add(mapName);
			GlobalData.Instance.LoadMap(mapName);
			name = gfxDir.GetNext();
		}
		gfxDir.ListDirEnd();
		_tools.SetMapOptions(mapNames);
		GlobalData.Instance.SaveSettings();
	}
	
	private void SaveMap(string dir)
	{
		if (Directory.Exists(dir))
		{
			var directory = new DirectoryInfo(dir);
			foreach (var file in directory.GetFiles()) file.Delete();
			foreach (var subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
		}
		
		DirAccess.MakeDirRecursiveAbsolute(dir);
		using var dirAccess = DirAccess.Open(dir);
		dirAccess.MakeDir("json");

		var allMapsData = new List<object>();
		foreach (var map in GlobalData.Instance.Maps.Values)
		{
			map.Save(dir);
			if (map.Fight != null)
				allMapsData.Add(map.Fight.GetSaveObject());
		}
		GlobalData.Instance.SaveElements($"{dir}/maps");
		// GlobalData.Instance.SavePlaylists($"{dir}/maps_sounds");
		File.WriteAllText($"{dir}/fight_map_info.json", JsonSerializer.Serialize(allMapsData, _jsonOptions));
		_lastDir = dir;
	}

	private bool IsPointOverUi(Vector2 globalPosition)
	{
		if (_filter.GetGlobalRect().HasPoint(globalPosition))
			return true;
		if (_filter.GetParent() is Control filterParent && filterParent.GetGlobalRect().HasPoint(globalPosition))
			return true;
		return _tools.GetGlobalRect().HasPoint(globalPosition)
		       || _inspector.GetGlobalRect().HasPoint(globalPosition)
		       || _assetsPreview.GetGlobalRect().HasPoint(globalPosition)
		       || _overlay.GetGlobalRect().HasPoint(globalPosition);
	}
}
