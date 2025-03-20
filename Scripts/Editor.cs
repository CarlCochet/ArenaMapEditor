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
	[Export] private FileDialog _fileDialog;
	[Export] private Inspector _inspector;
	[Export] private Gizmo _gizmo;

	private string _mapPath;
	private string _contentPath;
	private int _x;
	private int _y;
	private int _z;
	
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

		_overlay.PreviewChangePressed += _OnPreviewChangePressed;
		_overlay.OffsetChangeDown += _OnOffsetChangeDown;
		_overlay.OffsetChangeUp += _OnOffsetChangeUp;
		_overlay.CenterPressed += _OnCenterPressed;
		_overlay.HeightChangePressed += _OnHeightChangePressed;
		_overlay.HighlightHeightPressed += _OnHighlightHeightPressed;
		
		_map.TileSelected += _OnTileSelected;
		if (GlobalData.Instance.Settings != null)
			_OnDirectorySelected(GlobalData.Instance.Settings.ArenaPath);
	}

	public override void _Process(double delta)
	{
		if (_map.SelectedTiles.Count == 0)
			return;
		
		_gizmo.Position = _map.SelectedTiles[0].GetGlobalTransformWithCanvas().Origin;
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion)
		{
			(_x, _y) = _map.PositionToCoord(GetGlobalMousePosition(), _z);
			_overlay.UpdatePosition(_x, _y, _z);
			_map.HighlightTiles(GetGlobalMousePosition());
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
		GD.Print("Selected tile: " + e.Element.CommonData.Id);
		_inspector.Update(e.Element);
		_assetsPreview.Update(e.Element);
		_tools.Update(e.Element);
		_overlay.Update(e.Element);
		_z = e.Element.CellZ;
	}

	private void _OnHighlightHeightPressed(object sender, EventArgs e)
	{
		
	}

	private void _OnHeightChangePressed(object sender, Overlay.HeightChangedEventArgs e)
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

	private void _OnCenterPressed(object sender, EventArgs e)
	{
		
	}

	private void _OnOffsetChangeUp(object sender, Overlay.OffsetChangedEventArgs e)
	{
		
	}

	private void _OnOffsetChangeDown(object sender, Overlay.OffsetChangedEventArgs e)
	{
		
	}

	private void _OnPreviewChangePressed(object sender, Overlay.PreviewChangedEventArgs e)
	{
		
	}
	
	private void _OnAssetPreviewEntered()
	{
		_map.UpdateFocus(false);
	}

	private void _OnAssetPreviewExited()
	{
		_map.UpdateFocus(true);
	}
	
	private void _OnLocateArenaPressed(object sender, EventArgs eventArgs)
	{
		_fileDialog.Visible = true;
	}
	
	private void _OnMapSelected(object sender, Tools.MapSelectedEventArgs eventArgs)
	{
		var mapData = new MapData(eventArgs.MapName);
		mapData.Load(_contentPath);
		_map.Load(mapData, _filter.Mode);
	}

	private void _OnDirectorySelected(string dir)
	{
		if (!IsFolderArena(dir))
		{
			GD.PrintErr("Invalid directory");
			return;
		}

		GlobalData.Instance.Settings ??= new Settings();
		GlobalData.Instance.Settings.ArenaPath = dir;
		
		_contentPath = $"{GlobalData.Instance.Settings.ArenaPath}/game/contents";
		using var dirAccess = DirAccess.Open($"{_contentPath}/maps/gfx");
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
			mapNames.Add(name.Split(".")[0]);
			name = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		
		_tools.SetMapOptions(mapNames);
		GlobalData.Instance.LoadElements($"{_contentPath}/maps/data.jar");
		GlobalData.Instance.LoadPlaylists($"{_contentPath}/maps.jar");
		GlobalData.Instance.SaveSettings();
	}

	private bool IsFolderArena(string path)
	{
		using var dirAccess = DirAccess.Open(path);
		return dirAccess.DirExists("game/contents/maps/coords") &&
		       dirAccess.DirExists("game/contents/maps/env") &&
		       dirAccess.DirExists("game/contents/maps/fight") &&
		       dirAccess.DirExists("game/contents/maps/gfx") &&
		       dirAccess.DirExists("game/contents/maps/light") &&
		       dirAccess.DirExists("game/contents/maps/tplg") &&
		       dirAccess.FileExists("game/contents/maps/data.jar");
	}
}
