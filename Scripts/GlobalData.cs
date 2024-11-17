using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class GlobalData
{
    private static GlobalData _instance;
    private static readonly object Lock = new();

    public List<TileData> Assets { get; private set; } = new();
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
