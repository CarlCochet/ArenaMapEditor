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
		GlobalData.Instance.LoadAssets();
		_assetsPreview.DisplayAssets(Enums.Biome.Global, Enums.Category.Global, false);
	}
	
	
}
