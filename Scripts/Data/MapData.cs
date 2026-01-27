using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class MapData
{
    public string Id { get; set; }
    public TopologyData Topology { get; set; }
    public LightData Light { get; set; }
    public GfxData Gfx { get; set; }
    public FightData Fight { get; set; }
    public EnvData Env { get; set; }
    public AmbianceData Ambiances { get; set; }
    public List<(int x, int y)> ValidPositions { get; set; }
    
    
    
    public MapData(string id)
    {
        Id = id;
    }

    public void CreateEmpty()
    {
        Topology = new TopologyData(Id);
        Light = new LightData(Id);
        Gfx = new GfxData(Id);
        Fight = new FightData(Id);
        Env = new EnvData(Id);
        Ambiances = new AmbianceData(Id);
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
            GD.PrintErr($"Error loading TOPOLOGY for map {Id}: {e.Message}");
        }
        
        try
        {
            Light = new LightData(Id);
            Light.Load($"{path}/maps/light");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading LIGHT for map {Id}: {e.Message}");
        }
        
        try
        {
            Gfx = new GfxData(Id);
            Gfx.Load($"{path}/maps/gfx");
            ValidPositions = Gfx.Partitions
                .SelectMany(partition => partition.Elements)
                .Select(element => (element.CellX, element.CellY))
                .Distinct()
                .OrderBy(pos => pos.CellX)
                .ThenBy(pos => pos.CellY)
                .ToList();
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading GFX for map {Id}: {e.Message}");
        }
        
        try
        {
            Fight = new FightData(Id);
            Fight.Load($"{path}/maps/fight");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading FIGHT for map {Id}: {e.Message}");
        }
        
        try
        {
            Env = new EnvData(Id);
            Env.Load($"{path}/maps/env");
        }
        catch (Exception e)
        {
            GD.PrintErr($"Error loading ENV for map {Id}: {e.Message}");
        }

        // try
        // {
        //     LoadAmbiance($"{path}/maps_sounds");
        // }
        // catch (Exception e)
        // {
        //     GD.PrintErr($"Error loading AMBIANCE for map {Id}: {e.Message}");
        // }
    }

    public void Save(string path)
    {
        var dirAccess = DirAccess.Open(path);
        dirAccess.MakeDirRecursive($"maps/tplg/{Id}");
        dirAccess.MakeDirRecursive($"maps/light/{Id}");
        dirAccess.MakeDirRecursive($"maps/gfx/{Id}");
        dirAccess.MakeDirRecursive($"maps/fight/{Id}");
        dirAccess.MakeDirRecursive($"maps/env/{Id}");
        
        Topology.Save($"{path}/maps/tplg/{Id}");
        Light.Save($"{path}/maps/light/{Id}");
        Gfx.Save($"{path}/maps/gfx/{Id}");
        Fight.Save($"{path}/maps/fight/{Id}");
        Env.Save($"{path}/maps/env/{Id}");
        
        Topology.SaveJson($"{path}/json");
        // SaveAmbiance($"{path}/maps_sounds");
    }

    private void LoadAmbiance(string path)
    {
        var reader = GlobalData.Instance.GetReader(path, $"maps_sounds/env/{Id}/ambiences.lib");
        
        if (reader == null)
        {
            GD.PrintErr("ambiences.lib not found.");
            return; 
        }
        
        Ambiances = new AmbianceData(Id);
        Ambiances.Load(reader);
    }

    private void SaveAmbiance(string path)
    {
        if (Ambiances == null) 
            return;
        using var writer = GlobalData.Instance.GetWriter(path, $"maps/env/{Id}", "ambiences.lib");
        Ambiances.Save(writer);
    }
}
