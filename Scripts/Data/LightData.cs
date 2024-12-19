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
            using var reader = new BinaryReader(stream);
            
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
        public List<CellLight> CellLights { get; set; } = [];
        public Dictionary<long, CellLight> CellLightsById { get; set; } = new();
        
        private const int ChunkSize = 18;

        public void Load(BinaryReader reader)
        {
            X = reader.ReadInt16() * ChunkSize;
            Y = reader.ReadInt16() * ChunkSize;
            var lightCount = reader.ReadInt16() & 0xFFFF;
            CellLights.EnsureCapacity(300);
            CellLightsById.EnsureCapacity(300);

            for (var i = 0; i < lightCount; i++)
            {
                var id = reader.ReadInt64();
                var allowOutdoorLighting = reader.ReadBoolean();
                var ambiance = reader.ReadInt32();
                var shadows = reader.ReadInt32();
                var lights = reader.ReadInt32();
                var cellLight = new CellLight(ambiance, shadows, lights, allowOutdoorLighting);
                CellLights.Add(cellLight);
                CellLightsById.Add(id, cellLight);
            }
            CellLightsById.TrimExcess();
        }

        public void Save(BinaryWriter writer)
        {
            
        }
    }

    public class CellLight
    {
        private const int DefaultFactor = 128;
        public float AmbianceLightR { get; set; }
        public float AmbianceLightG { get; set; }
        public float AmbianceLightB { get; set; }
        public float ShadowR { get; set; }
        public float ShadowG { get; set; }
        public float ShadowB { get; set; }
        public float LightR { get; set; }
        public float LightG { get; set; }
        public float LightB { get; set; }
        public bool AllowOutdoorLighting { get; set; }
        public bool HasShadows { get; set; }
        public float[] Merged { get; set; } = [0.0f, 0.0f, 0.0f];
        public float[] NightLight { get; set; }

        public CellLight(int ambianceLight, int shadows, int lights, bool allowOutdoorLighting)
        {
            AmbianceLightR = GetRedFromInt(ambianceLight);
            AmbianceLightG = GetGreenFromInt(ambianceLight);
            AmbianceLightB = GetBlueFromInt(ambianceLight);
            
            HasShadows = shadows != DefaultFactor;
            ShadowR = GetRedFromInt(shadows);
            ShadowG = GetGreenFromInt(shadows);
            ShadowB = GetBlueFromInt(shadows);
            
            NightLight = lights != DefaultFactor ? [0.0f, 0.0f, 0.0f] : null;
            LightR = GetRedFromInt(lights);
            LightG = GetGreenFromInt(lights);
            LightB = GetBlueFromInt(lights);
            AllowOutdoorLighting = allowOutdoorLighting;
        }

        private float GetRedFromInt(int value)
        {
            return (value & 0xFF) / 255.0f;
        }
        
        private float GetGreenFromInt(int value)
        {
            return (value >> 8 & 0xFF) / 255.0f;
        }
        
        private float GetBlueFromInt(int value)
        {
            return (value >> 16 & 0xFF) / 255.0f;
        }
    }
}
