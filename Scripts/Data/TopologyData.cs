using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Transactions;

public class TopologyData
{
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
            using var reader = new BinaryReader(stream);
            var header = reader.ReadByte();
            var topologyType = (byte)((header & 15) >> 0);

            Partition partition = topologyType switch
            {
                0 => new TopologyMapA(Id),
                1 => new TopologyMapB(Id),
                2 => new TopologyMapBi(Id),
                3 => new TopologyMapM3(Id),
                5 => new TopologyMapM5(Id),
                6 => new TopologyMapM6(Id),
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

    public abstract class Partition(int id)
    {
        public int Id { get; set; } = id;
        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }
        
        protected const int ChunkSize = 18;
        
        public abstract int GetPathData(int x, int y, CellPathData[] cellPathData, int index);
        
        public abstract int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index);

        public virtual void Load(BinaryReader reader)
        {
            X = reader.ReadInt16() * ChunkSize;
            Y = reader.ReadInt16() * ChunkSize;
            Z = reader.ReadInt16();
        }

        public virtual void Save(BinaryWriter writer)
        {
            
        }

        public bool IsInMap(int x, int y)
        {
            return x >= X && x < X + ChunkSize && y >= Y && y < Y + ChunkSize;
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

    public class TopologyMapA(int id) : Partition(id)
    {
        private byte Cost { get; set; }

        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            Cost = reader.ReadByte();
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
            pathData.IsHollow = false;
            pathData.Height = 0;
            pathData.MurFinInfo = 0;
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
            visibilityData.IsHollow = false;
            visibilityData.Height = 0;
            return 1;
        }
    }

    public class TopologyMapB(int id) : Partition(id)
    {
        private const byte NumZValues = 8;
        private const byte NumCosts = 2;
        
        public short[] Properties { get; set; } = new short[NumZValues];
        public byte[] Costs { get; set; } = new byte[NumCosts];
        public byte[] Cells { get; set; } = new byte[162];

        
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);

            for (var i = 0; i < Properties.Length; i++)
            {
                Properties[i] = (short)(reader.ReadInt16() + Z);
            }

            for (var i = 0; i < Costs.Length; i++)
            {
                Costs[i] = reader.ReadByte();
            }

            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i] = reader.ReadByte();
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
            pathData.IsHollow = false;
            pathData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var cell = (byte)((index2 & 1) != 0 ?
                Cells[index2 >>> 1] & 255 & 15 :
                (Cells[index2 >>> 1] & 255) >>> 4 & 15);
            
            pathData.Cost = Costs[cell & 1];

            if (cell >>> 1 >= NumZValues)
                return 0;

            pathData.Z = Properties[cell >>> 1];
            pathData.MurFinInfo = 0;
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.IsHollow = false;
            visibilityData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var cell = (byte)((index2 & 1) != 0 ?
                Cells[index2 >>> 1] & 255 & 15 :
                (Cells[index2 >>> 1] & 255) >>> 4 & 15);
            
            visibilityData.Z = Properties[cell >>> 1];
            return 1;
        }
    }

    public class TopologyMapBi(int id) : Partition(id)
    {
        private const int ZPosition = 0;
        private const byte CostMask = 48;
        private const byte ZMask = 15;
        private const byte NumZValues = 16;
        private const byte NumCosts = 4;
        
        public short[] Properties { get; set; } = new short[NumZValues];
        public byte[] Costs { get; set; } = new byte[NumCosts];
        public byte[] MurFins { get; set; } = new byte[324];
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);

            for (var i = 0; i < Properties.Length; i++)
            {
                Properties[i] = (short)(reader.ReadInt16() + Z);
            }

            for (var i = 0; i < Costs.Length; i++)
            {
                Costs[i] = reader.ReadByte();
            }

            for (var i = 0; i < MurFins.Length; i++)
            {
                MurFins[i] = reader.ReadByte();
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
            pathData.IsHollow = false;
            pathData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var murFin = MurFins[index2];
            
            pathData.Z = Properties[(murFin & ZMask) >>> ZPosition];
            pathData.Cost = Costs[(murFin & CostMask) >>> NumCosts];
            pathData.MurFinInfo = 0;
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.IsHollow = false;
            visibilityData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var murFin = MurFins[index2];
            
            visibilityData.Z = Properties[(murFin & ZMask) >>> ZPosition];
            return 1;
        }
    }

    public class TopologyMapM3(int id) : Partition(id)
    {
        private const short CostMask = -4096;
        private const short ViewMask = 2048;
        private const short MoveMask = 1024;
        private const short ZMask = 1023;
        private const int CostPosition = 12;
        private const int VisibilityPosition = 11;
        private const int MovePosition = 10;
        private const int ZPosition = 0;
        private const short ZShift = 512;
        private const short ZMin = -32768;
        
        public short[] Cells { get; set; } = new short[324];
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);

            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i] = reader.ReadByte();
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
            pathData.IsHollow = false;
            pathData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var cell = Cells[index2];
            
            pathData.Cost = (byte)((cell & CostMask) >>> CostPosition);
            var zOffset = (cell & ZMask) >>> ZPosition;
            pathData.Z = (short)(zOffset != 0 ? Z - ZShift + zOffset : ZMin);
            pathData.MurFinInfo = 0;
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.IsHollow = false;
            visibilityData.Height = 0;

            var xDiff = x - X;
            var yDiff = y - Y;
            var index2 = yDiff * ChunkSize + xDiff;
            var cell = Cells[index2];

            visibilityData.Z = (short)(Z - ZShift + ((cell & ZMask) >>> ZPosition));
            return 1;
        }
    }

    public class TopologyMapM5(int id) : Partition(id)
    {
        private const int HeightMask = -67108864;
        private const int CostMask = 62914560;
        private const int ViewMask = 2097152;
        private const int MoveMask = 1048576;
        private const int ZMask = 1047552;
        private const int YMask = 992;
        private const int XMask = 31;
        private const int HeightPosition = 26;
        private const int CostPosition = 22;
        private const int ViewPosition = 21;
        private const int MovePosition = 20;
        private const int ZPosition = 10;
        private const int YPosition = 5;
        private const int XPosition = 0;
        private const short ZShift = 512;
        private const byte NumHeights = 64;
        private const short ZMin = -32768;
        
        public byte[] Heights { get; set; } = new byte[NumHeights];
        public int[] Cells { get; set; }
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);

            for (var i = 0; i < Heights.Length; i++)
            {
                Heights[i] = reader.ReadByte();
            }
            
            var cellCount = reader.ReadInt16();
            Cells = new int[cellCount];
            
            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i] = reader.ReadInt32();
            }
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
        
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return 0;

            var (index1, index2) = ComputeIndices(x, y);

            if (index1 + index < cellPathData.Length)
                return 0;

            for (var index3 = 0; index3 < index1; index3++)
            {
                var cell = Cells[index2 + index3];
                var pathData = cellPathData[index + index3];
                var zOffset = (cell & ZMask) >>> ZPosition;
                
                pathData.X = x;
                pathData.Y = y;
                pathData.Z = (short)(zOffset != 0 ? Z - ZShift + zOffset : ZMin);
                pathData.Cost = (byte)((cell & CostMask) >>> CostPosition);
                pathData.Cost = pathData.Cost == 15 ? unchecked((byte)-1) : pathData.Cost;
                pathData.IsHollow = (cell & MoveMask) >>> MovePosition != 0;
                pathData.Height = Heights[(cell & HeightMask) >>> HeightPosition];
                pathData.MurFinInfo = 0;
            }
            
            return index1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return 0;
            
            var (index1, index2) = ComputeIndices(x, y);

            if (index1 + index < cellVisibilityData.Length)
                return 0;

            for (var index3 = 0; index3 < index1; index3++)
            {
                var cell = Cells[index2 + index3];
                var visibilityData = cellVisibilityData[index + index3];
                
                visibilityData.X = x;
                visibilityData.Y = y;
                visibilityData.Z = (short)(Z - ZShift + ((cell & ZMask) >>> ZPosition));
                visibilityData.Height = Heights[(cell & HeightMask) >>> HeightPosition];
                visibilityData.IsHollow = (cell & ViewMask) >>> ViewPosition != 0;
            }
            
            return index1;
        }

        private (int index1, int index2) ComputeIndices(int x, int y)
        {
            var xDiff = x - X;
            var yDiff = y - Y;
            
            var index1 = 1;
            var index2 = 0;
            var index3 = Cells.Length - 1;
            var index4 = -1;

            do
            {
                var cellIndex = index3 + index2 >>> 1;
                int cell, xTemp, yTemp;

                if (index2 + 1 == index3)
                {
                    cell = Cells[index2];
                    yTemp = (cell & YMask) >>> YPosition;
                    xTemp = (cell & XMask) >>> XPosition;
                    index4 = xDiff == xTemp && yDiff == yTemp ? index2 : index3; 
                    continue;
                }
                
                cell = Cells[cellIndex];
                yTemp = (cell & YMask) >>> YPosition;
                xTemp = (cell & XMask) >>> XPosition;

                if (yTemp > yDiff)
                {
                    index3 = cellIndex;
                    continue;
                }
                if (yTemp < yDiff)
                {
                    index2 = cellIndex;
                    continue;
                }
                if (xTemp > xDiff)
                {
                    index3 = cellIndex;
                    continue;
                }
                if (xTemp < xDiff)
                {
                    index2 = cellIndex;
                    continue;
                }
                index4 = cellIndex;
            } 
            while (index4 == -1);

            for (index2 = index4; index2 - index1 >= 0; index1++)
            {
                var cell = Cells[index4 - index1];
                var yTemp = (cell & YMask) >>> YPosition;
                var xTemp = (cell & XMask) >>> XPosition;
                
                if (xTemp != xDiff || yTemp != yDiff)
                    break;
            }

            for (index2 = index2 + 1 - index1; index4 + 1 < Cells.Length; index1++)
            {
                var cell = Cells[++index4];
                var yTemp = (cell & YMask) >>> YPosition;
                var xTemp = (cell & XMask) >>> XPosition;
                
                if (xTemp != xDiff || yTemp != yDiff)
                    break;
            }
            
            return (index1, index2);
        }
    }

    public class TopologyMapM6(int id) : Partition(id)
    {
        private const int HeightMask = -16777216;
        private const int CostMask = 15728640;
        private const int ZMask = 1047552;
        private const int YMask = 992;
        private const int XMask = 31;
        private const int HeightPosition = 24;
        private const int CostPosition = 20;
        private const int ZPosition = 10;
        private const int YPosition = 5;
        private const int XPosition = 0;
        private const short ZShift = 512;
        private const short ZMin = -32768;
        
        public int[] Cells { get; set; }
        public byte[] MoveAndVisibilities { get; set; }
        public byte[] MurFinInfo { get; set; }
        
        public override void Load(BinaryReader reader)
        {
            base.Load(reader);
            
            var cellCount = reader.ReadInt16();
            Cells = new int[cellCount];
            MoveAndVisibilities = new byte[cellCount + 1 >>> 1];
            MurFinInfo = new byte[cellCount];
            
            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i] = reader.ReadInt32();
            }

            for (var i = 0; i < MoveAndVisibilities.Length; i++)
            {
                MoveAndVisibilities[i] = reader.ReadByte();
            }

            for (var i = 0; i < MurFinInfo.Length; i++)
            {
                MurFinInfo[i] = reader.ReadByte();
            }
        }
        
        public override void Save(BinaryWriter writer)
        {
            
        }
        
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            var index1 = 1;
            var index2 = 0;
            var index3 = Cells.Length - 1;
            
            while (index3 != index2)
            {
                (index1, index2, index3, var index4) = ComputeIndices1(x, y, index1, index2, index3);

                if (index4 == -1) 
                    continue;
                
                (index1, index2) = ComputeIndices2(x, y, index1, index4);

                for (var index5 = 0; index5 < index1; index5++)
                {
                    index4 = index2 + index5;
                    var cell = Cells[index4];
                    var pathData = cellPathData[index + index5];
                    var zOffset = (cell & ZMask) >>> ZPosition;
                    
                    pathData.X = x;
                    pathData.Y = y;
                    pathData.Z = (short)(zOffset != 0 ? Z - ZShift + zOffset : ZMin);
                    pathData.Height = (byte)((cell & HeightMask) >>> HeightPosition);
                    pathData.Cost = (byte)((cell & CostMask) >>> CostPosition);
                    pathData.Cost = pathData.Cost == 15 ? unchecked((byte)-1) : pathData.Cost;
                    pathData.IsHollow = (index4 & 1) == 0 ?
                        (MoveAndVisibilities[index4 >>> 1] >>> 4 & 1) != 0 :
                        (MoveAndVisibilities[index4 >>> 1] & 1) != 0;
                    pathData.MurFinInfo = MurFinInfo[index4];
                }

                return index1;
            }
            return 0;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            var index1 = 1;
            var index2 = 0;
            var index3 = Cells.Length - 1;
            
            while (index3 != index2)
            {
                (index1, index2, index3, var index4) = ComputeIndices1(x, y, index1, index2, index3);

                if (index4 == -1) 
                    continue;
                
                (index1, index2) = ComputeIndices2(x, y, index1, index4);

                for (var index5 = 0; index5 < index1; index5++)
                {
                    index4 = index2 + index5;
                    var cell = Cells[index4];
                    var visibilityData = cellVisibilityData[index + index5];

                    visibilityData.X = x;
                    visibilityData.Y = y;
                    visibilityData.Z = (short)(Z - ZShift + (cell & ZMask) >>> ZPosition);
                    visibilityData.Height = (byte)((cell & HeightMask) >>> HeightPosition);
                    visibilityData.IsHollow = (index4 & 1) == 0 ?
                        (MoveAndVisibilities[index4 >>> 1] >>> 4 & 2) != 0 :
                        (MoveAndVisibilities[index4 >>> 1] & 2) != 0;
                }

                return index1;
            }
            return 0;
        }

        private (int index1, int index2, int index3, int index4) ComputeIndices1(int x, int y, int index1, int index2, int index3)
        {
            var index4 = -1;
            var xDiff = x - X;
            var yDiff = y - Y;
            
            var cellIndex = index3 + index2 >>> 1;
            int cell, xTemp, yTemp;

            if (index2 + 1 == index3)
            {
                cell = Cells[index2];
                yTemp = (cell & YMask) >>> YPosition;
                xTemp = (cell & XMask) >>> XPosition;
                index4 = xDiff == xTemp && yDiff == yTemp ? index2 : index3; 
                return (index1, index2, index3, index4);
            }
                
            cell = Cells[cellIndex];
            yTemp = (cell & YMask) >>> YPosition;
            xTemp = (cell & XMask) >>> XPosition;

            if (yTemp > yDiff)
            {
                index3 = cellIndex;
                return (index1, index2, index3, index4);
            }
            if (yTemp < yDiff)
            {
                index2 = cellIndex;
                return (index1, index2, index3, index4);
            }
            if (xTemp > xDiff)
            {
                index3 = cellIndex;
                return (index1, index2, index3, index4);
            }
            if (xTemp < xDiff)
            {
                index2 = cellIndex;
                return (index1, index2, index3, index4);
            }
            index4 = cellIndex;
            
            return (index1, index2, index3, index4);
        }

        private (int index1, int index2) ComputeIndices2(int x, int y, int index1, int index4)
        {
            var xDiff = x - X;
            var yDiff = y - Y;
            int index2;
            
            for (index2 = index4; index2 - index1 >= 0; index1++)
            {
                var cell = Cells[index4 - index1];
                var yTemp = (cell & YMask) >>> YPosition;
                var xTemp = (cell & XMask) >>> XPosition;
                    
                if (xTemp != xDiff || yTemp != yDiff)
                    break;
            }

            for (index2 = index2 + 1 - index1; index4 + 1 < Cells.Length; index1++)
            {
                var cell = Cells[++index4];
                var yTemp = (cell & YMask) >>> YPosition;
                var xTemp = (cell & XMask) >>> XPosition;
                    
                if (xTemp != xDiff || yTemp != yDiff)
                    break;
            }
            
            return (index1, index2);
        }
    }

    public class CellPathData
    {
        public int X { get; set; }
        public int Y { get; set; }
        public short Z { get; set; }
        public bool IsHollow { get; set; }
        public byte Cost { get; set; }
        public byte Height { get; set; }
        public byte MurFinInfo { get; set; } = 0;
        
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
            IsHollow = data.IsHollow;
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
                if (cellPathData[i].Z != z || cellPathData[i].IsHollow)
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
        public bool IsHollow { get; set; }
        public byte Height { get; set; }
    }
}
