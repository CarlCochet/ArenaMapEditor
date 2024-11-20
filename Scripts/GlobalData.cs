using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;

public class GlobalData
{
    private static GlobalData _instance;
    private static readonly Lock Lock = new();
    
    public int SelectedTool;
    public int BrushSize = 1;
    public List<int> SelectedTiles = [];
    public List<TileData> Assets { get; private set; } = [];
    public RandomNumberGenerator Rng { get; private set; } = new();
    
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
