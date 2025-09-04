using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

public class GfxData
{
    public int Id { get; set; }
    public const int MapWidth = 1024;
    public const int MapHeight = 576;
    public List<Partition> Partitions { get; set; } = [];
    public Dictionary<long, Partition> PartitionsMap { get; set; } = new();
    
    private const int ElevationStep = 10;

    public GfxData(string id)
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
            if (!short.TryParse(splitName[0], out var x))
                x = 0;
            if (!short.TryParse(splitName[1], out var y))
                y = 0;
            
            var partition = new Partition(entry, x, y);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public void Save(string path, string id)
    {
        
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
            Load(entry);
        }

        public void Load(ZipArchiveEntry entry)
        {
            using var stream = entry.Open();
            var reader = new ExtendedDataInputStream(stream);
            
            _coordMinX = reader.ReadInt();
            _coordMinY = reader.ReadInt();
            _coordMinZ = reader.ReadShort();
            _coordMaxX = reader.ReadInt();
            _coordMaxY = reader.ReadInt();
            _coordMaxZ = reader.ReadShort();

            var count = reader.ReadShort() & 0xFFFF;
            
            var groupKeys = new int[count];
            var layerIndexes = new sbyte[count];
            var groupIds = new int[count];

            for (var i = 0; i < count; ++i)
            {
                groupKeys[i] = reader.ReadInt();
                layerIndexes[i] = reader.ReadByte();
                groupIds[i] = reader.ReadInt();
            }
            
            var colorCount = reader.ReadShort() & 0xFFFF;
            var colors = new float[colorCount][];
            for (var i = 0; i < colorCount; ++i)
            {
                var type = reader.ReadByte();
                colors[i] = Element.GetNewColors(type);
                Element.ReadColors(reader, type, colors[i]);
            }

            var mapX = reader.ReadInt();
            var mapY = reader.ReadInt();
            var numRects = reader.ReadShort() & 0xFFFF;
            
            for (var i = 0; i < numRects; i++)
            {
                var minX = mapX + (reader.ReadByte() & 0xFF);
                var maxX = mapX + (reader.ReadByte() & 0xFF);
                var minY = mapY + (reader.ReadByte() & 0xFF);
                var maxY = mapY + (reader.ReadByte() & 0xFF);

                for (var cx = minX; cx < maxX; cx++)
                {
                    for (var cy = minY; cy < maxY; cy++)
                    {
                        var numElements = reader.ReadByte() & 0xFF;
                        for (var elementIndex = 0; elementIndex < numElements; elementIndex++)
                        {
                            var element = new Element(cx, cy);
                            element.Load(reader);
                            
                            var groupIndex = reader.ReadShort() & 0xFFFF;
                            element.GroupKey = groupKeys[groupIndex];
                            element.LayerIndex = layerIndexes[groupIndex];
                            element.GroupId = groupIds[groupIndex];
                            
                            var colorIndex = reader.ReadShort() & 0xFFFF;
                            element.Colors = colors[colorIndex];
                            element.Color = element.Colors.Length < 3
                                ? new Color(1, 1, 1)
                                : new Color(element.Colors[0], element.Colors[1], element.Colors[2]);
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
            
            Elements = Elements
                .Select((e, i) => (Element: e, Index: i))
                .OrderBy(x => x.Element.HashCode)
                .ThenBy(x => x.Index)
                .Select(x => x.Element)
                .ToList();
        }

        public void Save()
        {
            
        }
    }

    public class Element
    {
        public const int TeintMask = 0x1;
        public const int AlphaMask = 0x2;
        public const int GradientMask = 0x4;

        private const float DefaultTeint = 0.5f;
        private const float DefaultLight = 1.0f;
        private const float DefaultAlpha = 1.0f;
        
        public byte Type { get; set; }
        public int CellX { get; set; }
        public int CellY { get; set; }
        public short CellZ { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public sbyte AltitudeOrder { get; set; }
        public sbyte Height { get; set; }
        public int GroupId { get; set; }
        public sbyte LayerIndex { get; set; }
        public int GroupKey { get; set; }
        public bool Occluder { get; set; }
        public byte TypeMask { get; set; }
        public long HashCode { get; set; }
        public float[] Colors { get; set; }
        public Color Color { get; set; }
        public ElementData CommonData { get; set; }

        public Element(int x, int y)
        {
            CellX = x;
            CellY = y;
        }

        public void Load(ExtendedDataInputStream reader)
        {
            CellZ = reader.ReadShort();
            Height = reader.ReadByte();
            AltitudeOrder = reader.ReadByte();
            Occluder = reader.ReadBooleanBit();
            
            TypeMask = reader.ReadBooleanBit() ? (byte)TeintMask : (byte)0;
            TypeMask |= reader.ReadBooleanBit() ? (byte)AlphaMask : (byte)0;
            TypeMask |= reader.ReadBooleanBit() ? (byte)GradientMask : (byte)0;
            
            var elementId = reader.ReadInt();
            if (GlobalData.Instance.Elements.TryGetValue(elementId, out var data))
            {
                CommonData = data;
                (Left, Top) = IsoToScreen(CellX, CellY, CellZ - Height);
                Top += CommonData.OriginY;
            }
            else
            {
                GD.PrintErr($"Element {elementId} not found");
            }
            
            ComputeHashCode();
        }
        
        public void Save()
        {
            
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

        public static float[] GetNewColors(int type)
        {
            var size = 0;
            size += (type & TeintMask) == TeintMask ? 3 : 0;
            size += (type & AlphaMask) == AlphaMask ? 1 : 0;
            size *= (type & GradientMask) == GradientMask ? 2 : 1;
            return new float[size];
        }

        public static void ReadColors(ExtendedDataInputStream reader, int type, float[] colors)
        {
            var i = 0;
            if ((type & TeintMask) == TeintMask)
            {
                colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
            }

            if ((type & AlphaMask) == AlphaMask)
            {
                colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
            }

            if ((type & GradientMask) == GradientMask)
            {
                if ((type & TeintMask) == TeintMask)
                {
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                }

                if ((type & AlphaMask) == AlphaMask)
                {
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;;
                }
            }
        }
    }
}
