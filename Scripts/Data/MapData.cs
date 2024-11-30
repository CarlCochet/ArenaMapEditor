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
    public static Dictionary<int, ElementProperties> Elements { get; set; } 
    
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
            var elementProperties = new ElementProperties(reader);
            Elements.TryAdd(elementProperties.Id, elementProperties);
        }
    }

    private void LoadAmbiance(string path)
    {
        
    }

    private void LoadPlaylists(string path)
    {
        
    }
    
    public class ElementProperties
    {
        public int Id { get; private set; }
        public AnimationData AnimationData { get; set; }
        public short OriginX { get; set; }
        public short OriginY { get; set; }
        public short ImgWidth { get; set; }
        public short ImgHeight { get; set; }
        public int GfxId { get; set; }
        public byte VisualHeight { get; set; }
        public byte VisibilityMask { get; set; }
        public byte ExportMask { get; set; }
        public byte Shader { get; set; }
        public byte PropertiesFlag { get; set; }
        public byte GroundSoundType { get; set; }
        public byte Slope { get; set; }
        public bool MoveTop { get; set; }
        public bool Walkable { get; set; }
        public bool Animated { get; set; }
        public bool BeforeMobile { get; set; }
        public bool Flip { get; set; }

        public ElementProperties(BinaryReader reader)
        {
            Id = reader.ReadInt32();
            OriginX = reader.ReadInt16();
            OriginY = reader.ReadInt16();
            ImgWidth = reader.ReadInt16();
            ImgHeight = reader.ReadInt16();
            GfxId = reader.ReadInt32();
            
            PropertiesFlag = reader.ReadByte();
            
            VisualHeight = reader.ReadByte();
            VisibilityMask = reader.ReadByte();
            ExportMask = reader.ReadByte();
        }
    }

    public class AnimationData
    {
        public int Duration { get; set; }
        public short[] Frames { get; set; }
    }
}
