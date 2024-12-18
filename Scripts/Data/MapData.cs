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
    public AmbianceData Ambiances { get; set; }
    
    public MapData(string id)
    {
        Id = id;
    }

    public void Load(string path)
    {
        try
        {
            Topology = new TopologyData(Id);
            Light = new LightData(Id);
            Gfx = new GfxData(Id);
            Fight = new FightData(Id);
            Env = new EnvData(Id);
            
            Topology.Load(path);
            Light.Load(path);
            Gfx.Load(path);
            Fight.Load(path);
            Env.Load(path);
            
            LoadAmbiance(path + "maps.jar");
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.Message);
        }
    }

    public void Save(string path)
    {
        
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

    private void SavePartitions(string path)
    {
        
    }

    private void SaveAmbiance(string path)
    {
        
    }
}
