using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

public class MapData
{
    public string Id { get; set; }
    public TopologyData Topology { get; set; }
    public LightData Light { get; set; }
    public GfxData Gfx { get; set; }
    public FightData Fight { get; set; }
    public EnvData Env { get; set; }
    public CoordsData Coords { get; set; }
    
    public static AmbianceData Ambiances { get; set; }
    public static Dictionary<int, PlaylistData> Playlists { get; set; }
    
    public MapData(string path, string id)
    {
        Id = id;
        LoadData(path);
    }

    private void LoadData(string path)
    {
        try
        {
            LoadPartitions(path);
            LoadAmbiance(path + "maps.jar");
            LoadPlaylists(path + "maps.jar");
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.Message);
        }
    }

    private void LoadPartitions(string path)
    {
        Topology = new TopologyData(path, Id);
        Light = new LightData(path, Id);
        Gfx = new GfxData(path, Id);
        Fight = new FightData(path, Id);
        Env = new EnvData(path, Id);
        Coords = new CoordsData(path, Id);
    }

    private void LoadAmbiance(string path)
    {
        using var archive = ZipFile.OpenRead(path);
        var entry = archive.GetEntry($"maps/env/{Id}/ambiences.lib");
        
        if (entry == null)
            return;
        
        using var stream = entry.Open();
        using var reader = new BinaryReader(stream);
        
        Ambiances = new AmbianceData(entry.FullName);
        Ambiances.Load(reader);
    }

    private void LoadPlaylists(string path)
    {
        
    }
}
