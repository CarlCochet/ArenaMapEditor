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
	
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(900, 500));
		GlobalData.Instance.LoadAssets();
		_assetsPreview.DisplayAssets(Enums.Biome.Global, Enums.Category.Global, false);
		_tools.LoadMapPressed += _OnLoadMapPressed;
	}
	
	private void _OnAssetPreviewEntered()
	{
		
	}

	private void _OnAssetPreviewExited()
	{
		
	}

	private void _OnLoadMapPressed(object sender, EventArgs eventArgs)
	{
		_fileDialog.Visible = true;
	}

	private void _OnFileSelected(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		var tiles = JsonSerializer.Deserialize<MapInfo>(file.GetAsText());
	}
}
