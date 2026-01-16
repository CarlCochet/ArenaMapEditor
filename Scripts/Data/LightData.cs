using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FileAccess = Godot.FileAccess;

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
        if (!FileAccess.FileExists($"{path}/{Id}.jar"))
            LoadFromJar(path);
        else
            LoadFromFolder(path);
    }

    private void LoadFromJar(string path)
    {
        using var archive = ZipFile.OpenRead($"{path}/{Id}.jar");
        
        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.Contains('_'))
                continue;

            using var stream = entry.Open(); 
            var reader = new ExtendedDataInputStream(stream);

            var partition = new Partition(Id);
            partition.Load(reader);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    private void LoadFromFolder(string path)
    {
        var folderPath = $"{path}/{Id}";
        var dirAccess = DirAccess.Open(folderPath);
        if (dirAccess == null)
            return;
        
        dirAccess.ListDirBegin();
        var fileName = dirAccess.GetNext();

        while (fileName != "")
        {
            if (dirAccess.CurrentIsDir() || !fileName.Contains('_'))
            {
                fileName = dirAccess.GetNext();
                continue;
            }

            var filePath = $"{folderPath}/{fileName}";
            if (!FileAccess.FileExists(filePath))
            {
                fileName = dirAccess.GetNext();
                continue;
            }
            using var stream = File.OpenRead(filePath);
            var reader = new ExtendedDataInputStream(stream);

            var partition = new Partition(Id);
            partition.Load(reader);
            Partitions.Add(partition);

            fileName = dirAccess.GetNext();
        }
        dirAccess.ListDirEnd();

        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public void Save(string path)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"light_{Id}_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            foreach (var partition in Partitions)
            {
                var mapX = partition.X / MapConstants.MapWidth;
                var mapY = partition.Y / MapConstants.MapLength;
                var fileName = $"{mapX}_{mapY}";
                var filePath = Path.Combine(tempDir, fileName);
            
                using var fileStream = File.Create(filePath);
                using var writer = new OutputBitStream(fileStream);
                partition.Save(writer);
            }
            
            var jarPath = Path.Combine(path, $"{Id}.jar");
            if (File.Exists(jarPath))
            {
                File.Delete(jarPath);
            }
            ZipFile.CreateFromDirectory(tempDir, jarPath);
        }
        finally
        {
            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }
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
                var def = new CellLightDef();
                def.Load(reader);
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

        public void Save(OutputBitStream writer)
        {
            writer.WriteShort((short)(X / ChunkSize));
            writer.WriteShort((short)(Y / ChunkSize));
            writer.WriteShort(unchecked((short)CellLightDef.Count));

            foreach (var def in CellLightDef)
            {
                def.Save(writer);
            }
            
            writer.WriteByte(unchecked((sbyte)(layerColors.Length / MapConstants.NumCells)));
            var filledLayersCount = layerColors.Count(l => l != null);
            writer.WriteShort(unchecked((short)filledLayersCount));

            for (var k = 0; k < layerColors.Length; k++)
            {
                for (var idx = 0; idx < CellLightDef.Count; idx++)
                {
                    if (layerColors[k] != CellLightDef[idx])
                    {
                        continue;
                    }
                    
                    writer.WriteShort(unchecked((short)k));
                    writer.WriteShort(unchecked((short)idx));
                    break;
                }
            }
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

        private int _ambianceLight;
        private int _shadows;
        private int _lights;

        public void Load(ExtendedDataInputStream reader)
        {
            AllowOutdoorLighting = reader.ReadBooleanBit();
            _ambianceLight = reader.ReadInt();
            _shadows = reader.ReadInt();
            _lights = reader.ReadInt();
            
            AmbianceLightR = ArenaColor.GetRedFromARGB(_ambianceLight) * ColorFactor;
            AmbianceLightG = ArenaColor.GetGreenFromARGB(_ambianceLight) * ColorFactor;
            AmbianceLightB = ArenaColor.GetBlueFromARGB(_ambianceLight) * ColorFactor;
            
            HasShadows = _shadows != _defaultColor;
            ShadowsR = ArenaColor.GetRedFromARGB(_shadows);
            ShadowsG = ArenaColor.GetGreenFromARGB(_shadows);
            ShadowsB = ArenaColor.GetBlueFromARGB(_shadows);
            
            NightLight = _lights != _defaultColor ? [0.0f, 0.0f, 0.0f] : null;
            LightsR = ArenaColor.GetRedFromARGB(_lights) - 0.5f;
            LightsG = ArenaColor.GetGreenFromARGB(_lights) - 0.5f;
            LightsB = ArenaColor.GetBlueFromARGB(_lights) - 0.5f;
        }

        public void Save(OutputBitStream writer)
        {
            writer.WriteBooleanBit(AllowOutdoorLighting);
            
            var ambR = Math.Clamp(AmbianceLightR / ColorFactor, 0.0f, 1.0f);
            var ambG = Math.Clamp(AmbianceLightG / ColorFactor, 0.0f, 1.0f);
            var ambB = Math.Clamp(AmbianceLightB / ColorFactor, 0.0f, 1.0f);
            var ambianceLight = ArenaColor.GetFromFloat(ambR, ambG, ambB, 1.0f);
    
            int shadows;
            if (!HasShadows)
            {
                shadows = _defaultColor;
            }
            else
            {
                var shadR = Math.Clamp(ShadowsR, 0.0f, 1.0f);
                var shadG = Math.Clamp(ShadowsG, 0.0f, 1.0f);
                var shadB = Math.Clamp(ShadowsB, 0.0f, 1.0f);
                shadows = ArenaColor.GetFromFloat(shadR, shadG, shadB, 1.0f);
            }
    
            int lights;
            if (NightLight == null)
            {
                lights = _defaultColor;
            }
            else
            {
                var lightR = Math.Clamp(LightsR + 0.5f, 0.0f, 1.0f);
                var lightG = Math.Clamp(LightsG + 0.5f, 0.0f, 1.0f);
                var lightB = Math.Clamp(LightsB + 0.5f, 0.0f, 1.0f);
                lights = ArenaColor.GetFromFloat(lightR, lightG, lightB, 1.0f);
            }
    
            writer.WriteInt(ambianceLight);
            writer.WriteInt(shadows);
            writer.WriteInt(lights);
        }
    }
}
