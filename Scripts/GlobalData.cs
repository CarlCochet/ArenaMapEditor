using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

public class GlobalData
{
    public Enums.Tool SelectedTool { get; set; } = Enums.Tool.Select;
    public Enums.Biome SelectedBiome { get; set; } = Enums.Biome.Global;
    public Enums.Category SelectedCategory { get; set; } = Enums.Category.Global;
    public Enums.Mode SelectedMode { get; set; } = Enums.Mode.Gfx;
    public int BrushSize { get; set; } = 1;
    public bool Erasing { get; set; } = false;
    public Color SelectedColor { get; set; } = Colors.White;
    public List<int> SelectedTiles = [];
    public List<TileData> Assets { get; private set; } = [];
    public RandomNumberGenerator Rng { get; private set; } = new();
    
    private static GlobalData _instance;
    private static readonly Lock Lock = new();
    
    public static GlobalData Instance
    {
        get
        {
            if (_instance != null) 
                return _instance;
            
            lock (Lock)
            {
                _instance ??= new GlobalData();
            }
            return _instance;
        }
    }

    public void LoadAssets()
    {
        using var file = FileAccess.Open("res://Assets/metadata.json", FileAccess.ModeFlags.Read);
        Assets = JsonSerializer.Deserialize<List<TileData>>(file.GetAsText());
        foreach (var asset in Assets)
        {
            asset.LoadTexture();
        }
    }
}
