using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class LightData
{
    public int Id { get; set; }
    public List<Partition> Partitions { get; set; } = [];
    public Dictionary<long, Partition> PartitionsMap { get; set; } = new();

    public LightData(string id)
    {
        if (!int.TryParse(id, out var worldId))
            return;
        Id = worldId;
    }

    public void Load(string path)
    {
        using var archive = ZipFile.OpenRead($"{path}/{Id}.jar");
        
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.Contains('_'))
                continue;
            
            var splitName = entry.FullName.Split('_');
            if (!int.TryParse(splitName[0], out var x))
                x = 0;
            if (!int.TryParse(splitName[1], out var y))
                y = 0;
            
            using var stream = entry.Open();
            var reader = new ExtendedDataInputStream(stream);
            
            var partition = new Partition(Id);
            partition.Load(reader);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public void Save()
    {
        
    }

    public class Partition(int id)
    {
        public int Id { get; set; } = id;
        public int X { get; set; }
        public int Y { get; set; }
        public List<CellLightDef> CellLightDef { get; set; } = [];
        public CellLightDef[] layerColors;
        
        private const int ChunkSize = 18;

        public void Load(ExtendedDataInputStream reader)
        {
            X = reader.ReadShort() * ChunkSize;
            Y = reader.ReadShort() * ChunkSize;
            var count = reader.ReadShort() & 0xFFFF;
            CellLightDef.EnsureCapacity(count);

            for (var i = 0; i < count; i++)
            {
                var allowOutdoorLighting = reader.ReadBooleanBit();
                var ambiance = reader.ReadInt();
                var shadow = reader.ReadInt();
                var light = reader.ReadInt();
                var def = new CellLightDef(ambiance, shadow, light, allowOutdoorLighting);
                CellLightDef.Add(def);
            }

            var layerCount = reader.ReadByte() & 0xFF;
            count = reader.ReadShort() & 0xFFFF;
            layerColors = new CellLightDef[MapConstants.NumCells * layerCount];
            for (var i = 0; i < count; i++)
            {
                var k = reader.ReadShort() & 0xFFFF;
                var idx = reader.ReadShort() & 0xFFFF;
                layerColors[k] = CellLightDef[idx];
            }
        }

        public void Save(BinaryWriter writer)
        {
            
        }
    }

    public class CellLightDef
    {
        private const float ColorFactor = 2.0f;
        private readonly int _defaultColor = ArenaColor.Gray.Get();
        public float AmbianceLightR { get; set; }
        public float AmbianceLightG { get; set; }
        public float AmbianceLightB { get; set; }
        public float ShadowsR { get; set; }
        public float ShadowsG { get; set; }
        public float ShadowsB { get; set; }
        public float LightsR { get; set; }
        public float LightsG { get; set; }
        public float LightsB { get; set; }
        public bool AllowOutdoorLighting { get; set; }
        public bool HasShadows { get; set; }
        public float[] Merged { get; set; } = [0.0f, 0.0f, 0.0f];
        public float[] NightLight { get; set; }

        public CellLightDef(int ambianceLight, int shadows, int lights, bool allowOutdoorLighting)
        {
            AmbianceLightR = ArenaColor.GetRedFromARGB(ambianceLight) * ColorFactor;
            AmbianceLightG = ArenaColor.GetGreenFromARGB(ambianceLight) * ColorFactor;
            AmbianceLightB = ArenaColor.GetBlueFromARGB(ambianceLight) * ColorFactor;
            
            HasShadows = shadows != _defaultColor;
            ShadowsR = ArenaColor.GetRedFromARGB(shadows);
            ShadowsG = ArenaColor.GetGreenFromARGB(shadows);
            ShadowsB = ArenaColor.GetBlueFromARGB(shadows);
            
            NightLight = lights != _defaultColor ? [0.0f, 0.0f, 0.0f] : null;
            LightsR = ArenaColor.GetRedFromARGB(lights) - 0.5f;
            LightsG = ArenaColor.GetGreenFromARGB(lights) - 0.5f;
            LightsB = ArenaColor.GetBlueFromARGB(lights) - 0.5f;
            AllowOutdoorLighting = allowOutdoorLighting;
        }
    }
}
