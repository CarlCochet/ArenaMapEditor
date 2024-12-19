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
            using var reader = new BinaryReader(stream);

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
        
        public Particle[] Particles { get; set; }
        public Sound[] Sounds { get; set; }
        public int[] AmbiancesId { get; set; }
        public byte[] Ambiances { get; set; }
        public InteractiveElement[] InteractiveElements { get; set; }
        public DynamicElement[] DynamicElements { get; set; }
        
        private const int ChunkSize = 18;

        public void Load(BinaryReader reader)
        {
            X = reader.ReadInt16();
            Y = reader.ReadInt16();

            LoadParticles(reader);
            LoadSounds(reader);
            LoadAmbiances(reader);
            LoadInteractiveElements(reader);
            LoadDynamicElements(reader);
        }

        public void Save(BinaryWriter writer)
        {
            
        }

        private void LoadParticles(BinaryReader reader)
        {
            var particleCount = reader.ReadByte() & 255;
            if (particleCount <= 0) 
                return;
            
            Particles = new Particle[particleCount];
            for (var i = 0; i < particleCount; i++)
            {
                var particle = new Particle();
                particle.Load(reader);
                Particles[i] = particle;
            }
        }

        private void LoadSounds(BinaryReader reader)
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

        private void LoadAmbiances(BinaryReader reader)
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
                AmbiancesId[i] = reader.ReadInt32();
            }
            
            var ambianceSize = reader.ReadByte() & 255;
            if (ambianceSize <= 0)
            {
                Ambiances = null;
                return;
            }
            Ambiances = reader.ReadBytes(ambianceSize);
        }

        private void LoadInteractiveElements(BinaryReader reader)
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

        private void LoadDynamicElements(BinaryReader reader)
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
        public byte X { get; set; }
        public byte Y { get; set; }
        public short Z { get; set; }
        
        public virtual void Load(BinaryReader reader)
        {
            X = reader.ReadByte();
            Y = reader.ReadByte();
            Z = reader.ReadInt16();
        }

        public virtual void Save(BinaryWriter writer)
        {
            
        }
    }

    public class Particle : Element
    {
        public int SystemId { get; set; }
        public byte Level { get; set; }
        public byte OffsetX { get; set; }
        public byte OffsetY { get; set; }
        public byte OffsetZ { get; set; }
        public byte LoD { get; set; }
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            
            SystemId = reader.ReadInt32();
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
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            
            SoundId = reader.ReadInt32();
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
        public byte[] Data { get; set; }
        public bool ClientOnly { get; set; }
        public short LandmarkType { get; set; }
        
        public override void Load(BinaryReader reader)
        {
            Id = reader.ReadInt64();
            Type = reader.ReadInt16();

            var viewCount = reader.ReadInt16() & 255;
            Views = new int[viewCount];
            for (var i = 0; i < viewCount; i++)
            {
                Views[i] = reader.ReadInt32();
            }
            
            var dataLength = reader.ReadInt16() & 0xFFFF;
            Data = reader.ReadBytes(dataLength);
            ClientOnly = reader.ReadBoolean();
            LandmarkType = reader.ReadInt16();
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
        public byte Direction { get; set; }
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            
            Id = reader.ReadInt32();
            GfxId = reader.ReadInt32();
            Type = reader.ReadInt16();
            Direction = reader.ReadByte();
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
    }
}
