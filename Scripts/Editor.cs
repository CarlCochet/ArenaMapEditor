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

		_overlay.InterfaceEntered += (_, _) => _map.UpdateFocus(false);
		_overlay.InterfaceExited += (_, _) => _map.UpdateFocus(true);
		_overlay.PreviewChanged += _OnPreviewChanged;
		_overlay.HeightChanged += _OnHeightChanged;
		_overlay.HighlightHeightToggled += OnHighlightHeightToggled;
		_overlay.GenerateTopologyPressed += _OnGenerateTopologyPressed;
		
		_assetsPreview.MouseEntered += () => _map.UpdateFocus(false);
		_assetsPreview.MouseExited += () => _map.UpdateFocus(true);
		_assetsPreview.AssetSelected += (_, e) => _map.UpdatePreview(e.Element);

		_inspector.ElementUpdated += (_, e) => _map.UpdateElement(e.OldElement, e.NewElement);
		_inspector.TopologyUpdated += (_, e) => _map.UpdateTopologyCell(e.Path, e.Visibility);
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
		_inspector.Update(e.Element, e.PathData, e.VisibilityData);
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

	private void _OnDirectorySelected(string dir)
	{
		LoadMapList(dir);
	}

	private void _OnSaveDirectorySelected(string dir)
	{
		SaveMap(dir);
	}

	private void SaveMap(string dir)
	{
		DirAccess.MakeDirRecursiveAbsolute(dir);
		using var dirAccess = DirAccess.Open(dir);

		dirAccess.MakeDir("env");
		dirAccess.MakeDir("fight");
		dirAccess.MakeDir("gfx");
		dirAccess.MakeDir("light");
		dirAccess.MakeDir("tplg");
		dirAccess.MakeDir("json");

		foreach (var map in GlobalData.Instance.Maps.Values)
		{
			map.Save(dir);
		}
		_lastDir = dir;
	}

	private void LoadMapList(string dir)
	{
		if (!IsFolderArena(dir))
		{
			GD.PrintErr("Selected directory is not a valid Arena Client folder.");
			return;
		}

		GlobalData.Instance.Settings ??= new Settings();
		GlobalData.Instance.Settings.ArenaPath = dir;
		
		_contentPath = $"{GlobalData.Instance.Settings.ArenaPath}/game/contents";
		GlobalData.Instance.LoadElements($"{_contentPath}/maps/data.jar");
		GlobalData.Instance.LoadPlaylists($"{_contentPath}/maps_sounds.jar");
		
		using var dirAccess = DirAccess.Open($"{_contentPath}/maps/fight");
		if (dirAccess == null)
			return;
		
		dirAccess.ListDirBegin();
		var name = dirAccess.GetNext();
		List<string> mapNames = [];
		while (name != "")
		{
			if (dirAccess.CurrentIsDir())
				continue;
			if (!name.EndsWith(".jar"))
				continue;
			var mapName = name.Split(".")[0];
			mapNames.Add(mapName);
			GlobalData.Instance.LoadMap(mapName);
			name = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		
		_tools.SetMapOptions(mapNames);
		GlobalData.Instance.SaveSettings();
	}

	private bool IsFolderArena(string path)
	{
		using var dirAccess = DirAccess.Open(path);
		return dirAccess.DirExists("game/contents/maps/env") &&
		       dirAccess.DirExists("game/contents/maps/fight") &&
		       dirAccess.DirExists("game/contents/maps/gfx") &&
		       dirAccess.DirExists("game/contents/maps/light") &&
		       dirAccess.DirExists("game/contents/maps/tplg") &&
		       dirAccess.FileExists("game/contents/maps/data.jar");
	}
}
