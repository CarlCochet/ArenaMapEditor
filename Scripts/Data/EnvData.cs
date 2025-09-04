using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class EnvData
{
    public int Id { get; set; }
    public List<Partition> Partitions { get; set; } = [];
    public Dictionary<long, Partition> PartitionsMap { get; set; } = new();

    public EnvData(string id)
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

            var version = reader.ReadByte();

            var partition = new Partition(Id);
            partition.Load(reader);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public class Partition(int id)
    {
        public int Id { get; set; } = id;
        public int X { get; set; }
        public int Y { get; set; }
        
        public ParticleDef[] ParticleData { get; set; }
        public Sound[] Sounds { get; set; }
        public int[] AmbiancesId { get; set; }
        public sbyte[] Ambiances { get; set; }
        public InteractiveElement[] InteractiveElements { get; set; }
        public DynamicElement[] DynamicElements { get; set; }
        
        private const int ChunkSize = 18;

        public void Load(ExtendedDataInputStream reader)
        {
            X = reader.ReadShort();
            Y = reader.ReadShort();

            LoadParticleData(reader);
            LoadSoundData(reader);
            LoadAmbianceData(reader);
            LoadInteractiveElements(reader);
            LoadDynamicElements(reader);
        }

        public void Save(BinaryWriter writer)
        {
            
        }

        private void LoadParticleData(ExtendedDataInputStream reader)
        {
            var particleCount = reader.ReadByte() & 255;
            if (particleCount <= 0) 
                return;
            
            ParticleData = new ParticleDef[particleCount];
            for (var i = 0; i < particleCount; i++)
            {
                var particle = new ParticleDef();
                particle.Load(reader);
                ParticleData[i] = particle;
            }
        }

        private void LoadSoundData(ExtendedDataInputStream reader)
        {
            var soundCount = reader.ReadByte() & 255;
            if (soundCount <= 0) 
                return;
            
            Sounds = new Sound[soundCount];
            for (var i = 0; i < soundCount; i++)
            {
                var sound = new Sound();
                sound.Load(reader);
                Sounds[i] = sound;
            }
        }

        private void LoadAmbianceData(ExtendedDataInputStream reader)
        {
            var ambianceCount = reader.ReadByte() & 255;
            if (ambianceCount <= 0)
            {
                Ambiances = null;
                AmbiancesId = null;
                return;
            }
            
            AmbiancesId = new int[ambianceCount];
            for (var i = 0; i < ambianceCount; i++)
            {
                AmbiancesId[i] = reader.ReadInt();
            }
            
            var ambianceSize = reader.ReadByte() & 255;
            if (ambianceSize <= 0)
            {
                Ambiances = null;
                return;
            }
            Ambiances = reader.ReadBytes(ambianceSize);
        }

        private void LoadInteractiveElements(ExtendedDataInputStream reader)
        {
            var elementCount = reader.ReadByte() & 255;
            if (elementCount <= 0)
            {
                InteractiveElements = [];
                return;
            }
            
            InteractiveElements = new InteractiveElement[elementCount];
            for (var i = 0; i < elementCount; i++)
            {
                var element = new InteractiveElement();
                element.Load(reader);
                InteractiveElements[i] = element;
            }
        }

        private void LoadDynamicElements(ExtendedDataInputStream reader)
        {
            var elementCount = reader.ReadByte() & 255;
            if (elementCount <= 0) 
                return;
            
            DynamicElements = new DynamicElement[elementCount];
            for (var i = 0; i < elementCount; i++)
            {
                var element = new DynamicElement();
                element.Load(reader);
                DynamicElements[i] = element;
            }
        }
    }

    public class Element
    {
        public sbyte X { get; set; }
        public sbyte Y { get; set; }
        public short Z { get; set; }
        
        public virtual void Load(ExtendedDataInputStream reader)
        {
            X = reader.ReadByte();
            Y = reader.ReadByte();
            Z = reader.ReadShort();
        }

        public virtual void Save(BinaryWriter writer)
        {
            
        }
    }

    public class ParticleDef : Element
    {
        public int SystemId { get; set; }
        public sbyte Level { get; set; }
        public sbyte OffsetX { get; set; }
        public sbyte OffsetY { get; set; }
        public sbyte OffsetZ { get; set; }
        public sbyte LoD { get; set; }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            SystemId = reader.ReadInt();
            Level = reader.ReadByte();
            OffsetX = reader.ReadByte();
            OffsetY = reader.ReadByte();
            OffsetZ = reader.ReadByte();
            LoD = reader.ReadByte();
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
    }

    public class Sound : Element
    {
        public int SoundId { get; set; }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            SoundId = reader.ReadInt();
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
    }

    public class InteractiveElement : Element
    {
        public long Id { get; set; }
        public short Type { get; set; }
        public int[] Views { get; set; }
        public sbyte[] Data { get; set; }
        public bool ClientOnly { get; set; }
        public short LandmarkType { get; set; }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            Id = reader.ReadLong();
            Type = reader.ReadShort();

            var viewCount = reader.ReadByte() & 255;
            Views = new int[viewCount];
            for (var i = 0; i < viewCount; i++)
            {
                Views[i] = reader.ReadInt();
            }
            
            var dataLength = reader.ReadShort() & 0xFFFF;
            Data = reader.ReadBytes(dataLength);
            ClientOnly = reader.ReadBooleanBit();
            LandmarkType = reader.ReadShort();
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
    }

    public class DynamicElement : Element
    {
        public int Id { get; set; }
        public int GfxId { get; set; }
        public int Type { get; set; }
        public sbyte Direction { get; set; }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            Id = reader.ReadInt();
            GfxId = reader.ReadInt();
            Type = reader.ReadShort();
            Direction = reader.ReadByte();
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
    }
}
