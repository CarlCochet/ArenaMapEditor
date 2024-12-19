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
            Topology.Load($"{path}/maps/tplg");
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message);
        }
        
        try
        {
            Light = new LightData(Id);
            Light.Load($"{path}/maps/light");
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message);
        }
        
        try
        {
            Gfx = new GfxData(Id);
            Gfx.Load($"{path}/maps/gfx");
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message);
        }
        
        try
        {
            Fight = new FightData(Id);
            Fight.Load($"{path}/maps/fight");
        }
        catch (Exception e)
        {
            GD.PrintErr(e.Message);
        }
        
        try
        {
            Env = new EnvData(Id);
            Env.Load($"{path}/maps/env");
        }
        catch (Exception ex)
        {
            GD.PrintErr(ex.Message);
        }

        try
        {
            LoadAmbiance($"{path}/maps.jar");
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
