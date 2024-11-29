using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class GfxData
{
    public const int MapWidth = 1024;
    public const int MapHeight = 576;
    public List<Partition> Partitions = [];
    
    private readonly List<string> _ignoreFiles = ["META-INF", "coord", "groups.lib"];
    private const int ElevationStep = 10;

    public GfxData(string path, string id)
    {
        LoadData(path, id);
    }

    public static (int x, int y) IsoToScreen(int isoX, int isoY, int isoAltitude)
    {
        var x = (isoX - isoY) * 43;
        var y = (int)(-(isoY + isoX) * 21.5f) + isoAltitude * ElevationStep;
        return (x, y);
    }
    
    private void LoadData(string path, string id)
    {
        using var archive = ZipFile.OpenRead($"{path}/gfx/{id}.jar");

        foreach (var entry in archive.Entries)
        {
            if (_ignoreFiles.Contains(entry.FullName))
                continue;
            
            var partition = new Partition(entry);
            Partitions.Add(partition);
        }
    }

    public class Partition
    {
        public string Id { get; set; }
        public List<Element> Elements = [];
        
        private int _coordMinX = int.MaxValue;
        private int _coordMinY = int.MaxValue;
        private short _coordMinZ = short.MaxValue;
        private int _coordMaxX = int.MinValue;
        private int _coordMaxY = int.MinValue;
        private short _coordMaxZ = short.MinValue;

        public Partition(ZipArchiveEntry entry)
        {
            Id = entry.FullName;
            LoadData(entry);
        }

        private void LoadData(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            using var reader = new BinaryReader(stream);
            
            _coordMinX = reader.ReadInt32();
            _coordMinY = reader.ReadInt32();
            _coordMinZ = reader.ReadInt16();
            _coordMaxX = reader.ReadInt32();
            _coordMaxY = reader.ReadInt32();
            _coordMaxZ = reader.ReadInt16();
        
            var baseX = reader.ReadInt32();
            var baseY = reader.ReadInt32();
            var maxZ = reader.ReadInt16() & 0xFFFF;

            for (var z = 0; z < maxZ; z++)
            {
                var minX = baseX + (reader.ReadByte() & 0xFF);
                var maxX = baseX + (reader.ReadByte() & 0xFF);
                var minY = baseY + (reader.ReadByte() & 0xFF);
                var maxY = baseY + (reader.ReadByte() & 0xFF);

                for (var x = minX; x < maxX; x++)
                {
                    for (var y = minY; y < maxY; y++)
                    {
                        var elementCount = reader.ReadByte() & 0xFF;
                        for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
                        {
                            var elementType = reader.ReadByte();
                            var element = new Element(elementType, x, y);
                            element.LoadData(reader);
                            
                            Elements.Add(element);
                        }
                    }
                }
            }
        }
    }

    public class Element
    {
        public byte Type { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public short CellZ { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public byte AltitudeOrder { get; set; }
        public byte Height { get; set; }
        public int GroupId { get; set; }
        public byte LayerIndex { get; set; }
        public int GroupLayer { get; set; }
        public bool Occluder { get; set; }
        public long HashCode { get; set; }
        public float[] Colors { get; set; }
        public ElementProperties CommonData { get; set; }

        public Element(byte type, int x, int y)
        {
            Type = type;
            CellX = x;
            CellY = y;
            Colors = GetNewColors(type);
        }

        public void LoadData(BinaryReader reader)
        {
            CellZ = reader.ReadInt16();
            Height = reader.ReadByte();
            AltitudeOrder = reader.ReadByte();
            GroupId = reader.ReadInt32();
            LayerIndex = reader.ReadByte();
            GroupLayer = reader.ReadInt32();
            Occluder = reader.ReadBoolean();
            var elementId = reader.ReadInt32();
            CommonData = new ElementProperties(elementId);
            (Left, Top) = IsoToScreen(CellX, CellY, CellZ - Height);
            Top += CommonData.OriginY;

            ReadColors(reader, Type);
        }

        private float[] GetNewColors(int type)
        {
            var value = (type & 2) == 2 ? 3 : 0;
            value += (type & 8) == 8 ? 1 : 0;
            value *= (type & 16) == 16 ? 2 : 1;
            value += (type & 1) == 1 ? 3 : 0;
            value += (type & 4) == 4 ? 3 : 0;
            return new float[value];
        }

        private void ReadColors(BinaryReader reader, int type)
        {
            var index = 0;

            if ((type & 1) == 1)
            {
                Colors[index++] = 2.0f * (reader.ReadByte() / 255.0f) + 1.0f;
                Colors[index++] = 2.0f * (reader.ReadByte() / 255.0f) + 1.0f;
                Colors[index++] = 2.0f * (reader.ReadByte() / 255.0f) + 1.0f;
            }

            if ((type & 2) == 2)
            {
                Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
                Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
                Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
            }

            if ((type & 4) == 4)
            {
                Colors[index++] = reader.ReadByte() / 255.0f;
                Colors[index++] = reader.ReadByte() / 255.0f;
                Colors[index++] = reader.ReadByte() / 255.0f;
            }

            if ((type & 8) == 8)
            {
                Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
            }

            if ((type & 16) == 16)
            {
                if ((type & 2) == 2)
                {
                    Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
                    Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
                    Colors[index++] = reader.ReadByte() / 255.0f + 0.5f;
                }

                if ((type & 8) == 8)
                {
                    Colors[index] = reader.ReadByte() / 255.0f + 0.5f;
                }
            }
        }
    }

    public class ElementProperties
    {
        public AnimationData AnimationData { get; set; }
        public short OriginX { get; set; }
        public short OriginY { get; set; }
        public short ImgWidth { get; set; }
        public short ImgHeight { get; set; }
        public int GfxId { get; set; }
        public byte VisualHeight { get; set; }
        public byte VisibilityMask { get; set; }
        public byte Shader { get; set; }
        public byte PropertiesFlag { get; set; }
        public byte GroundSoundType { get; set; }
        public byte Slope { get; set; }
        public bool MoveTop { get; set; }
        public bool Walkable { get; set; }
        public bool Animated { get; set; }
        public bool BeforeMobile { get; set; }
        public bool Flip { get; set; }

        public ElementProperties(int elementId)
        {
            
        }
    }

    public class AnimationData
    {
        public int Duration { get; set; }
        public short[] Frames { get; set; }
    }
}
