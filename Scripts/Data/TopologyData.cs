using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;

public class TopologyData
{
    private const sbyte VersionMask = -16;
    private const sbyte MethodMask = 15;
    private const int VersionNumberPosition = 4;
    private const int MethodNumberPosition = 0;
    private const sbyte MethodA = 0;
    private const sbyte MethodB = 1;
    private const sbyte MethodBi = 2;
    private const sbyte MethodC = 3;
    private const sbyte MethodCi = 4;
    private const sbyte MethodDi = 5;

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        // Converters = { new SByteArrayBase64Converter() }
    };
    
    public int Id { get; set; }
    public TopologyMapInstanceSet InstanceSet { get; set; }

    public TopologyData(string id)
    {
        if (!int.TryParse(id, out var worldId))
            return;
        Id = worldId;
        InstanceSet = new TopologyMapInstanceSet();
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
            
            var hash = GetHashCode(Id, x, y, 0);
            
            using var stream = entry.Open();
            var reader = new ExtendedDataInputStream(stream);
            var header = reader.ReadByte();
            var version = (sbyte)((header & VersionMask) >> VersionNumberPosition);
            var topologyMethod = (sbyte)((header & MethodMask) >> MethodNumberPosition);

            TopologyMap topologyMap = topologyMethod switch
            {
                MethodA => new TopologyMapA(header),
                MethodB => new TopologyMapB(header),
                MethodBi => new TopologyMapBi(header),
                MethodC => new TopologyMapC(header),
                MethodCi => new TopologyMapCi(header),
                MethodDi => new TopologyMapDi(header),
                _ => null
            };
            if (topologyMap == null)
                continue;
            
            topologyMap.Load(reader);
            var instance = new TopologyMapInstance(topologyMap);
            InstanceSet.AddMap(instance, x, y);
        }
    }

    public void Save(string path)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"topology_{Id}_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            foreach (var mapInstance in InstanceSet.Maps)
            {
                var topologyMap = mapInstance.TopologyMap;
                var mapX = topologyMap.X / MapConstants.MapWidth;
                var mapY = topologyMap.Y / MapConstants.MapLength;
                var fileName = $"{mapX}_{mapY}";
                var filePath = Path.Combine(tempDir, fileName);
            
                using var fileStream = File.Create(filePath);
                using var writer = new OutputBitStream(fileStream);
                writer.WriteByte(topologyMap.Header);
                topologyMap.Save(writer);
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

    public void SaveJson(string path)
    {
        var data = new
        {
            worldId = Id,
            topologyMap = InstanceSet.Maps.Select(m => m.TopologyMap).ToList()
        };

        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText($"{path}/{Id}.json", json);
    }

    public CellPathData[] GetPathData(int x, int y)
    {
        var topologyMap = InstanceSet.GetTopologyMap(x, y);
        if (topologyMap == null)
        {
            return null;
        }
        
        var tempPathData = new CellPathData[1];
        tempPathData[0] = new CellPathData();
        var zCount = topologyMap.GetPathData(x, y, tempPathData, 0);
        
        if (zCount == 0)
        {
            return null;
        }
        if (zCount == 1)
        {
            return tempPathData;
        }
        
        var cellPathData = new CellPathData[zCount];
        for (var i = 0; i < zCount; i++)
        {
            cellPathData[i] = new CellPathData();
            topologyMap.GetPathData(x, y, cellPathData, i);
        }
        
        return cellPathData;
    }
    
    public CellVisibilityData[] GetVisibilityData(int x, int y)
    {
        var topologyMap = InstanceSet.GetTopologyMap(x, y);
        if (topologyMap == null)
        {
            return null;
        }

        var tempVisibilityData = new CellVisibilityData[1];
        tempVisibilityData[0] = new CellVisibilityData();
        var zCount = topologyMap.GetVisibilityData(x, y, tempVisibilityData, 0);
        
        if (zCount == 0)
        {
            return null;
        }
        if (zCount == 1)
        {
            return tempVisibilityData;
        }
        
        var cellVisibilityData = new CellVisibilityData[zCount];
        for (var i = 0; i < zCount; i++)
        {
            cellVisibilityData[i] = new CellVisibilityData();
            topologyMap.GetVisibilityData(x, y, cellVisibilityData, i);
        }
        
        return cellVisibilityData;
    }
    
    private long GetHashCode(int worldId, long x, long y, int instanceId)
    {
        x += 32767L;
        y += 32767L;
        return x << 48 | y << 32 |
               (long)((worldId & 65535) << 16) |
               (long)(instanceId & 65535);
    }

    public class TopologyMapInstanceSet
    {
        public List<TopologyMapInstance> Maps { get; set; }
        public int MinX { get; set; }
        public int MinY { get; set; }
        public int MaxX { get; set; }
        public int MaxY { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public TopologyMapInstanceSet()
        {
            Maps = [];
            Reset();
        }
        
        public TopologyMap GetTopologyMap(int x, int y)
        {
            if (x < MinX || x >= MinX + Width || y < MinY || y >= MinY + Height)
            {
                return null;
            }
            var index = GetMapIndex(x, y);
            return index < 0 || index >= Maps.Count ? null : Maps[index]?.TopologyMap;
        }

        public bool IsInMap(int x, int y)
        {
            return x >= MinX && x < MaxX + Width && y >= MinY && y < MaxY + Height;
        }

        public bool IsMovementBlocked(int x, int y)
        {
            var index = GetMapIndex(x, y);
            return index >= 0 && index < Maps.Count && Maps[index]?.IsBlocked(x, y) == true;
        }

        public bool IsSightBlocked(int x, int y)
        {
            var index = GetMapIndex(x, y);
            return index >= 0 && index < Maps.Count && Maps[index]?.IsBlocked(x, y) == true;
        }

        public void Reset()
        {
            Maps.Clear();
            MinX = int.MaxValue;
            MinY = int.MaxValue;
            MaxX = int.MinValue;
            MaxY = int.MinValue;
            Width = 0;
            Height = 0;
        }

        public void AddMap(TopologyMapInstance mapInstance, int mapX, int mapY)
        {
            Maps.Add(mapInstance);
            mapX *= MapConstants.MapWidth;
            mapY *= MapConstants.MapLength;
            MinX = Math.Min(MinX, mapX);
            MinY = Math.Min(MinY, mapY);
            MaxX = Math.Max(MaxX, mapX);
            MaxY = Math.Max(MaxY, mapY);
            Width = MapConstants.MapWidth + MaxX - MinX;
            Height = MapConstants.MapLength + MaxY - MinY;
        }

        private int GetMapIndex(int x, int y)
        {
            if (x < MinX || y < MinY)
            {
                return -1;
            }
            
            var offsetX = (x - MinX) / MapConstants.MapWidth;
            var offsetY = (y - MinY) / MapConstants.MapLength;
            return offsetY * (Width / MapConstants.MapWidth) + offsetX;
        }
    }

    public class TopologyMapInstance
    {
        public TopologyMap TopologyMap { get; set; }
        private ByteArrayBitSet EntirelyBlockedCells { get; set; } = new(324);
        private ByteArrayBitSet UsedInFight { get; set; } = new(324);
        private int NonBlockedCellsNumber { get; set; }
        private static CellPathData[] PathData { get; set; } = new CellPathData[32];

        public TopologyMapInstance(TopologyMap topologyMap)
        {
            for (var i = 0; i < PathData.Length; i++)
            {
                PathData[i] = new CellPathData();
            }
            SetTopologyMap(topologyMap);
        }

        public bool IsBlocked(int x, int y)
        {
            x -= TopologyMap.X;
            y -= TopologyMap.Y;
            return EntirelyBlockedCells.Get(y * MapConstants.MapWidth + x);
        }

        private int GetMurFinType(int x, int y, int z)
        {
            if (!TopologyMap.IsInMap(x, y))
            {
                return 0;
            }
            
            var zCount = TopologyMap.GetPathData(x, y, PathData, 0);
            if (zCount == 0)
            {
                return 0;
            }

            for (var i = 0; i < zCount; i++)
            {
                if (PathData[i].Z == z)
                {
                    return PathData[i].GetMurFinType();
                }
            }
            return 0;
        }

        public bool IsIndoor(int x, int y, int z)
        {
            return CellPathData.IsIndoor(GetMurFinType(x, y, z));
        }

        private bool IsUsedInFight(int x, int y)
        {
            x -= TopologyMap.X;
            y -= TopologyMap.Y;
            return UsedInFight.Get(y * MapConstants.MapWidth + x);
        }

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (IsBlocked(x, y) == blocked)
            {
                return;
            }

            if (blocked)
            {
                x -= TopologyMap.X;
                y -= TopologyMap.Y;
                EntirelyBlockedCells.Set(y * MapConstants.MapWidth + x, true);
                NonBlockedCellsNumber--;
                return;
            }

            if (IsTopologyMapCellBlocked(x, y))
            {
                return;
            }
            
            x -= TopologyMap.X;
            y -= TopologyMap.Y;
            EntirelyBlockedCells.Set(y * MapConstants.MapWidth + x, false);
            NonBlockedCellsNumber++;
        }

        public void SetUsedInFight(int x, int y, bool usedInFight)
        {
            if (IsUsedInFight(x, y) == usedInFight)
            {
                return;
            }

            if (usedInFight)
            {
                x -= TopologyMap.X;
                y -= TopologyMap.Y;
                UsedInFight.Set(y * MapConstants.MapWidth + x, true);
                return;
            }

            if (IsBlocked(x, y))
            {
                return;
            }
            
            x -= TopologyMap.X;
            y -= TopologyMap.Y;
            UsedInFight.Set(y * MapConstants.MapWidth + x, false);
        }

        private void SetTopologyMap(TopologyMap topologyMap)
        {
            TopologyMap = topologyMap;
            EntirelyBlockedCells.SetAll(false);
            NonBlockedCellsNumber = 324;
            var x = TopologyMap.X;
            var y = TopologyMap.Y;
            var index = 0;

            for (var i = 0; i < MapConstants.MapLength; i++)
            {
                for (var j = 0; j < MapConstants.MapWidth; j++)
                {
                    if (IsTopologyMapCellBlocked(x + j, y + i))
                    {
                        EntirelyBlockedCells.Set(index, true);
                        NonBlockedCellsNumber--;
                    }
                    index++;
                }
            }
        }

        private bool IsTopologyMapCellBlocked(int x, int y)
        {
            var zCount = TopologyMap.GetPathData(x, y, PathData, 0);
            if (zCount == 1)
            {
                return PathData[0].Cost == -1;
            }

            for (var i = 0; i < zCount; i++)
            {
                if (PathData[i].Cost != -1)
                {
                    return false;
                }
            }
            return true;
        }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(TopologyMapA), "topologyMapA")]
    [JsonDerivedType(typeof(TopologyMapB), "topologyMapB")]
    [JsonDerivedType(typeof(TopologyMapBi), "topologyMapBi")]
    [JsonDerivedType(typeof(TopologyMapC), "topologyMapC")]
    [JsonDerivedType(typeof(TopologyMapCi), "topologyMapCi")]
    [JsonDerivedType(typeof(TopologyMapDi), "topologyMapDi")]
    public abstract class TopologyMap
    {
        protected const int MaxZPerCells = 32;
        protected const sbyte InfiniteCost = -1;
        protected const sbyte DefaultCost = 7;
     
        public sbyte Header { get; set; }
        
        [JsonPropertyName("posX")] public int X { get; set; }
        [JsonPropertyName("posY")] public int Y { get; set; }
        [JsonPropertyName("posZ")] public short Z { get; set; }
        
        public abstract int GetPathData(int x, int y, CellPathData[] cellPathData, int index);
        
        public abstract int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index);

        public virtual void Load(ExtendedDataInputStream reader)
        {
            X = reader.ReadShort() * MapConstants.MapWidth;
            Y = reader.ReadShort() * MapConstants.MapLength;
            Z = reader.ReadShort();
        }

        public virtual void Save(OutputBitStream writer)
        {
            writer.WriteShort((short) (X / MapConstants.MapWidth));
            writer.WriteShort((short) (Y / MapConstants.MapLength));
            writer.WriteShort(Z);
        }

        public bool IsInMap(int x, int y)
        {
            return x >= X && x < X + MapConstants.MapWidth && y >= Y && y < Y + MapConstants.MapLength;
        }

        protected bool CheckPathData(int x, int y, CellPathData[] cellPathData)
        {
            if (cellPathData == null)
                return false;
            if (cellPathData.Length < 1)
                return false;
            if (cellPathData[0] == null)
                return false;
            return IsInMap(x, y);
        }

        protected bool CheckVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData)
        {
            if (cellVisibilityData == null)
                return false;
            if (cellVisibilityData.Length < 1)
                return false;
            if (cellVisibilityData[0] == null)
                return false;
            return IsInMap(x, y);
        }
    }

    private abstract class TopologyMapBlockedCells : TopologyMap
    {
        [JsonPropertyName("blockedCells")] private readonly sbyte[] _blockedCells = new sbyte[ByteArrayBitSet.GetDataLength(MapConstants.NumCells)];
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            reader.ReadBytes(_blockedCells);
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteBytes(_blockedCells);
        }
        
        public void FillBlockedCells(ByteArrayBitSet bitSet)
        {
            Array.Copy(_blockedCells, 0, bitSet.GetByteArray(), 0, _blockedCells.Length);
        }

        public bool IsCellBlocked(int x, int y)
        {
            return ByteArrayBitSet.Get(_blockedCells, (y - Y) * MapConstants.MapWidth + x - X);
        }
    }

    private class TopologyMapA : TopologyMap
    {
        [JsonPropertyName("cost")] private sbyte Cost { get; set; }
        [JsonPropertyName("wallCell")] private sbyte MurFin { get; set; }
        [JsonPropertyName("property")] private sbyte Property { get; set; }

        public TopologyMapA(sbyte header)
        {
            Header = header;
        }

        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            Cost = reader.ReadByte();
            MurFin = reader.ReadByte();
            Property = reader.ReadByte();
        }

        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteByte(Cost);
            writer.WriteByte(MurFin);
            writer.WriteByte(Property);
        }

        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;
            
            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            pathData.Z = Z;
            pathData.Cost = Cost;
            pathData.CanMoveThrough = false;
            pathData.Height = 0;
            pathData.MurFinInfo = MurFin;
            pathData.MiscProperties = Property;
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
            return 1;
        }

        public void FillBlockedCells(ByteArrayBitSet bitSet)
        {
            bitSet.SetAll(Cost == InfiniteCost);
        }

        public bool IsCellBlocked(int x, int y)
        {
            return Cost == InfiniteCost;
        }
    }

    private class TopologyMapB : TopologyMapBlockedCells
    {
        [JsonPropertyName("costs")] private sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("wallCells")] private sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("properties")] private sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        
        public TopologyMapB(sbyte header)
        {
            Header = header;
        }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            for (var i = 0; i < MapConstants.NumCells; ++i)
            {
                Costs[i] = reader.ReadByte();
                MurFins[i] = reader.ReadByte();
                Properties[i] = reader.ReadByte();
            }
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);

            for (var i = 0; i < MapConstants.NumCells; ++i)
            {
                writer.WriteByte(Costs[i]);
                writer.WriteByte(MurFins[i]);
                writer.WriteByte(Properties[i]);
            }
        }

        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;
            
            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            pathData.Z = Z;
            pathData.CanMoveThrough = false;
            pathData.Height = 0;

            var cellIndex = GetIndex(x, y);
            pathData.Cost = Costs[cellIndex];
            pathData.MurFinInfo = MurFins[cellIndex];
            pathData.MiscProperties = Properties[cellIndex];
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
            return 1;
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            return yIndex * MapConstants.MapWidth + xIndex;
        }
    }

    private class TopologyMapBi : TopologyMapBlockedCells
    {
        [JsonPropertyName("costs")] private sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] private sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] private sbyte[] Properties { get; set; }
        [JsonPropertyName("cells")] private int[] Cells { get; set; }

        private sbyte _cellSize;
        
        public TopologyMapBi(sbyte header)
        {
            Header = header;
        }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);

            var indexSize = reader.ReadByte();
            Costs = new sbyte[indexSize];
            MurFins = new sbyte[indexSize];
            Properties = new sbyte[indexSize];

            for (var i = 0; i < indexSize; ++i)
            {
                Costs[i] = reader.ReadByte();
                MurFins[i] = reader.ReadByte();
                Properties[i] = reader.ReadByte();
            }

            var cellSize = reader.ReadByte() & 0xFF;
            Cells = new int[cellSize];
            Cells = TopologyIndexerHelper.CreateFor(Cells, cellSize, reader);
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            var indexSize = (sbyte)Costs.Length;
            writer.WriteByte(indexSize);

            for (var i = 0; i < indexSize; ++i)
            {
                writer.WriteByte(Costs[i]);
                writer.WriteByte(MurFins[i]);
                writer.WriteByte(Properties[i]);
            }
            
            writer.WriteByte(unchecked((sbyte)Cells.Length));
        }

        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;
            
            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            pathData.Z = Z;
            pathData.CanMoveThrough = false;
            pathData.Height = 0;

            var tab = GetIndex(x, y);
            pathData.Cost = Costs[tab];
            pathData.MurFinInfo = MurFins[tab];
            pathData.MiscProperties = Properties[tab];
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
            return 1;
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }
    }

    private class TopologyMapC : TopologyMapBlockedCells
    {
        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;

        [JsonPropertyName("costs")] private sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("wallCells")] private sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("properties")] private sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("movLos")] private sbyte[] MovLos { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("zs")] private short[] Zs { get; set; } = new short[MapConstants.NumCells];
        [JsonPropertyName("heights")] private sbyte[] Heights { get; set; } = new sbyte[MapConstants.NumCells];
        
        public TopologyMapC(sbyte header)
        {
            Header = header;
        }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);

            for (var i = 0; i < MapConstants.NumCells; ++i)
            {
                Costs[i] = reader.ReadByte();
                MurFins[i] = reader.ReadByte();
                Properties[i] = reader.ReadByte();
                Zs[i] = reader.ReadShort();
                Heights[i] = reader.ReadByte();
                MovLos[i] = reader.ReadByte();
            }
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);

            for (var i = 0; i < MapConstants.NumCells; ++i)
            {
                writer.WriteByte(Costs[i]);
                writer.WriteByte(MurFins[i]);
                writer.WriteByte(Properties[i]);
                writer.WriteShort(Zs[i]);
                writer.WriteByte(Heights[i]);
                writer.WriteByte(MovLos[i]);
            }
        }

        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;
            
            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            
            var cellIndex = GetIndex(x, y);
            pathData.Z = Zs[cellIndex];
            pathData.CanMoveThrough = (MovLos[cellIndex] & MovMask) == MovMask;
            pathData.Height = Heights[cellIndex];
            pathData.Cost = Costs[cellIndex];
            pathData.MurFinInfo = MurFins[cellIndex];
            pathData.MiscProperties = Properties[cellIndex];
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            
            var cellIndex = GetIndex(x, y);
            visibilityData.Z = Zs[cellIndex];
            visibilityData.CanViewThrough = (MovLos[cellIndex] & LosMask) == LosMask;
            visibilityData.Height = Heights[cellIndex];
            return 1;
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            return yIndex * MapConstants.MapWidth + xIndex;
        }
    }

    private class TopologyMapCi : TopologyMapBlockedCells
    {
        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;

        [JsonPropertyName("costs")] private sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] private sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] private sbyte[] Properties { get; set; }
        [JsonPropertyName("movLos")] private sbyte[] MovLos { get; set; }
        [JsonPropertyName("zs")] private short[] Zs { get; set; }
        [JsonPropertyName("heights")] private sbyte[] Heights { get; set; }
        [JsonPropertyName("cells")] private long[] Cells { get; set; }

        public TopologyMapCi(sbyte header)
        {
            Header = header;
        }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);

            var indexSize = reader.ReadByte() & 0xFF;
            Costs = new sbyte[indexSize];
            MurFins = new sbyte[indexSize];
            Properties = new sbyte[indexSize];
            Zs = new short[indexSize];
            Heights = new sbyte[indexSize];
            MovLos = new sbyte[indexSize];
            for (var i = 0; i < indexSize; ++i)
            {
                Costs[i] = reader.ReadByte();
                MurFins[i] = reader.ReadByte();
                Properties[i] = reader.ReadByte();
                Zs[i] = reader.ReadShort();
                Heights[i] = reader.ReadByte();
                MovLos[i] = reader.ReadByte();
            }
            
            var cellSize = reader.ReadByte() & 0xFF;
            Cells = TopologyIndexerHelper.CreateFor(Cells, cellSize, reader);
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);

            writer.WriteByte(unchecked((sbyte)Costs.Length));
            for (var i = 0; i < Costs.Length; ++i)
            {
                writer.WriteByte(Costs[i]);
                writer.WriteByte(MurFins[i]);
                writer.WriteByte(Properties[i]);
                writer.WriteShort(Zs[i]);
                writer.WriteByte(Heights[i]);
                writer.WriteByte(MovLos[i]);
            }
            
            writer.WriteByte(unchecked((sbyte)Cells.Length));
        }
        
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;

            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            
            var tab = GetIndex(x, y);
            pathData.Z = Zs[tab];
            pathData.CanMoveThrough = (MovLos[tab] & MovMask) == MovMask;
            pathData.Height = Heights[tab];
            pathData.Cost = Costs[tab];
            pathData.MurFinInfo = MurFins[tab];
            pathData.MiscProperties = Properties[tab];
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            
            var tab = GetIndex(x, y);
            visibilityData.Z = Zs[tab];
            visibilityData.CanViewThrough = (MovLos[tab] & LosMask) == LosMask;
            visibilityData.Height = Heights[tab];
            return 1;
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }
    }

    private class TopologyMapDi : TopologyMapBlockedCells
    {
        private static readonly List<int> Indexes = [];
        private static readonly Lock Mutex = new();

        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;
        private const int IndexOffset = 1;

        [JsonPropertyName("costs")] private sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] private sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] private sbyte[] Properties { get; set; }
        [JsonPropertyName("movLos")] private sbyte[] MovLos { get; set; }
        [JsonPropertyName("zs")] private short[] Zs { get; set; }
        [JsonPropertyName("heights")] private sbyte[] Heights { get; set; }
        [JsonPropertyName("cells")] private long[] Cells { get; set; }
        [JsonPropertyName("cellsWithMultiZ")] private int[] CellsWithMultiZ { get; set; }
        
        public TopologyMapDi(sbyte header)
        {
            Header = header;
        }
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            var indexSize = reader.ReadByte() & 0xFF;
            Costs = new sbyte[indexSize];
            MurFins = new sbyte[indexSize];
            Properties = new sbyte[indexSize];
            Zs = new short[indexSize];
            Heights = new sbyte[indexSize];
            MovLos = new sbyte[indexSize];
            for (var i = 0; i < indexSize; ++i)
            {
                Costs[i] = reader.ReadByte();
                MurFins[i] = reader.ReadByte();
                Properties[i] = reader.ReadByte();
                Zs[i] = reader.ReadShort();
                Heights[i] = reader.ReadByte();
                MovLos[i] = reader.ReadByte();
            }

            var cellCount = reader.ReadByte() & 0xFF;
            Cells = TopologyIndexerHelper.CreateFor(Cells, cellCount, reader); 
            
            var remainsCount = reader.ReadShort() & 0xFFFF;
            CellsWithMultiZ = new int[remainsCount];
            for (var i = 0; i < remainsCount; ++i)
            {
                CellsWithMultiZ[i] = reader.ReadInt();
            }
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteByte(unchecked((sbyte)Costs.Length));
            for (var i = 0; i < Costs.Length; ++i)
            {
                writer.WriteByte(Costs[i]);
                writer.WriteByte(MurFins[i]);
                writer.WriteByte(Properties[i]);
                writer.WriteShort(Zs[i]);
                writer.WriteByte(Heights[i]);
                writer.WriteByte(MovLos[i]);
            }
            
            writer.WriteByte(unchecked((sbyte)Cells.Length));
            writer.WriteShort(unchecked((short)CellsWithMultiZ.Length));

            foreach (var c in CellsWithMultiZ)
            {
                writer.WriteInt(c);
            }
        }
        
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            var cellIndex = GetIndex(x, y);
            if (cellIndex != 0)
            {
                var pathData = cellPathData[index];
                pathData.X = x;
                pathData.Y = y;
                FillPathData(pathData, cellIndex - IndexOffset);
                return 1;
            }

            using (Mutex.EnterScope())
            {
                var tab = GetMultiIndex(x - X, y - Y, Indexes);
                var zCount = tab.Count;
                for (var i = 0; i < zCount; ++i)
                {
                    var pathData = cellPathData[index + i];
                    pathData.X = x;
                    pathData.Y = y;
                    FillPathData(pathData, tab[i]);
                }
                return zCount;
            }
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            var cellIndex = GetIndex(x, y);
            if (cellIndex != 0)
            {
                var visibilityData = cellVisibilityData[index];
                visibilityData.X = x;
                visibilityData.Y = y;
                FillVisibilityData(visibilityData, cellIndex - IndexOffset);
                return 1;
            }

            using (Mutex.EnterScope())
            {
                var tab = GetMultiIndex(x - X, y - Y, Indexes);
                var zCount = tab.Count;
                for (var i = 0; i < zCount; ++i)
                {
                    var visibilityData = cellVisibilityData[index + i];
                    visibilityData.X = x;
                    visibilityData.Y = y;
                    FillVisibilityData(visibilityData, tab[i]);
                }
                return zCount;
            }
        }
        
        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }

        private List<int> GetMultiIndex(int x, int y, List<int> indexes)
        {
            indexes.Clear();
            var multiCount = CellsWithMultiZ.Length;

            for (var i = 0; i < multiCount; ++i)
            {
                var cellData = CellsWithMultiZ[i];
                var cy = (cellData >> 8) & 0xFF;
                if (cy < y)
                    continue;
                if (cy > y)
                    break;
                
                var cx = cellData & 0xFF;
                if (cx < x)
                    continue;
                if (cx > x)
                    break;
                
                var index = (cellData >> 16) & 0xFFFF;
                indexes.Add(index);
            }
            
            return indexes;
        }

        private void FillPathData(CellPathData data, int cellIndex)
        {
            data.Z = Zs[cellIndex];
            data.CanMoveThrough = (MovLos[cellIndex] & MovMask) == MovMask;
            data.Height = Heights[cellIndex];
            data.Cost = Costs[cellIndex];
            data.MurFinInfo = MurFins[cellIndex];
            data.MiscProperties = Properties[cellIndex];
        }
        
        private void FillVisibilityData(CellVisibilityData data, int cellIndex)
        {
            data.Z = Zs[cellIndex];
            data.CanViewThrough = (MovLos[cellIndex] & LosMask) == LosMask;
            data.Height = Heights[cellIndex];
        }
    }

    public class CellPathData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }
        public bool CanMoveThrough { get; set; }
        public sbyte Cost { get; set; }
        public sbyte Height { get; set; }
        public sbyte MurFinInfo { get; set; }
        public sbyte MiscProperties { get; set; }
        
        public CellPathData(){}

        public CellPathData(CellPathData data)
        {
            SetData(data);
        }

        private void SetData(CellPathData data)
        {
            X = data.X;
            Y = data.Y;
            Z = data.Z;
            CanMoveThrough = data.CanMoveThrough;
            Cost = data.Cost;
            Height = data.Height;
            MurFinInfo = data.MurFinInfo;
        }

        public static CellPathData[] CreateCellPathData()
        {
            var pathData = new CellPathData[32];

            for (var i = 0; i < pathData.Length; i++)
            {
                pathData[i] = new CellPathData();
            }
            
            return pathData;
        }

        public static short GetZIndex(CellPathData[] cellPathData, TopologyMap topologyMap, int x, int y, short z)
        {
            var pathData = topologyMap.GetPathData(x, y, cellPathData, 0);

            if (pathData == 1)
                return (short)(cellPathData[0].Z == z ? 0 : -1);

            for (var i = 0; i < pathData; i++)
            {
                if (cellPathData[i].Z != z || cellPathData[i].CanMoveThrough)
                    continue;
                return (short)i;
            }
            
            return -1;
        }

        public int GetMurFinType()
        {
            return MurFinInfo & 192;
        }

        public static bool IsIndoor(int murfinInfo)
        {
            return (murfinInfo & 128) == 128;
        }

        public static bool IsDoor(int murfinInfo)
        {
            return (murfinInfo & 64) == 64;
        }
    }

    public class CellVisibilityData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }
        public bool CanViewThrough { get; set; }
        public sbyte Height { get; set; }
    }
    
    public class SByteArrayBase64Converter : JsonConverter<sbyte[]>
    {
        public override sbyte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return null;
            }

            var base64String = reader.GetString();
            if (string.IsNullOrEmpty(base64String))
            {
                return [];
            }

            var bytes = Convert.FromBase64String(base64String);
            var sbyteArray = new sbyte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, sbyteArray, 0, bytes.Length);
            return sbyteArray;
        }

        public override void Write(Utf8JsonWriter writer, sbyte[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var bytes = new byte[value.Length];
            Buffer.BlockCopy(value, 0, bytes, 0, value.Length);
            var base64String = Convert.ToBase64String(bytes);
            writer.WriteStringValue(base64String);
        }
    }
}
