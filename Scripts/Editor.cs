using Godot;
using System;
using System.Collections.Generic;

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
	
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(1200, 600));
		GlobalData.Instance.LoadAssets();
		GlobalData.Instance.LoadSettings();
		
		_assetsPreview.DisplayAssets(_filter.Biome, _filter.Category, _filter.Mode);

		_filter.FilterUpdated += (_, _) => _assetsPreview.DisplayAssets(_filter.Biome, _filter.Category, _filter.Mode);
		_filter.ModeUpdated += _OnModeUpdated;
		
		_tools.MouseEntered += () => _map.UpdateFocus(false);
		_tools.MouseExited += () => _map.UpdateFocus(true);
		_tools.MapSelected += _OnMapSelected;
		_tools.LocateArenaPressed += (_, _) => _openDialog.Visible = true;
		_tools.ExportMapPressed += (_, _) => _saveDialog.Visible = true;
		_tools.ToolSelected += (_, e) => _map.ShowPlacementPreview(e);
		_tools.UndoPressed += _map.Undo;
		_tools.RedoPressed += _map.Redo;
		_tools.NewMapPressed += _OnNewMapPressed;

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
		
		_map.TileSelected += _OnTileSelected;
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
		
		if (_map.SelectedTiles == null || _map.SelectedTiles.Count == 0)
			return;
		_gizmo.Position = _map.SelectedTiles[0].GetGlobalTransformWithCanvas().Origin;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			(_x, _y) = _map.PositionToCoord(GetGlobalMousePosition(), _z);
			_overlay.UpdatePosition(_x, _y, _z);
		}
	}
	
	private void _OnTileSelected(object sender, Map.TileSelectedEventArgs e)
	{
		_inspector.Update(e.Element, e.PathData, e.VisibilityData, e.FightData);
		_assetsPreview.Update(e.Element);
		_tools.Update(e.Element);
		_overlay.Update(e.Element);
		_z = e.Element.CellZ;
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
	}

	private void _OnNewMapPressed(object sender, Tools.NewMapPressedEventArgs e)
	{
		_map.CreateNewMap(e.Id);
	}

	private void _OnDirectorySelected(string dir)
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

		if (!dirAccess.DirExists($"{GlobalData.Instance.Settings.ArenaPath}/maps/fight"))
			return;
		
		List<string> mapNames = [];
		GlobalData.Instance.Maps.Clear();

		using var fightDir = DirAccess.Open($"{GlobalData.Instance.Settings.ArenaPath}/maps/fight");
		fightDir.ListDirBegin();
		var name = fightDir.GetNext();
		while (name != "")
		{
			var mapName = name.EndsWith(".jar") ? name.Split(".")[0] : name;
			mapNames.Add(mapName);
			GlobalData.Instance.LoadMap(mapName);
			name = fightDir.GetNext();
		}
		fightDir.ListDirEnd();
		
		_tools.SetMapOptions(mapNames);
		GlobalData.Instance.SaveSettings();
	}

	private void _OnSaveDirectorySelected(string dir)
	{
		SaveMap(dir);
	}

	private void SaveMap(string dir)
	{
		DirAccess.MakeDirRecursiveAbsolute(dir);
		using var dirAccess = DirAccess.Open(dir);
		dirAccess.MakeDir("json");

		foreach (var map in GlobalData.Instance.Maps.Values)
		{
			map.Save(dir);
		}
		GlobalData.Instance.SaveElements($"{dir}/maps");
		// GlobalData.Instance.SavePlaylists($"{dir}/maps_sounds");
		_lastDir = dir;
	}
}
