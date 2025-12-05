using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class AssetsPreview : Control
{
	[Export] private GridContainer _container;
	[Export] private PackedScene _component;
	[Export] private SpinBox _pageSpin;
	
	private List<PreviewComponent> _components = [];
	private int _currentPage;
	private int _totalElements;
	private Enums.Biome _biome;
	private Enums.Category _category;
	private Enums.Mode _mode;
	private int _pageSize;
	
	private const int HeaderHeight = 180;
	private const int AssetSize = 50;
	private const int ColumnCount = 6;
	
	public event EventHandler<AssetSelectedEventArgs> AssetSelected; 

	public override void _Ready()
	{
		GetTree().GetRoot().SizeChanged += OnSizeChanged;
		_pageSize = (GetWindow().Size.Y - HeaderHeight) / AssetSize * ColumnCount;
		_pageSpin.ValueChanged += _OnCurrentSubmitted;
	}

	private void OnSizeChanged()
	{
		var newPageSize = (GetWindow().Size.Y - HeaderHeight) / AssetSize * ColumnCount;
		if (_pageSize == newPageSize)
			return;
		
		var currentPageCount = _totalElements / _pageSize;
		var newPageCount = _totalElements / newPageSize;
		var ratio = (float) currentPageCount / newPageCount;
		
		_pageSize = newPageSize;
		_currentPage = (int) Math.Floor(_currentPage / ratio);
		_currentPage = Math.Max(0, Math.Min(_currentPage, _totalElements / _pageSize));
		
		DisplayAssets(_biome, _category, _mode);
	}

	public void DisplayAssets(Enums.Biome biome, Enums.Category category, Enums.Mode mode)
	{
		if (_biome != biome || _category != category || _mode != mode)
			_currentPage = 0;
		
		_totalElements = GlobalData.Instance.AssetIds.Length;

		_pageSpin.Value = _currentPage;
		_pageSpin.Suffix = $"/{_totalElements / _pageSize}";
		_pageSpin.MaxValue = (double)_totalElements / _pageSize;
		
		_biome = biome;
		_category = category;
		_mode = mode;

		foreach (var component in _components)
		{
			component.QueueFree();
		}
		_components.Clear();
		
		for (var index = _currentPage * _pageSize; index < (_currentPage + 1) * _pageSize; index++)
		{
			if (index >= _totalElements)
				break;
			
			var id = GlobalData.Instance.AssetIds[index];
			if (!GlobalData.Instance.ValidAssets.TryGetValue(id, out var asset)) continue;
			if ((asset.Biome != biome && biome != Enums.Biome.Global) || (asset.Category != category && category != Enums.Category.Global)) continue;

			var preview = _component.Instantiate<PreviewComponent>();
			preview.InitAsset(index, asset);
			_container.AddChild(preview);
			_components.Add(preview);
			preview.Pressed += _OnAssetSelected;
		}
	}

	public void Update(GfxData.Element element)
	{
		foreach (var component in _components)
		{
			component.Select(component.GfxId == element.CommonData.GfxId);
		}
	}

	private void _OnAssetSelected(object sender, PreviewComponent.PressedEventArgs eventArgs)
	{
		foreach (var component in _components)
		{
			component.Select(component.GfxId == eventArgs.GfxId);
		}

		var element = GlobalData.Instance.Elements.Values.FirstOrDefault(e => e.GfxId == eventArgs.GfxId && !e.Flip) ??
		              GlobalData.Instance.Elements.Values.FirstOrDefault(e => e.GfxId == eventArgs.GfxId);
		if (element == null) 
			return;

		AssetSelected?.Invoke(this, new AssetSelectedEventArgs(element));
	}

	private void _OnPreviousPressed()
	{
		_currentPage = Math.Max(0, _currentPage - 1);
		DisplayAssets(_biome, _category, _mode);
	}

	private void _OnNextPressed()
	{
		_currentPage = Math.Min(_currentPage + 1, _totalElements / _pageSize);
		DisplayAssets(_biome, _category, _mode);
	}

	private void _OnCurrentSubmitted(double newPage)
	{
		_currentPage = (int)newPage;
		DisplayAssets(_biome, _category, _mode);
	}

	public class AssetSelectedEventArgs(ElementData element) : EventArgs
	{
		public ElementData Element => element;
	}
}
