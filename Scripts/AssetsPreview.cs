using Godot;
using System;
using System.Collections.Generic;

public partial class AssetsPreview : Control
{
	[Export] private GridContainer _container;
	[Export] private PackedScene _component;
	private List<PreviewComponent> _components = [];
	
	public override void _Ready() { }

	public void DisplayAssets(Enums.Biome biome, Enums.Category category, bool onlyInScene)
	{
		for (var index = 0; index < GlobalData.Instance.Assets.Count; index++)
		{
			var asset = GlobalData.Instance.Assets[index];
			if ((asset.Biome != biome && biome != Enums.Biome.Global) ||
			    (asset.Category != category && category != Enums.Category.Global))
				continue;
			var preview = _component.Instantiate<PreviewComponent>();
			preview.InitAsset(index, asset);
			_container.AddChild(preview);
			_components.Add(preview);

			preview.Pressed += _OnAssetSelected;
		}
	}

	private void _OnAssetSelected(object sender, PreviewComponent.PressedEventArgs eventArgs)
	{
		foreach (var component in _components)
		{
			component.Select(component.Index == eventArgs.Index);
		}
	}
}
