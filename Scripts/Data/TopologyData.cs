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
    public List<Partition> Partitions { get; set; } = [];
    public Dictionary<long, Partition> PartitionsMap { get; set; } = [];

    public TopologyData(string id)
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
            if (!long.TryParse(splitName[0], out var x))
                x = 0;
            if (!long.TryParse(splitName[1], out var y))
                y = 0;
            
            var hash = GetHashCode(Id, x, y, 0);
            
            using var stream = entry.Open();
            var reader = new ExtendedDataInputStream(stream);
            var header = reader.ReadByte();
            var version = (sbyte)((header & VersionMask) >> VersionNumberPosition);
            var topologyMethod = (sbyte)((header & MethodMask) >> MethodNumberPosition);

            Partition partition = topologyMethod switch
            {
                MethodA => new TopologyMapA(),
                MethodB => new TopologyMapB(),
                MethodBi => new TopologyMapBi(),
                MethodC => new TopologyMapC(),
                MethodCi => new TopologyMapCi(),
                MethodDi => new TopologyMapDi(),
                _ => null
            };
            if (partition == null)
                continue;
            
            partition.Load(reader);
            Partitions.Add(partition);
        }
        Partitions = Partitions.OrderBy(p => p.X).ThenBy(p => p.Y).ToList();
    }

    public void Save()
    {
        
    }
    
    private long GetHashCode(int worldId, long x, long y, int instanceId)
    {
        x += 32767L;
        y += 32767L;
        return x << 48 | y << 32 |
               (long)((worldId & 65535) << 16) |
               (long)(instanceId & 65535);
    }

    public abstract class Partition
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

    public abstract class TopologyMapBlockedCells : Partition
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

    public class TopologyMapA : Partition
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

        public static short GetZIndex(CellPathData[] cellPathData, Partition partition, int x, int y, short z)
        {
            var pathData = partition.GetPathData(x, y, cellPathData, 0);

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
