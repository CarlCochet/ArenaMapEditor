using Godot;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Transactions;

public class TopologyData
{
    public const sbyte VersionMask = -16;
    public const sbyte MethodMask = 15;
    public const int VersionNumberPosition = 4;
    public const int MethodNumberPosition = 0;
    public const sbyte MethodA = 0;
    public const sbyte MethodB = 1;
    public const sbyte MethodBi = 2;
    public const sbyte MethodC = 3;
    public const sbyte MethodCi = 4;
    public const sbyte MethodDi = 5;
    
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
                MethodA => new TopologyMapA(),
                MethodB => new TopologyMapB(),
                MethodBi => new TopologyMapBi(),
                MethodC => new TopologyMapC(),
                MethodCi => new TopologyMapCi(),
                MethodDi => new TopologyMapDi(),
                _ => null
            };
            if (topologyMap == null)
                continue;
            
            topologyMap.Load(reader);
            var instance = new TopologyMapInstance(topologyMap);
            InstanceSet.AddMap(instance, x, y);
        }
    }

    public void Save()
    {
        
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
        public ByteArrayBitSet EntirelyBlockedCells { get; set; } = new(324);
        public ByteArrayBitSet UsedInFight { get; set; } = new(324);
        public TopologyMap TopologyMap { get; set; }
        public int NonBlockedCellsNumber { get; set; }
        public static CellPathData[] PathData { get; set; } = new CellPathData[32];

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

        public int GetMurFinType(int x, int y, int z)
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
        
        public bool IsUsedInFight(int x, int y)
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

        public void SetTopologyMap(TopologyMap topologyMap)
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

        public bool IsTopologyMapCellBlocked(int x, int y)
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

    public abstract class TopologyMap
    {
        public const int MaxZPerCells = 32;
        public const sbyte InfiniteCost = -1;
        public const sbyte DefaultCost = 7;
            
        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }
        
        public abstract int GetPathData(int x, int y, CellPathData[] cellPathData, int index);
        
        public abstract int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index);

        public virtual void Load(ExtendedDataInputStream reader)
        {
            X = reader.ReadShort() * MapConstants.MapWidth;
            Y = reader.ReadShort() * MapConstants.MapLength;
            Z = reader.ReadShort();
        }

        public virtual void Save(BinaryWriter writer)
        {
            
        }

        public bool IsInMap(int x, int y)
        {
            return x >= X && x < X + MapConstants.MapWidth && y >= Y && y < Y + MapConstants.MapLength;
        }

        public bool CheckPathData(int x, int y, CellPathData[] cellPathData)
        {
            if (cellPathData == null)
                return false;
            if (cellPathData.Length < 1)
                return false;
            if (cellPathData[0] == null)
                return false;
            return IsInMap(x, y);
        }

        public bool CheckVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData)
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

    public abstract class TopologyMapBlockedCells : TopologyMap
    {
        private readonly sbyte[] _blockedCells = new sbyte[ByteArrayBitSet.GetDataLength(MapConstants.NumCells)];
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            reader.ReadBytes(_blockedCells);
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

    public class TopologyMapA : TopologyMap
    {
        private sbyte Cost { get; set; }
        private sbyte MurFin { get; set; }
        private sbyte Property { get; set; }

        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            Cost = reader.ReadByte();
            MurFin = reader.ReadByte();
            Property = reader.ReadByte();
        }

        public override void Save(BinaryWriter writer)
        {
            
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

    public class TopologyMapB : TopologyMapBlockedCells
    {
        public sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        public sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        public sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        
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
        
        public override void Save(BinaryWriter writer)
        {
            
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

    public class TopologyMapBi : TopologyMapBlockedCells
    {
        private const int ZPosition = 0;
        private const byte CostMask = 48;
        private const byte ZMask = 15;
        private const byte NumZValues = 16;
        private const byte NumCosts = 4;
        
        public sbyte[] Costs { get; set; }
        public sbyte[] MurFins { get; set; }
        public sbyte[] Properties { get; set; }
        public int[] Cells { get; set; }
        
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
            Cells = TopologyIndexerHelper.CreateFor(Cells, cellSize, reader);
        }
        
        public override void Save(BinaryWriter writer)
        {
            
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

    public class TopologyMapC : TopologyMapBlockedCells
    {
        public const sbyte MovMask = 0x01;
        public const sbyte LosMask = 0x02;
        
        public sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        public sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        public sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        public short[] Zs { get; set; } = new short[MapConstants.NumCells];
        public sbyte[] Heights { get; set; } = new sbyte[MapConstants.NumCells];
        public sbyte[] MovLos { get; set; } = new sbyte[MapConstants.NumCells];
        
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
        
        public override void Save(BinaryWriter writer)
        {
            
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

    public class TopologyMapCi : TopologyMapBlockedCells
    {
        public const sbyte MovMask = 0x01;
        public const sbyte LosMask = 0x02;
        
        public sbyte[] Costs { get; set; }
        public sbyte[] MurFins { get; set; }
        public sbyte[] Properties { get; set; }
        public short[] Zs { get; set; }
        public sbyte[] Heights { get; set; }
        public sbyte[] MovLos { get; set; }
        public long[] Cells { get; set; }
        
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
        
        public override void Save(BinaryWriter writer)
        {
            
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

    public class TopologyMapDi : TopologyMapBlockedCells
    {
        private static List<int> _list = [];
        private static readonly Lock Mutex = new();
        
        public const sbyte MovMask = 0x01;
        public const sbyte LosMask = 0x02;
        public const int IndexOffset = 1;
        
        public sbyte[] Costs { get; set; }
        public sbyte[] MurFins { get; set; }
        public sbyte[] Properties { get; set; }
        public short[] Zs { get; set; }
        public sbyte[] Heights { get; set; }
        public sbyte[] MovLos { get; set; }
        public long[] Cells { get; set; }
        public int[] CellsWithMultiZ { get; set; }
        
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
        
        public override void Save(BinaryWriter writer)
        {
            
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
                var tab = GetMultiIndex(x - X, y - Y, _list);
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
                var tab = GetMultiIndex(x - X, y - Y, _list);
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

        private List<int> GetMultiIndex(int x, int y, List<int> list)
        {
            list.Clear();
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
                list.Add(index);
            }
            
            return list;
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

        public void SetData(CellPathData data)
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
}
