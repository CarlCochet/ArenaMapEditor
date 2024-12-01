using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class GfxData
{
    public const int MapWidth = 1024;
    public const int MapHeight = 576;
    public List<Partition> Partitions = [];
    
    private const int ElevationStep = 10;

    public GfxData(string path, string id)
    {
        LoadData(path, id);
    }
    
    private void LoadData(string path, string id)
    {
        using var archive = ZipFile.OpenRead($"{path}/gfx/{id}.jar");

        foreach (var entry in archive.Entries)
        {
            if (!entry.FullName.Contains('_'))
                continue;
            
            var splitName = entry.FullName.Split('_');
            if (!short.TryParse(splitName[0], out var x))
                x = 0;
            if (!short.TryParse(splitName[1], out var y))
                y = 0;
            
            var partition = new Partition(entry, x, y);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public class Partition
    {
        public string Id { get; set; }
        public List<Element> Elements = [];

        public short X;
        public short Y;
        private int _minX = int.MaxValue;
        private int _minY = int.MaxValue;
        private int _maxX = int.MinValue;
        private int _maxY = int.MinValue;
        
        private int _coordMinX = int.MaxValue;
        private int _coordMinY = int.MaxValue;
        private short _coordMinZ = short.MaxValue;
        private int _coordMaxX = int.MinValue;
        private int _coordMaxY = int.MinValue;
        private short _coordMaxZ = short.MinValue;

        public Partition(ZipArchiveEntry entry, short x, short y)
        {
            Id = entry.FullName;
            X = x;
            Y = y;
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
                            
                            if (element.Left < _minX)
                                _minX = element.Left;
                            if (element.Left + element.CommonData.ImgWidth > _maxX)
                                _maxX = element.Left + element.CommonData.ImgWidth;
                            if (element.Top < _minY)
                                _minY = element.Top;
                            if (element.Top + element.CommonData.ImgHeight > _maxY)
                                _maxY = element.Top + element.CommonData.ImgHeight;
                        }
                    }
                }
            }
            reader.Close();
            Elements = Elements
                .Select((e, i) => (Element: e, Index: i))
                .OrderBy(x => x.Element.HashCode)
                .ThenBy(x => x.Index)
                .Select(x => x.Element)
                .ToList();
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
        public ElementData CommonData { get; set; }

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
            CommonData = MapData.Elements[elementId];
            (Left, Top) = IsoToScreen(CellX, CellY, CellZ - Height);
            Top += CommonData.OriginY;

            ComputeHashCode();
            ReadColors(reader, Type);
        }
        
        public (int x, int y) IsoToScreen(int isoX, int isoY, int isoAltitude)
        {
            var x = (isoX - isoY) * 43;
            var y = (int)(-(isoY + isoX) * 21.5f) + isoAltitude * ElevationStep;
            return (x, y);
        }

        private void ComputeHashCode()
        {
            HashCode = (CellY + 8192L & 0x3FFFL) << 34 | 
                       (CellX + 8192L & 0x3FFFL) << 19 | 
                       (AltitudeOrder & 0x1FFFL) << 6 | 0;
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
}
