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
	private HashSet<int> _usedAssetIds;
	private int[] _filteredAssetIds = [];
	
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
		if (_pageSize == newPageSize || newPageSize <= 0)
			return;

		var firstVisibleIndex = _currentPage * _pageSize;
		_pageSize = newPageSize;
		_currentPage = firstVisibleIndex / _pageSize;
		
		DisplayAssets(_biome, _category, _mode, _usedAssetIds);
	}

	public void DisplayAssets(Enums.Biome biome, Enums.Category category, Enums.Mode mode,
		IReadOnlySet<int> usedAssetIds = null)
	{
		var usedFilterChanged = usedAssetIds == null != (_usedAssetIds == null) ||
		                        usedAssetIds != null && (_usedAssetIds == null || !_usedAssetIds.SetEquals(usedAssetIds));
		if (_biome != biome || _category != category || _mode != mode || usedFilterChanged)
			_currentPage = 0;

		_usedAssetIds = usedAssetIds == null ? null : [..usedAssetIds];
		_filteredAssetIds = GlobalData.Instance.AssetIds
			.Where(id => GlobalData.Instance.ValidAssets.TryGetValue(id, out var asset) &&
			             (biome == Enums.Biome.Global || asset.Biome == biome) &&
			             (category == Enums.Category.Global || asset.Category == category) &&
			             (_usedAssetIds == null || _usedAssetIds.Contains(id)))
			.ToArray();
		_totalElements = _filteredAssetIds.Length;
		var pageCount = Math.Max(1, Mathf.CeilToInt((float)_totalElements / _pageSize));
		_currentPage = Math.Clamp(_currentPage, 0, pageCount - 1);

		_pageSpin.Value = _currentPage + 1;
		_pageSpin.Suffix = $"/{pageCount}";
		_pageSpin.MaxValue = pageCount;
		
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
			
			var id = _filteredAssetIds[index];
			if (!GlobalData.Instance.ValidAssets.TryGetValue(id, out var asset)) continue;

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
		DisplayAssets(_biome, _category, _mode, _usedAssetIds);
	}

	private void _OnNextPressed()
	{
		var lastPage = Math.Max(0, Mathf.CeilToInt((float)_totalElements / _pageSize) - 1);
		_currentPage = Math.Min(_currentPage + 1, lastPage);
		DisplayAssets(_biome, _category, _mode, _usedAssetIds);
	}

	private void _OnCurrentSubmitted(double newPage)
	{
		_currentPage = (int)newPage - 1;
		DisplayAssets(_biome, _category, _mode, _usedAssetIds);
	}

	public class AssetSelectedEventArgs(ElementData element) : EventArgs
	{
		public ElementData Element => element;
	}
}
