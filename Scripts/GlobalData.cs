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
    public Dictionary<int, TileData> ValidAssets { get; private set; } = new();
    public int[] AssetIds { get; private set; }
    public RandomNumberGenerator Rng { get; private set; } = new();
    public Settings Settings { get; set; }

    public Dictionary<int, ElementData> Elements { get; set; } = new();
    public Dictionary<short, PlaylistData> Playlists { get; set; } = new();
    public Dictionary<string, MapData> Maps { get; set; } = new();
    public List<Texture2D> BonusTextures { get; set; } = [];
    public List<Texture2D> PlacementTextures { get; set; } = [];
    
    public int[] IgnoreGfxIds { get; set; } = [76, 113, 114, 141, 252, 253, 331, 332, 333, 461, 462, 463, 504, 610, 613,
        730, 731, 907, 908, 909, 910, 927, 933, 991, 993, 1048, 1194, 1209, 1210, 1211, 1230, 1236, 1237, 1238, 1246, 
        1253, 1257, 1330, 1332, 1342, 1348, 1377, 1378, 1414, 1499, 1505, 1508, 1509, 1510, 1515, 1537, 1558, 1583, 
        1584, 1585, 1586, 1600, 1602, 1621, 1628, 1646, 1655, 1662, 1663, 1665, 1672, 1678, 1755, 1781, 2017, 2037, 
        2038, 2039, 2043, 2044, 2045, 2046, 2047, 2048, 2049, 2050, 2051, 2114, 2273, 2274, 2349, 2376, 2377, 2382, 
        2385, 2386, 2439, 2443, 2446, 2447, 2450, 2451, 2452, 2487, 2488, 2489, 2490, 2560, 2587, 2661, 2667, 2671, 
        2672, 2737, 2783, 2785, 2786, 2787, 2788, 2839, 2840, 2841, 2842, 2843, 2850, 2853, 2854, 2855, 2856, 2857, 
        2858, 2964, 2966, 2969, 2970, 3007, 3008, 3009, 3010, 3061, 3062, 3063, 3064, 3065, 3074, 3179, 3186, 3345, 
        3346, 3347, 3382, 3484, 3485, 3486, 3487, 3602, 3603, 3670, 3671, 3672, 3673, 3674, 3682, 3690, 3699, 3702, 
        3704, 3839, 3929, 3945, 3949];

    public const int CellWidth = 86;
    public const int CellHeight = 43;
    public const int ElevationStep = 10;
    
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

    public void LoadMap(string mapName)
    {
        Maps[mapName] = new MapData(mapName);
        Maps[mapName].Load($"{Settings.ArenaPath}/game/contents");
    }
 
    public void LoadAssets()
    {
        using var file = FileAccess.Open("res://Assets/metadata.json", FileAccess.ModeFlags.Read);
        Assets = JsonSerializer.Deserialize<List<TileData>>(file.GetAsText());
        foreach (var asset in Assets)
        {
            asset.LoadTexture();
            if (asset.IsValid)
                ValidAssets.Add(asset.Id, asset);
        }
        AssetIds = ValidAssets.Keys.ToArray();

        for (var i = 0; i < 8; i++)
        {
            BonusTextures.Add(GD.Load<CompressedTexture2D>($"res://Assets/Bonus/{i}.tgam.png"));
        }

        for (var i = 0; i < 3; i++)
        {
            PlacementTextures.Add(GD.Load<CompressedTexture2D>($"res://Assets/Placement/{i}.tgam.png"));
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
        var reader = new ExtendedDataInputStream(stream);
        
        var elementCount = reader.ReadInt();
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
        var entry = archive.GetEntry("maps_sounds/env/playlists.dat");
        
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

    public void SaveSettings()
    {
		using var settingsFile = FileAccess.Open("user://settings.json", FileAccess.ModeFlags.Write);
        
        settingsFile.StoreString(JsonSerializer.Serialize(Settings));
    }
	
    public void LoadSettings()
    {
        using var settingsFile = FileAccess.Open("user://settings.json", FileAccess.ModeFlags.Read);
        if (settingsFile == null)
            return;
		
		Settings = JsonSerializer.Deserialize<Settings>(settingsFile.GetAsText());
    }
}
