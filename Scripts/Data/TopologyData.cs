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

    public TopologyData(string path, string id)
    {
        Load(path, id);
    }

    public void Load(string path, string id)
    {
        using var archive = ZipFile.OpenRead($"{path}/tplg/{id}.jar");
        if (!int.TryParse(id, out var worldId))
            return;
        Id = worldId;
        
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
        
        public abstract int GetPathData(int x, int y, CellPathData[] cellPathData, int index);
        
        public abstract int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index);

        public virtual void Load(BinaryReader reader)
        {
            X = reader.ReadInt32() * 18;
            Y = reader.ReadInt32() * 18;
            Z = reader.ReadInt16();
        }

        public bool IsInMap(int x, int y)
        {
            return x >= X && x < X + 18 && y >= Y && y < Y + 18;
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

        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            CheckPathData(x, y, cellPathData);
            return 1;
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class TopologyMapB(int id) : Partition(id)
    {
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            throw new NotImplementedException();
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class TopologyMapBi(int id) : Partition(id)
    {
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            throw new NotImplementedException();
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class TopologyMapM3(int id) : Partition(id)
    {
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            throw new NotImplementedException();
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class TopologyMapM5(int id) : Partition(id)
    {
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            throw new NotImplementedException();
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
        }
    }

    public class TopologyMapM6(int id) : Partition(id)
    {
        public override int GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            throw new NotImplementedException();
        }

        public override int GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            throw new NotImplementedException();
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
