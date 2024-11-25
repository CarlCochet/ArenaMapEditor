using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public partial class MapData : Node
{
    public int Id { get; set; }
    public TopologyData Topology { get; set; }
    public LightData Light { get; set; }
    public GfxData Gfx { get; set; }
    public FightData Fight { get; set; }
    public EnvData Env { get; set; }
    public CoordsData Coords { get; set; }
    
    public MapData(string path, int id)
    {
        Id = id;
        LoadData(path);
    }

    private void LoadData(string path)
    {
        try
        {
            LoadPartitions(path);
            LoadElements(path + "maps/data.jar");
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
        using var reader = new StreamReader(stream);
    }

    private void LoadAmbiance(string path)
    {
        
    }

    private void LoadPlaylists(string path)
    {
        
    }
}
