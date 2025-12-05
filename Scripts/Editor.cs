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

		_filter.FilterUpdated += _OnFilterUpdated;
		_filter.ModeUpdated += _OnModeUpdated;
		
		_tools.MapSelected += _OnMapSelected;
		_tools.LocateArenaPressed += _OnLocateArenaPressed;
		_tools.ExportMapPressed += _OnMapExportedPressed;
		_tools.ToolSelected += _OnToolSelected;

		_overlay.PreviewChanged += _OnPreviewChanged;
		_overlay.HeightChanged += _OnHeightChanged;
		_overlay.HighlightHeightToggled += OnHighlightHeightToggled;
		_overlay.GenerateTopologyPressed += _OnGenerateTopologyPressed;
		
		_assetsPreview.MouseEntered += _OnAssetPreviewEntered;
		_assetsPreview.MouseExited += _OnAssetPreviewExited;
		_assetsPreview.AssetSelected += _OnAssetSelected;

		_inspector.ElementUpdated += _OnElementUpdated;
		_inspector.TopologyUpdated += _OnTopologyUpdated;
		_inspector.MouseEntered += _OnAssetPreviewEntered;
		_inspector.MouseExited += _OnAssetPreviewExited;
		
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

	private void _OnFilterUpdated(object sender, EventArgs e)
	{
		_assetsPreview.DisplayAssets(_filter.Biome, _filter.Category, _filter.Mode);
	}
	
	private void _OnModeUpdated(object sender, EventArgs e)
	{
		_map.UpdateDisplay(_filter.Mode);
	}
	
	private void _OnTileSelected(object sender, Map.TileSelectedEventArgs e)
	{
		GD.Print($"Selected tile: {e.Element.CommonData.Id}");
		
		if (e.PathData != null)
		{
			var data = e.PathData;
			GD.Print("Path data:");
			GD.Print($"({data.X}, {data.Y}, {data.Z}) | CanMoveThrough: {data.CanMoveThrough} | Cost: {data.Cost} | Height: {data.Height} | MurFinInfo: {data.MurFinInfo} | MiscProperties: {data.MiscProperties}");
		}
		
		if (e.VisibilityData != null)
		{
			var data = e.VisibilityData;
			GD.Print("Visibility data:");
			GD.Print($"({data.X}, {data.Y}, {data.Z}) | CanViewThrough: {data.CanViewThrough} | Height: {data.Height}");
		}
		
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
	
	private void _OnElementUpdated(object sender, Inspector.ElementUpdatedEventArgs e)
	{
		_map.UpdateElement(e.OldElement, e.NewElement);
	}

	private void _OnTopologyUpdated(object sender, Inspector.TopologyUpdatedEventArgs e)
	{
		_map.UpdateTopologyCell(e.Path, e.Visibility);
	}
	
	private void _OnAssetPreviewEntered()
	{
		_map.UpdateFocus(false);
	}

	private void _OnAssetPreviewExited()
	{
		_map.UpdateFocus(true);
	}

	private void _OnAssetSelected(object sender, AssetsPreview.AssetSelectedEventArgs e)
	{
		_map.UpdatePreview(e.Element);
	}
	
	private void _OnLocateArenaPressed(object sender, EventArgs eventArgs)
	{
		_openDialog.Visible = true;
	}
	
	private void _OnMapExportedPressed(object sender, EventArgs e)
	{
		_saveDialog.Visible = true;
	}

	private void _OnToolSelected(object sender, Tools.ToolSelectedEventArgs eventArgs)
	{
		_map.ShowPlacementPreview(eventArgs.Tool != Enums.Tool.Select && eventArgs.Tool != Enums.Tool.Erase);
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
