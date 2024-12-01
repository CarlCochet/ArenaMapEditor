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
    public static Dictionary<int, ElementData> Elements { get; set; } 
    public static Dictionary<int, AmbianceData> Ambiances { get; set; }
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
            LoadElements(path + "maps/data.jar");
            LoadPartitions(path);
            // LoadAmbiance(path + "maps.jar");
            // LoadPlaylists(path + "maps.jar");
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

    private void LoadElements(string path)
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
        for (var i = 0; i < elementCount; i++)
        {
            var elementProperties = new ElementData(reader);
            Elements.TryAdd(elementProperties.Id, elementProperties);
        }
    }

    private void LoadAmbiance(string path)
    {
        using var archive = ZipFile.OpenRead(path);

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.StartsWith("maps/env"))
                continue;
            if (!entry.FullName.EndsWith("ambiences.lib"))
                continue;
            
            using var stream = entry.Open();
            using var reader = new BinaryReader(stream);

            var ambiance = new AmbianceData(reader, entry.FullName);
            if (ambiance.Id == -1)
                continue;
            
            Ambiances.TryAdd(ambiance.Id, ambiance);
        }
    }

    private void LoadPlaylists(string path)
    {
        
    }
}
