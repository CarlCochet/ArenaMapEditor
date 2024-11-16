using Godot;
using System;
using System.Collections.Generic;

public partial class Editor : Node2D
{
	private List<CompressedTexture2D> _assets = new();
	private RandomNumberGenerator _rng = new();
	
	public override void _Ready()
	{
		LoadAssets();
	}

	private void LoadAssets()
	{
		var basePath = "res://Assets/GFX/";
		using var dir = DirAccess.Open(basePath);
		if (dir is null)
			return;

		dir.ListDirBegin();
		var folderName = dir.GetNext();
		while (folderName != "")
		{
			if (dir.CurrentIsDir())
			{
				folderName = dir.GetNext();
				continue;
			}
			if (folderName.Contains(".import"))
			{
				folderName = dir.GetNext();
				continue;
			}
			_assets.Add(GD.Load<CompressedTexture2D>($"{basePath}{folderName}"));
			folderName = dir.GetNext();
		}
	}
}
