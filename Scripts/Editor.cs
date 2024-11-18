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
	
	public override void _Ready()
	{
		DisplayServer.WindowSetMinSize(new Vector2I(1200, 600));
		GlobalData.Instance.LoadAssets();
		_assetsPreview.DisplayAssets(Enums.Biome.Global, Enums.Category.Global, false);
	}
	
	private void _OnAssetPreviewEntered()
	{
		
	}

	private void _OnAssetPreviewExited()
	{
		
	}
}
