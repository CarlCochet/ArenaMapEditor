using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FileAccess = Godot.FileAccess;

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
        if (FileAccess.FileExists($"{path}/{Id}.jar"))
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
        foreach (var partition in Partitions)
        {
            using var fileStream = File.Create(Path.Combine(path, $"{partition.X}_{partition.Y}"));
            using var writer = new OutputBitStream(fileStream);
            partition.Save(writer);
        }
    }

    public class Partition(int id)
    {
        public int Id { get; set; } = id;
        public sbyte Version { get; set; }
        public short X { get; set; }
        public short Y { get; set; }
        
        public ParticleDef[] ParticleData { get; set; }
        public Sound[] Sounds { get; set; }
        public int[] AmbiancesId { get; set; }
        public sbyte[] Ambiances { get; set; }
        public InteractiveElement[] InteractiveElements { get; set; }
        public DynamicElement[] DynamicElements { get; set; }
        
        private const int ChunkSize = 18;

        public void Load(ExtendedDataInputStream reader)
        {
            Version = reader.ReadByte();
            X = reader.ReadShort();
            Y = reader.ReadShort();

            LoadParticleData(reader);
            LoadSoundData(reader);
            LoadAmbianceData(reader);
            LoadInteractiveElements(reader);
            LoadDynamicElements(reader);
        }

        public void Save(OutputBitStream writer)
        {
            writer.WriteByte(Version);
            writer.WriteShort(X);
            writer.WriteShort(Y);
            
            SaveParticleData(writer);
            SaveSoundData(writer);
            SaveAmbianceData(writer);
            SaveInteractiveElements(writer);
            SaveDynamicElements(writer);
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

        private void SaveParticleData(OutputBitStream writer)
        {
            writer.WriteByte((sbyte)(ParticleData?.Length ?? 0));
            if (ParticleData == null)
                return;
            
            foreach (var particle in ParticleData)
            {
                particle.Save(writer);
            }
        }

        private void SaveSoundData(OutputBitStream writer)
        {
            writer.WriteByte((sbyte)(Sounds?.Length ?? 0));
            if (Sounds == null)
                return;
            
            foreach (var sound in Sounds)
            {
                sound.Save(writer);
            }
        }

        private void SaveAmbianceData(OutputBitStream writer)
        {
            writer.WriteByte((sbyte)(AmbiancesId?.Length ?? 0));
            if (AmbiancesId != null)
            {
                foreach (var id in AmbiancesId)
                {
                    writer.WriteInt(id);
                }
                writer.WriteByte((sbyte)(Ambiances?.Length ?? 0));
                if (Ambiances != null) 
                    writer.WriteBytes(Ambiances);
            }
            else
            {
                writer.WriteByte(0);
            }
        }

        private void SaveInteractiveElements(OutputBitStream writer)
        {
            writer.WriteByte((sbyte)(InteractiveElements?.Length ?? 0));
            if (InteractiveElements == null)
                return;
            
            foreach (var interactiveElement in InteractiveElements)
            {
                interactiveElement.Save(writer);
            }
        }

        private void SaveDynamicElements(OutputBitStream writer)
        {
            writer.WriteByte((sbyte)(DynamicElements?.Length ?? 0));
            if (DynamicElements == null)
                return;
            
            foreach (var dynamicElement in DynamicElements)
            {
                dynamicElement.Save(writer);
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

        public virtual void Save(OutputBitStream writer)
        {
            writer.WriteByte(X);
            writer.WriteByte(Y);
            writer.WriteShort(Z);
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
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteInt(SystemId);
            writer.WriteByte(Level);
            writer.WriteByte(OffsetX);
            writer.WriteByte(OffsetY);
            writer.WriteByte(OffsetZ);
            writer.WriteByte(LoD);
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
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteInt(SoundId);
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

        private sbyte _viewCount;
        private short _dataLength;
        
        public override void Load(ExtendedDataInputStream reader)
        {
            Id = reader.ReadLong();
            Type = reader.ReadShort();

            _viewCount = reader.ReadByte();
            var viewCount = _viewCount & 255;
            Views = new int[viewCount];
            for (var i = 0; i < viewCount; i++)
            {
                Views[i] = reader.ReadInt();
            }
            
            _dataLength = reader.ReadShort();
            var dataLength = _dataLength & 0xFFFF;
            Data = reader.ReadBytes(dataLength);
            ClientOnly = reader.ReadBooleanBit();
            LandmarkType = reader.ReadShort();
        }
        
        public override void Save(OutputBitStream writer)
        {
            writer.WriteLong(Id);
            writer.WriteShort(Type);
            writer.WriteByte(_viewCount);

            foreach (var view in Views)
            {
                writer.WriteInt(view);
            }
            
            writer.WriteShort(_dataLength);
            writer.WriteBytes(Data);
            writer.WriteBooleanBit(ClientOnly);
            writer.WriteShort(LandmarkType);
        }
    }

    public class DynamicElement : Element
    {
        public int Id { get; set; }
        public int GfxId { get; set; }
        public short Type { get; set; }
        public sbyte Direction { get; set; }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            Id = reader.ReadInt();
            GfxId = reader.ReadInt();
            Type = reader.ReadShort();
            Direction = reader.ReadByte();
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteInt(Id);
            writer.WriteInt(GfxId);
            writer.WriteShort(Type);
            writer.WriteByte(Direction);
        }
    }
}
