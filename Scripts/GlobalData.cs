using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using FileAccess = Godot.FileAccess;

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

    public Dictionary<int, ElementData> Elements { get; set; } = new();
    public Dictionary<short, PlaylistData> Playlists { get; set; } = new();
    
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
    
    public void LoadElements(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("elements.lib");

        if (entry == null)
        {
            GD.PrintErr("Can't find elements.lib");
            return;
        }
        
        using var stream = entry.Open();
        using var reader = new BinaryReader(stream);
        
        var elementCount = reader.ReadInt32();
        Elements.EnsureCapacity(elementCount);
        
        for (var i = 0; i < elementCount; i++)
        {
            var elementProperties = new ElementData();
            elementProperties.Load(reader);
            Elements.TryAdd(elementProperties.Id, elementProperties);
        }
    }

    public void LoadPlaylists(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry("maps/env/playlists.dat");
        
        if (entry == null)
        {
            GD.PrintErr("Can't find playlists.dat");
            return;
        }
        
        using var stream = entry.Open();
        using var reader = new BinaryReader(stream);

        var playlistCount = reader.ReadInt16();
        Playlists.EnsureCapacity(playlistCount);

        for (var i = 0; i < playlistCount; i++)
        {
            var playlistData = new PlaylistData();
            playlistData.Load(reader);
            Playlists.TryAdd(playlistData.Id, playlistData);
        }
    }

    public TileData GetAssetById(int gfxId)
    {
        return Assets.FirstOrDefault(a => a.Id == gfxId);
    }
}
