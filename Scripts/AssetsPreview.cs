using Godot;
using System;

public partial class AssetsPreview : Control
{
	[Export] private GridContainer _container;
	[Export] private PackedScene _component;
	
	public override void _Ready() { }

	public void DisplayAssets(Enums.Biome biome, Enums.Category category, bool onlyInScene)
	{
		foreach (var asset in GlobalData.Instance.Assets)
		{
			if ((asset.Biome != biome && biome != Enums.Biome.Global) || 
			    (asset.Category != category && category != Enums.Category.Global))
				continue;
			var preview = _component.Instantiate<PreviewComponent>();
			preview.DisplayAsset(asset);
			_container.AddChild(preview);
		}
	}

	public void _OnAssetSelected(int index)
	{
		
	}
}
