using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public partial class Editor : Node2D
{
	[Export] private Map _map;
	[Export] private Filter _filter;
	[Export] private AssetsPreview _assetsPreview;
	[Export] private Tools _tools;
	[Export] private Overlay _overlay;
	[Export] private FileDialog _fileDialog;

	private string _path;
	
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(1400, 500));
		GlobalData.Instance.LoadAssets();
		_assetsPreview.DisplayAssets(Enums.Biome.Global, Enums.Category.Global, false);
		_tools.MapSelected += _OnMapSelected;
		_tools.LocateArenaPressed += _OnLocateArenaPressed;
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
		var mapData = new MapData(_path, eventArgs.MapName);
		_map.LoadMap(mapData);
	}

	private void _OnDirectorySelected(string dir)
	{
		if (!IsFolderArena(dir))
		{
			GD.PrintErr("Invalid directory");
			return;
		}
		
		_path = $"{dir}/game/contents/maps";
		using var dirAccess = DirAccess.Open($"{_path}/gfx");
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
	}
	
	private void _OnMapSelectedJson(object sender, Tools.MapSelectedEventArgs eventArgs)
	{
		using var file = FileAccess.Open($"{_path}/{eventArgs.MapName}.json", FileAccess.ModeFlags.Read);
		var text = file.GetAsText();
		var mapInfo = JsonSerializer.Deserialize<MapInfo>(text);
		_map.LoadMap(mapInfo);
	}

	private void _OnDirectorySelectedJson(string dir)
	{
		_path = dir;
		using var dirAccess = DirAccess.Open(_path);
		if (dirAccess == null)
			return;
		
		dirAccess.ListDirBegin();
		var name = dirAccess.GetNext();
		List<string> mapNames = [];
		while (name != "")
		{
			if (dirAccess.CurrentIsDir())
				continue;
			if (!name.EndsWith(".json"))
				continue;
			mapNames.Add(name.Split(".")[0]);
			name = dirAccess.GetNext();
		}
		dirAccess.ListDirEnd();
		
		_tools.SetMapOptions(mapNames);
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
