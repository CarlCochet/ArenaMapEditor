using Godot;
using System;
using System.Collections.Generic;

public partial class MultiMeshMapRenderer : MultiMeshInstance2D
{
	private readonly List<(long hashCode, GfxData.Element element)> _elementList = [];
	private readonly Dictionary<long, int> _hashToIndex = new();
	private MultiMesh _multiMesh;
	private int _selectedIndex = -1;
	private long _selectedHashCode;
	private static readonly Color SelectedColor = Colors.Green;
	private const float DimFactor = 0.25f;
	private bool _isHeightHighlightActive;
	private int _highlightZ;

	public IReadOnlyList<GfxData.Element> Elements
	{
		get
		{
			var result = new List<GfxData.Element>(_elementList.Count);
			foreach (var (_, element) in _elementList)
				result.Add(element);
			return result;
		}
	}

	public int Count => _elementList.Count;

	public void Setup(ShaderMaterial material)
	{
		var quad = new QuadMesh();
		quad.Size = new Vector2(1, 1);

		_multiMesh = new MultiMesh();
		_multiMesh.Mesh = quad;
		_multiMesh.TransformFormat = MultiMesh.TransformFormatEnum.Transform2D;
		_multiMesh.UseCustomData = true;
		_multiMesh.UseColors = true;
		Multimesh = _multiMesh;
		Material = material;
	}

	public void LoadElements(List<GfxData.Element> sortedElements)
	{
		_selectedIndex = -1;
		_elementList.Clear();
		_hashToIndex.Clear();
		foreach (var el in sortedElements)
		{
			_elementList.Add((el.HashCode, el));
			_hashToIndex[el.HashCode] = _elementList.Count - 1;
		}

		RebuildAllInstances();
	}

	public void AddElement(GfxData.Element element)
	{
		var insertIdx = FindInsertIndex(element.HashCode);
		_elementList.Insert(insertIdx, (element.HashCode, element));
		_hashToIndex[element.HashCode] = insertIdx;
		FixIndicesFrom(insertIdx + 1);
		RebuildAllInstances();
	}

	public void RemoveElement(GfxData.Element element)
	{
		if (!_hashToIndex.TryGetValue(element.HashCode, out var idx))
			return;

		if (_selectedHashCode == element.HashCode)
		{
			_selectedIndex = -1;
			_selectedHashCode = 0;
		}

		_elementList.RemoveAt(idx);
		_hashToIndex.Remove(element.HashCode);
		FixIndicesFrom(idx);
		RebuildAllInstances();
	}

	public bool ElementExists(GfxData.Element element)
	{
		return _hashToIndex.ContainsKey(element.HashCode);
	}

	public GfxData.Element GetTopElementAt(Vector2 position, int[] ignoreIds)
	{
		for (var i = _elementList.Count - 1; i >= 0; i--)
		{
			var (_, element) = _elementList[i];
			if (Array.IndexOf(ignoreIds, element.CommonData.GfxId) >= ignoreIds.GetLowerBound(0))
				continue;
			if (!IsValidPixel(element, position))
				continue;
			return element;
		}
		return null;
	}

	public Vector2 GetSelectedTileCenter()
	{
		if (_selectedIndex < 0 || _selectedIndex >= _elementList.Count)
			return Vector2.Zero;
		var transform = _multiMesh.GetInstanceTransform2D(_selectedIndex);
		return transform.Origin;
	}

	public long GetSelectedHashCode() => _selectedHashCode;

	public void SelectElement(long hashCode)
	{
		if (!_hashToIndex.TryGetValue(hashCode, out var idx))
			return;
		_selectedIndex = idx;
		_selectedHashCode = hashCode;

		var (_, element) = _elementList[idx];
		var vSpan = GetVSpan(element);
		_multiMesh.SetInstanceColor(idx, new Color(SelectedColor.R, SelectedColor.G, SelectedColor.B, vSpan));

		var material = (ShaderMaterial)Material;
		material.SetShaderParameter("highlight_alpha", 1.0f);
	}

	public void DeselectAll()
	{
		if (_selectedIndex >= 0)
		{
			var (_, element) = _elementList[_selectedIndex];
			_multiMesh.SetInstanceColor(_selectedIndex, ComputeInstanceColor(element));
		}
		_selectedIndex = -1;
		_selectedHashCode = 0;
	}

	public void SetHeightHighlight(int z)
	{
		_isHeightHighlightActive = true;
		_highlightZ = z;
		UpdateAllInstanceColors();
	}

	public void ClearHeightHighlight()
	{
		_isHeightHighlightActive = false;
		UpdateAllInstanceColors();
	}

	private void UpdateAllInstanceColors()
	{
		for (var i = 0; i < _elementList.Count; i++)
		{
			if (i == _selectedIndex)
				continue;

			var (_, element) = _elementList[i];
			_multiMesh.SetInstanceColor(i, ComputeInstanceColor(element));
		}
	}

	private Color ComputeInstanceColor(GfxData.Element element)
	{
		var tint = ComputeBaseColor(element);
		var vSpan = GetVSpan(element);

		if (_isHeightHighlightActive && element.CellZ != _highlightZ)
			return new Color(tint.R * DimFactor, tint.G * DimFactor, tint.B * DimFactor, vSpan);

		return new Color(tint.R, tint.G, tint.B, vSpan);
	}

	private void RebuildAllInstances()
	{
		_multiMesh.InstanceCount = _elementList.Count;
		for (var i = 0; i < _elementList.Count; i++)
		{
			var (_, element) = _elementList[i];
			SetInstanceFromElement(i, element);
		}
	}

	private void SetInstanceFromElement(int idx, GfxData.Element element)
	{
		if (!GlobalData.Instance.AtlasEntries.TryGetValue(element.CommonData.GfxId, out var entry))
			return;

		var x = element.CellX;
		var y = element.CellY;
		var z = element.CellZ;
		var height = element.Height;
		var originX = element.CommonData.OriginX;
		var originY = element.CommonData.OriginY;

		var screenX = (x - y) * GlobalData.CellWidth * 0.5f;
		var screenY = (x + y) * GlobalData.CellHeight * 0.5f - (z - height) * GlobalData.ElevationStep;

		var centerX = screenX - originX + entry.Width * 0.5f;
		var centerY = screenY - originY + entry.Height * 0.5f;

		var flip = element.CommonData.Flip;
		var scaleX = flip ? -entry.Width : entry.Width;

		var transform = new Transform2D(0, new Vector2(scaleX, entry.Height), 0, new Vector2(centerX, centerY));
		_multiMesh.SetInstanceTransform2D(idx, transform);

		var customData = new Color(entry.AtlasIndex, entry.UMin, entry.VMin, entry.UMax - entry.UMin);
		_multiMesh.SetInstanceCustomData(idx, customData);

		_multiMesh.SetInstanceColor(idx, ComputeInstanceColor(element));
	}

	private static float GetVSpan(GfxData.Element element)
	{
		if (GlobalData.Instance.AtlasEntries.TryGetValue(element.CommonData.GfxId, out var entry))
			return entry.VMax - entry.VMin;
		return 0f;
	}

	private static Color ComputeBaseColor(GfxData.Element element)
	{
		return new Color(
			0.5f + 0.5f * element.Color.R,
			0.5f + 0.5f * element.Color.G,
			0.5f + 0.5f * element.Color.B);
	}

	private static bool IsValidPixel(GfxData.Element element, Vector2 position)
	{
		if (!GlobalData.Instance.AtlasEntries.TryGetValue(element.CommonData.GfxId, out var entry))
			return false;
		if (!GlobalData.Instance.ValidAssets.TryGetValue(element.CommonData.GfxId, out var asset))
			return false;

		var x = element.CellX;
		var y = element.CellY;
		var z = element.CellZ;
		var height = element.Height;
		var originX = element.CommonData.OriginX;
		var originY = element.CommonData.OriginY;

		var screenX = (x - y) * GlobalData.CellWidth * 0.5f;
		var screenY = (x + y) * GlobalData.CellHeight * 0.5f - (z - height) * GlobalData.ElevationStep;

		var left = screenX - originX;
		var top = screenY - originY;
		var right = left + entry.Width;
		var bottom = top + entry.Height;

		if (position.X < left || position.X > right || position.Y < top || position.Y > bottom)
			return false;

		var localX = (int)(position.X - left);
		var localY = (int)(position.Y - top);

		if (element.CommonData.Flip)
			localX = entry.Width - localX;

		if (localX < 0 || localX >= entry.Width || localY < 0 || localY >= entry.Height)
			return false;

		return asset.GetImage().GetPixel(localX, localY).A > 0.1f;
	}

	private int FindInsertIndex(long hashCode)
	{
		var lo = 0;
		var hi = _elementList.Count;
		while (lo < hi)
		{
			var mid = (lo + hi) / 2;
			if (_elementList[mid].hashCode < hashCode)
				lo = mid + 1;
			else
				hi = mid;
		}
		return lo;
	}

	private void FixIndicesFrom(int start)
	{
		for (var i = start; i < _elementList.Count; i++)
		{
			var (hashCode, _) = _elementList[i];
			_hashToIndex[hashCode] = i;
		}
	}
}
