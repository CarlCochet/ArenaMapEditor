using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using FileAccess = Godot.FileAccess;

public class GfxData
{
    public int Id { get; set; }
    public List<Partition> Partitions { get; set; } = [];
    
    private const int ElevationStep = 10;

    public GfxData(string id)
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
            
            var splitName = entry.FullName.Split('_');
            if (!short.TryParse(splitName[0], out var x))
                x = 0;
            if (!short.TryParse(splitName[1], out var y))
                y = 0;

            using var stream = entry.Open(); 
            var reader = new ExtendedDataInputStream(stream);
            
            var partition = new Partition(x, y);
            partition.Load(reader);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        CleanupDuplicates();    
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
            
            var splitName = fileName.Split('_');
            if (!short.TryParse(splitName[0], out var x))
                x = 0;
            if (!short.TryParse(splitName[1], out var y))
                y = 0;

            var partition = new Partition(x, y);
            partition.Load(reader);
            Partitions.Add(partition);

            fileName = dirAccess.GetNext();
        }
        dirAccess.ListDirEnd();

        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
        CleanupDuplicates();    
    }

    public void Save(string path)
    {
        using var coordStream = File.Create(Path.Combine(path, "coord"));
        using var coordWriter = new OutputBitStream(coordStream);
        
        foreach (var partition in Partitions)
        {
            using var fileStream = File.Create(Path.Combine(path, $"{partition.X}_{partition.Y}"));
            using var writer = new OutputBitStream(fileStream);
            partition.Save(writer);
            
            var bigEndianX = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(partition.X);
            var bigEndianY = System.Buffers.Binary.BinaryPrimitives.ReverseEndianness(partition.Y);
            
            coordWriter.WriteShort(bigEndianX);
            coordWriter.WriteShort(bigEndianY);
        }
    }

    public void AddElement(Element element)
    {
        var partition = Partitions
            .OrderBy(p => p.DistanceToCenter(element.CellX, element.CellY))
            .FirstOrDefault();
        var cellElements = Partitions
            .SelectMany(p => p.Elements)
            .Where(e => e.CellX == element.CellX && e.CellY == element.CellY)
            .OrderBy(e => e.AltitudeOrder)
            .ToList();

        var elementAdded = false;
        foreach (var cellElement in cellElements)
        {
            if (cellElement.CellZ > element.CellZ && !elementAdded)
            {
                element.AltitudeOrder = cellElement.AltitudeOrder;
                elementAdded = true;
            }

            if (!elementAdded) 
                continue;
            
            cellElement.AltitudeOrder++;
            cellElement.ComputeHashCode();
        }

        if (!elementAdded && cellElements.Count > 0)
            element.AltitudeOrder = (sbyte)(cellElements.MaxBy(e => e.AltitudeOrder).AltitudeOrder + 1);

        element.ComputeHashCode();
        partition?.AddElement(element);
    }

    public void RemoveElement(Element element)
    {
        var partition = Partitions.FirstOrDefault(p => p.Elements.Contains(element));
        partition?.Elements.Remove(element);
        partition?.SortElements();
        
        var elements = Partitions
            .SelectMany(p => p.Elements)
            .Where(e => e.CellX == element.CellX && e.CellY == element.CellY)
            .OrderBy(e => e.AltitudeOrder)
            .ToList();
        
        for (var i = 0; i < elements.Count; i++)
        {
            elements[i].AltitudeOrder = (sbyte)i;
            elements[i].ComputeHashCode();
        }
        Partitions.ForEach(p => p.SortElements());
    }
    
    public bool ElementExists(Element element) => Partitions.Any(p => p.Elements.Contains(element));

    public bool HasElementAt(int x, int y) => Partitions.Any(p => p.Elements.Exists(e => e.CellX == x && e.CellY == y));
    
    private void CleanupDuplicates()
    {
        var seen = new HashSet<(int cellX, int cellY, short cellZ, int gfxId)>();
        
        foreach (var partition in Partitions)
        {
            var filtered = (from element in partition.Elements 
                let gfxId = element.CommonData?.GfxId ?? -1 
                let key = (element.CellX, element.CellY, element.CellZ, gfxId) 
                where seen.Add(key) 
                select element)
                .ToList();
            partition.Elements = filtered;
        }

        var elementsByCell = Partitions
            .SelectMany(p => p.Elements)
            .GroupBy(e => (e.CellX, e.CellY))
            .ToList();
        foreach (var group in elementsByCell)
        {
            var ordered = group.OrderBy(e => e.AltitudeOrder).ToList();
            for (var i = 0; i < ordered.Count; i++)
            {
                ordered[i].AltitudeOrder = (sbyte)i;
                ordered[i].ComputeHashCode();
            }
        }

        foreach (var partition in Partitions)
        {
            partition.SortElements();
            partition.RecomputeBounds();
        }
    }

    public class Partition
    {
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

        private int _centerX = int.MinValue;
        private int _centerY = int.MinValue;

        private Color _color;

        public Partition(short x, short y)
        {
            X = x;
            Y = y;
            _color = new Color(GlobalData.Instance.Rng.Randf(), 
                GlobalData.Instance.Rng.Randf(),
                GlobalData.Instance.Rng.Randf());
        }

        public void Load(ExtendedDataInputStream reader)
        {
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
                            element.GroupKey = groupIndex < groupKeys.Length ? groupKeys[groupIndex] : 0;
                            element.LayerIndex = groupIndex < layerIndexes.Length ? layerIndexes[groupIndex] : (sbyte)0;
                            element.GroupId = groupIndex < groupIds.Length ? groupIds[groupIndex] : 0;
                            
                            var colorIndex = reader.ReadShort() & 0xFFFF;
                            element.Colors = colorIndex < colors.Length ? colors[colorIndex] : [ 1.0f, 1.0f, 1.0f ];
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
            
            SortElements();
            _centerX = (_coordMaxX + _coordMinX) / 2;
            _centerY = (_coordMaxY + _coordMinY) / 2;
        }

        public void Save(OutputBitStream writer)
        {
            RecomputeBounds();
            writer.WriteInt(_coordMinX);
            writer.WriteInt(_coordMinY);
            writer.WriteShort(_coordMinZ);
            writer.WriteInt(_coordMaxX);
            writer.WriteInt(_coordMaxY);
            writer.WriteShort(_coordMaxZ);
            
            var groupMap = new Dictionary<(int, sbyte, int), int>();
            var groupList = new List<(int groupKey, sbyte layerIndex, int groupId)>();
            
            foreach (var element in Elements)
            {
                var key = (element.GroupKey, element.LayerIndex, element.GroupId);
                if (groupMap.ContainsKey(key))
                    continue;
                
                groupMap[key] = groupList.Count;
                groupList.Add(key);
            }
            
            writer.WriteShort(unchecked((short)groupList.Count));
            foreach (var group in groupList)
            {
                writer.WriteInt(group.groupKey);
                writer.WriteByte(group.layerIndex);
                writer.WriteInt(group.groupId);
            }
            
            var colorMap = new Dictionary<string, int>(new ColorArrayComparer());
            var colorList = new List<float[]>();
            
            foreach (var element in Elements)
            {
                var colorKey = string.Join(",", element.Colors);
                if (colorMap.ContainsKey(colorKey))
                    continue;
                
                colorMap[colorKey] = colorList.Count;
                colorList.Add(element.Colors);
            }
            
            writer.WriteShort(unchecked((short)colorList.Count));
            foreach (var colors in colorList)
            {
                var type = Element.GetColorType(colors);
                writer.WriteByte(type);
                Element.WriteColors(writer, type, colors);
            }
            
            var cellMap = new Dictionary<(int, int), List<Element>>();
            var minCellX = int.MaxValue;
            var maxCellX = int.MinValue;
            var minCellY = int.MaxValue;
            var maxCellY = int.MinValue;
            
            foreach (var element in Elements)
            {
                var key = (element.CellX, element.CellY);
                if (!cellMap.TryGetValue(key, out var value))
                {
                    value = [];
                    cellMap[key] = value;
                }

                value.Add(element);
                
                minCellX = Math.Min(minCellX, element.CellX);
                maxCellX = Math.Max(maxCellX, element.CellX);
                minCellY = Math.Min(minCellY, element.CellY);
                maxCellY = Math.Max(maxCellY, element.CellY);
            }
            
            var mapX = minCellX;
            var mapY = minCellY;
            writer.WriteInt(mapX);
            writer.WriteInt(mapY);
            
            var rects = BuildRectangles(cellMap);
            writer.WriteShort((short)rects.Count);
            
            foreach (var rect in rects)
            {
                writer.WriteByte((sbyte)(rect.minX - mapX));
                writer.WriteByte((sbyte)(rect.maxX - mapX));
                writer.WriteByte((sbyte)(rect.minY - mapY));
                writer.WriteByte((sbyte)(rect.maxY - mapY));
                
                for (var cx = rect.minX; cx < rect.maxX; cx++)
                {
                    for (var cy = rect.minY; cy < rect.maxY; cy++)
                    {
                        var cellElements = cellMap.GetValueOrDefault((cx, cy), []);
                        writer.WriteByte((sbyte)cellElements.Count);
                        
                        foreach (var element in cellElements)
                        {
                            element.Save(writer);
                            
                            var groupIndex = groupMap[(element.GroupKey, element.LayerIndex, element.GroupId)];
                            writer.WriteShort((short)groupIndex);
                            
                            var colorKey = string.Join(",", element.Colors);
                            var colorIndex = colorMap[colorKey];
                            writer.WriteShort((short)colorIndex);
                        }
                    }
                }
            }
        }

        public void SortElements()
        {
            Elements = Elements
                .Select((e, i) => (Element: e, Index: i))
                .OrderBy(x => x.Element.HashCode)
                .ThenBy(x => x.Index)
                .Select(x => x.Element)
                .ToList();
        }
        
        public int DistanceToCenter(int x, int y) => Math.Abs(x - _centerX) + Math.Abs(y - _centerY);

        public void AddElement(Element element)
        {
            Elements.Add(element);
            SortElements();
            RecomputeBounds();
        }
        
        public void RecomputeBounds()
        {
            _coordMinX = int.MaxValue;
            _coordMinY = int.MaxValue;
            _coordMinZ = short.MaxValue;
            _coordMaxX = int.MinValue;
            _coordMaxY = int.MinValue;
            _coordMaxZ = short.MinValue;
    
            foreach (var element in Elements)
            {
                _coordMinX = Math.Min(_coordMinX, element.CellX);
                _coordMaxX = Math.Max(_coordMaxX, element.CellX);
                _coordMinY = Math.Min(_coordMinY, element.CellY);
                _coordMaxY = Math.Max(_coordMaxY, element.CellY);
                _coordMinZ = Math.Min(_coordMinZ, element.CellZ);
                _coordMaxZ = Math.Max(_coordMaxZ, element.CellZ);
            }
            
            _centerX = (_coordMaxX + _coordMinX) / 2;
            _centerY = (_coordMaxY + _coordMinY) / 2;
        }
        
        private static List<(int minX, int maxX, int minY, int maxY)> BuildRectangles(Dictionary<(int, int), List<Element>> cellMap)
        {
            var rects = new List<(int, int, int, int)>();
            var processed = new HashSet<(int, int)>();
            
            foreach (var cellKey in cellMap.Keys.OrderBy(k => k.Item1).ThenBy(k => k.Item2))
            {
                if (processed.Contains(cellKey))
                    continue;
                    
                var minX = cellKey.Item1;
                var minY = cellKey.Item2;
                var maxX = minX + 1;
                var maxY = minY + 1;
                
                processed.Add(cellKey);
                rects.Add((minX, maxX, minY, maxY));
            }
            
            return rects;
        }
    }

    public class Element(int x, int y)
    {
        public const int TeintMask = 0x1;
        public const int AlphaMask = 0x2;
        public const int GradientMask = 0x4;

        private const float DefaultTeint = 0.5f;

        public int CellX { get; set; } = x;
        public int CellY { get; set; } = y;
        public short CellZ { get; set; }
        public int Top { get; set; }
        public int Left { get; set; }
        public sbyte AltitudeOrder { get; set; }
        public sbyte Height { get; set; }
        public int GroupId { get; set; }
        public sbyte LayerIndex { get; set; }
        public int GroupKey { get; set; }
        public bool Occluder { get; set; }
        public sbyte TypeMask { get; set; }
        public long HashCode { get; set; }
        public bool Walkable { get; set; }
        public float[] Colors { get; set; }
        public Color Color { get; set; }
        public ElementData CommonData { get; set; }

        public void Load(ExtendedDataInputStream reader)
        {
            CellZ = reader.ReadShort();
            Height = reader.ReadByte();
            AltitudeOrder = reader.ReadByte();
            Occluder = reader.ReadBooleanBit();
            
            TypeMask = reader.ReadBooleanBit() ? (sbyte)TeintMask : (sbyte)0;
            TypeMask |= reader.ReadBooleanBit() ? (sbyte)AlphaMask : (sbyte)0;
            TypeMask |= reader.ReadBooleanBit() ? (sbyte)GradientMask : (sbyte)0;
            
            var elementId = reader.ReadInt();
            if (GlobalData.Instance.Elements.TryGetValue(elementId, out var data))
            {
                CommonData = data;
            }
            else
            {
                var newData = new ElementData();
                GlobalData.Instance.Elements.Add(elementId, newData);
                CommonData = newData;
                GD.PrintErr($"Element {elementId} not found");
            }
            
            (Left, Top) = IsoToScreen(CellX, CellY, CellZ - Height);
            Top += CommonData.OriginY;
            // Walkable = CommonData.Walkable;
            Walkable = reader.ReadBooleanBit();
            ComputeHashCode();
        }
        
        public void Save(OutputBitStream writer)
        {
            writer.WriteShort(CellZ);
            writer.WriteByte(Height);
            writer.WriteByte(AltitudeOrder);
            writer.WriteBooleanBit(Occluder);
            
            writer.WriteBooleanBit((TypeMask & TeintMask) != 0);
            writer.WriteBooleanBit((TypeMask & AlphaMask) != 0);
            writer.WriteBooleanBit((TypeMask & GradientMask) != 0);
            
            writer.WriteInt(CommonData.Id);
            writer.WriteBooleanBit(Walkable);
        }
        
        private static (int x, int y) IsoToScreen(int isoX, int isoY, int isoAltitude)
        {
            var x = (isoX - isoY) * 43;
            var y = (int)(-(isoY + isoX) * 21.5f) + isoAltitude * ElevationStep;
            return (x, y);
        }

        public void ComputeHashCode()
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
        
        public static sbyte GetColorType(float[] colors)
        {
            sbyte type = 0;
            var hasGradient = colors.Length is 6 or 8 or 2;
            var baseSize = hasGradient ? colors.Length / 2 : colors.Length;

            if (baseSize >= 3)
                type |= TeintMask;
            if (baseSize is 4 or 1)
                type |= AlphaMask;
            if (hasGradient)
                type |= GradientMask;
            
            return type;
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
                colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;

            if ((type & GradientMask) == GradientMask)
            {
                if ((type & TeintMask) == TeintMask)
                {
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
                }

                if ((type & AlphaMask) == AlphaMask)
                    colors[i++] = reader.ReadByte() / 255.0f + DefaultTeint;
            }
        }
        
        public static void WriteColors(OutputBitStream writer, sbyte type, float[] colors)
        {
            var i = 0;
            if ((type & TeintMask) == TeintMask)
            {
                writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
                writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
                writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
            }

            if ((type & AlphaMask) == AlphaMask)
                writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));

            if ((type & GradientMask) == GradientMask)
            {
                if ((type & TeintMask) == TeintMask)
                {
                    writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
                    writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
                    writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
                }

                if ((type & AlphaMask) == AlphaMask)
                    writer.WriteByte((sbyte)((colors[i++] - DefaultTeint) * 255));
            }
        }

        public Element Copy()
        {
            return new Element(CellX, CellY)
            {
                CellZ = CellZ,
                Top = Top,
                Left = Left,
                AltitudeOrder = AltitudeOrder,
                Height = Height,
                GroupId = GroupId,
                LayerIndex = LayerIndex,
                GroupKey = GroupKey,
                Occluder = Occluder,
                TypeMask = TypeMask,
                Colors = Colors.ToArray(),
                Color = Color,
                CommonData = CommonData
            };
        }
    }
    
    private class ColorArrayComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y) => x == y;
        public int GetHashCode(string obj) => obj.GetHashCode();
    }
}
