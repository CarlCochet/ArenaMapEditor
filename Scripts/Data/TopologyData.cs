using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using FileAccess = Godot.FileAccess;

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
        Converters = { new SByteArrayBase64Converter(), new ShortArrayCompactConverter() }
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

            var hash = GetHashCode(Id, x, y, 0);
            
            using var stream = entry.Open();
            var reader = new ExtendedDataInputStream(stream);
            var header = reader.ReadByte();
            var version = (sbyte)((header & VersionMask) >> VersionNumberPosition);
            var topologyMethod = (sbyte)((header & MethodMask) >> MethodNumberPosition);
            
            if (topologyMethod == MethodA)
                continue;

            TopologyMap topologyMap = topologyMethod switch
            {
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
            InstanceSet.AddMap(instance);
        }
        InstanceSet.Sort();
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

            var hash = GetHashCode(Id, x, y, 0);

            var header = reader.ReadByte();
            var version = (sbyte)((header & VersionMask) >> VersionNumberPosition);
            var topologyMethod = (sbyte)((header & MethodMask) >> MethodNumberPosition);

            if (topologyMethod == MethodA)
            {
                fileName = dirAccess.GetNext();
                continue;
            }
                

            TopologyMap topologyMap = topologyMethod switch
            {
                MethodB => new TopologyMapB(),
                MethodBi => new TopologyMapBi(),
                MethodC => new TopologyMapC(),
                MethodCi => new TopologyMapCi(),
                MethodDi => new TopologyMapDi(),
                _ => null
            };
            if (topologyMap == null)
            {
                fileName = dirAccess.GetNext();
                continue;
            }
            
            topologyMap.Load(reader);
            var instance = new TopologyMapInstance(topologyMap);
            InstanceSet.AddMap(instance);

            fileName = dirAccess.GetNext();
        }
        dirAccess.ListDirEnd();

        InstanceSet.Sort();  
    }

    public void GenerateFromGfx(GfxData gfxData)
    {
        InstanceSet.Reset();
        var elements = gfxData.Partitions.SelectMany(p => p.Elements);
        foreach (var element in elements)
        {
            AddFromElement(element);
        }
    }
    
    public void AddFromElement(GfxData.Element element)
    {
        if (GlobalData.Instance.IgnoreGfxIds.Contains(element.CommonData.GfxId))
            return;
        
        var topoC = InstanceSet.GetTopologyMap(element.CellX, element.CellY) ?? InstanceSet.CreateTopologyMap(element.CellX, element.CellY);
        topoC.AddElement(element);
    }

    public void Save(string path)
    {
        foreach (var mapInstance in InstanceSet.Maps)
        {
            var topologyMap = mapInstance.TopoC;
            if (topologyMap == null)
                continue;
            
            var mapX = topologyMap.X / MapConstants.MapWidth;
            var mapY = topologyMap.Y / MapConstants.MapLength;
            var filePath = Path.Combine(path, $"{mapX}_{mapY}");
            
            using var fileStream = File.Create(filePath);
            using var writer = new OutputBitStream(fileStream);
            writer.WriteByte(topologyMap.Header);
            topologyMap.Save(writer);
        }
    }

    public void SaveJson(string path)
    {
        var data = new
        {
            worldId = Id,
            topologyMap = InstanceSet.Maps
                .Select(m => m.TopoC)
                .Where(m => m != null)
                .ToList()
        };

        var json = JsonSerializer.Serialize(data, _options);
        File.WriteAllText($"{path}/map_{Id}.json", json);
    }

    public CellPathData GetPathData(int x, int y)
    {
        var topoC = InstanceSet.GetTopologyMap(x, y);
        if (topoC == null)
            return null;
        CellPathData[] path = [new()];
        topoC.GetPathData(x, y, path, 0);
        return path[0];
    }
    
    public CellVisibilityData GetVisibilityData(int x, int y)
    {
        var topoC = InstanceSet.GetTopologyMap(x, y);
        if (topoC == null)
            return null;
        CellVisibilityData[] visibility = [new()];
        topoC.GetVisibilityData(x, y, visibility, 0);
        return visibility[0];
    }

    public void Update(CellPathData pathData, CellVisibilityData visibilityData)
    {
        var topoC = InstanceSet.GetTopologyMap(pathData.X, pathData.Y);
        topoC?.UpdateData(pathData, visibilityData);
        InstanceSet.PruneEmptyMaps();
    }

    public void ResetTile(int x, int y)
    {
        var pathData = GetPathData(x, y);
        var visibilityData = GetVisibilityData(x, y);
        pathData.Z = short.MinValue;
        pathData.Height = 0;
        pathData.CanMoveThrough = false;
        pathData.Cost = -1;
        pathData.MiscProperties = 0;
        pathData.MurFinInfo = 0;
        visibilityData.Z = short.MinValue;
        visibilityData.Height = 0;
        visibilityData.CanViewThrough = false;
        Update(pathData, visibilityData);
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
        
        public TopologyMapC GetTopologyMap(int x, int y)
        {
            if (Maps.Count == 0)
                return null;
            if (x < MinX || x >= MinX + Width || y < MinY || y >= MinY + Height)
                return null;
            
            var index = GetMapIndex(x, y);
            return index < 0 || index >= Maps.Count ? null : Maps[index]?.TopoC;
        }

        public TopologyMapC CreateTopologyMap(int x, int y)
        {
            var topologyMap = new TopologyMapC(
                (int)Math.Floor((float)x / MapConstants.MapWidth) * MapConstants.MapWidth,
                (int)Math.Floor((float)y / MapConstants.MapLength) * MapConstants.MapLength);
            var instance = new TopologyMapInstance(topologyMap);
            AddMap(instance);
            return topologyMap;
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

        public void AddMap(TopologyMapInstance mapInstance)
        {
            Maps.Add(mapInstance);
            MinX = Math.Min(MinX, mapInstance.TopoC.X);
            MinY = Math.Min(MinY, mapInstance.TopoC.Y);
            MaxX = Math.Max(MaxX, mapInstance.TopoC.X);
            MaxY = Math.Max(MaxY, mapInstance.TopoC.Y);
            Width = MapConstants.MapWidth + MaxX - MinX;
            Height = MapConstants.MapLength + MaxY - MinY;
        }

        public void Sort()
        {
            Maps = Maps.OrderBy(instance => instance.TopoC.Y)
                .ThenBy(instance => instance.TopoC.X)
                .ToList();
        }

        public void PruneEmptyMaps()
        {
            Maps = Maps.Where(m => !m.IsEmpty()).ToList();
            RecomputeBounds();
        }

        private int GetMapIndex(int x, int y)
        {
            if (x < MinX || y < MinY)
                return -1;

            for (var index = 0; index < Maps.Count; index++)
            {
                var mapX = Maps[index].TopoC.X;
                var mapY = Maps[index].TopoC.Y;
                if (x >= mapX && x < mapX + MapConstants.MapWidth && y >= mapY && y < mapY + MapConstants.MapLength)
                    return index;
            }

            return -1;
        }

        private void RecomputeBounds()
        {
            Reset();
            foreach (var map in Maps)
            {
                MinX = Math.Min(MinX, map.TopoC.X);
                MinY = Math.Min(MinY, map.TopoC.Y);
                MaxX = Math.Max(MaxX, map.TopoC.X);
                MaxY = Math.Max(MaxY, map.TopoC.Y);
            }
            Width = MapConstants.MapWidth + MaxX - MinX;
            Height = MapConstants.MapLength + MaxY - MinY;
        }
    }

    public class TopologyMapInstance
    {
        public TopologyMapC TopoC { get; set; }
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
            x -= TopoC.X;
            y -= TopoC.Y;
            return EntirelyBlockedCells.Get(y * MapConstants.MapWidth + x);
        }

        private int GetMurFinType(int x, int y, int z)
        {
            if (!TopoC.IsInMap(x, y))
                return 0;

            var zCount = TopoC.GetZCount(x, y);
            if (zCount == 0)
                return 0;

            for (var i = 0; i < zCount; i++)
            {
                if (PathData[i].Z == z)
                    return PathData[i].GetMurFinType();
            }
            return 0;
        }

        public bool IsIndoor(int x, int y, int z)
        {
            return CellPathData.IsIndoor(GetMurFinType(x, y, z));
        }

        private bool IsUsedInFight(int x, int y)
        {
            x -= TopoC.X;
            y -= TopoC.Y;
            return UsedInFight.Get(y * MapConstants.MapWidth + x);
        }

        public void SetBlocked(int x, int y, bool blocked)
        {
            if (IsBlocked(x, y) == blocked)
                return;

            if (blocked)
            {
                x -= TopoC.X;
                y -= TopoC.Y;
                EntirelyBlockedCells.Set(y * MapConstants.MapWidth + x, true);
                NonBlockedCellsNumber--;
                return;
            }

            if (IsTopologyMapCellBlocked(x, y))
                return;
            
            x -= TopoC.X;
            y -= TopoC.Y;
            EntirelyBlockedCells.Set(y * MapConstants.MapWidth + x, false);
            NonBlockedCellsNumber++;
        }

        public void SetUsedInFight(int x, int y, bool usedInFight)
        {
            if (IsUsedInFight(x, y) == usedInFight)
                return;

            if (usedInFight)
            {
                x -= TopoC.X;
                y -= TopoC.Y;
                UsedInFight.Set(y * MapConstants.MapWidth + x, true);
                return;
            }

            if (IsBlocked(x, y))
                return;
            
            x -= TopoC.X;
            y -= TopoC.Y;
            UsedInFight.Set(y * MapConstants.MapWidth + x, false);
        }

        public bool IsEmpty()
        {
            return TopoC.IsEmpty();
        }

        private void SetTopologyMap(TopologyMap topologyMap)
        {
            TopoC = topologyMap.ConvertToC();
            if (TopoC == null)
                return;
            
            EntirelyBlockedCells.SetAll(false);
            NonBlockedCellsNumber = 324;
            var x = TopoC.X;
            var y = TopoC.Y;
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
            var zCount = TopoC.GetZCount(x, y);
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
     
        [JsonIgnore] public sbyte Header { get; set; }
        
        [JsonPropertyName("posX")] public int X { get; set; }
        [JsonPropertyName("posY")] public int Y { get; set; }
        [JsonPropertyName("posZ")] public short Z { get; set; }
        
        public abstract void GetPathData(int x, int y, CellPathData[] cellPathData, int index);
        
        public abstract void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index);
        
        public abstract int GetZCount(int x, int y);
        
        public abstract void UpdateData(CellPathData pathData, CellVisibilityData visibilityData);

        public abstract TopologyMapC ConvertToC();

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

    public abstract class TopologyMapBlockedCells : TopologyMap
    {
        [JsonPropertyName("blockedCells")] public sbyte[] BlockedCells { get; } = new sbyte[ByteArrayBitSet.GetDataLength(MapConstants.NumCells)];
        
        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            reader.ReadBytes(BlockedCells);
        }
        
        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteBytes(BlockedCells);
        }
        
        public void FillBlockedCells(ByteArrayBitSet bitSet)
        {
            Array.Copy(BlockedCells, 0, bitSet.GetByteArray(), 0, BlockedCells.Length);
        }

        public bool IsCellBlocked(int x, int y)
        {
            return ByteArrayBitSet.Get(BlockedCells, (y - Y) * MapConstants.MapWidth + x - X);
        }
    }

    public class TopologyMapA : TopologyMap
    {
        [JsonPropertyName("cost")] public sbyte Cost { get; set; }
        [JsonPropertyName("wallCell")] public sbyte MurFin { get; set; }
        [JsonPropertyName("property")] public sbyte Property { get; set; }
        [JsonPropertyName("type")] public string Type => "topologyMapA";

        public TopologyMapA()
        {
            Header = (2 << 4) | MethodA;
        }

        public override void Load(ExtendedDataInputStream reader)
        {
            base.Load(reader);
            
            Cost = reader.ReadByte();
            MurFin = reader.ReadByte();
            Property = reader.ReadByte();
        }

        public void LoadFromGfx(GfxData.Partition partition)
        {
            X = partition.X;
            Y = partition.Y;
            Z = short.MinValue;
            Cost = InfiniteCost;
            MurFin = 0;
            Property = 0;
        }

        public override void Save(OutputBitStream writer)
        {
            base.Save(writer);
            
            writer.WriteByte(Cost);
            writer.WriteByte(MurFin);
            writer.WriteByte(Property);
        }

        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return;
            
            var pathData = cellPathData[index];
            pathData.X = x;
            pathData.Y = y;
            pathData.Z = Z;
            pathData.Cost = Cost;
            pathData.CanMoveThrough = false;
            pathData.Height = 0;
            pathData.MurFinInfo = MurFin;
            pathData.MiscProperties = Property;
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
        }
        
        public override int GetZCount(int x, int y)
        {
            return 1;
        }

        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {
            Cost = pathData.Cost;
            MurFin = pathData.MurFinInfo;
            Property = pathData.MiscProperties;
        }

        public override TopologyMapC ConvertToC()
        {
            return null;
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
        [JsonPropertyName("costs")] public sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("wallCells")] public sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("properties")] public sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("type")] public string Type => "topologyMapB";
        
        public TopologyMapB()
        {
            Header = (2 << 4) | MethodB;
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

        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return;
            
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
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
        }
        
        public override int GetZCount(int x, int y)
        {
            return 1;
        }
        
        public override TopologyMapC ConvertToC()
        {
            return new TopologyMapC
            {
                X = X,
                Y = Y,
                Z = Z,
                Costs = Costs,
                MurFins = MurFins,
                Properties = Properties,
                MovLos = new sbyte[MapConstants.NumCells],
                Zs = new short[MapConstants.NumCells], 
                Heights = new sbyte[MapConstants.NumCells]
            };
        }
        
        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {
            var cellIndex = GetIndex(pathData.X, pathData.Y);
            Costs[cellIndex] = pathData.Cost;
            MurFins[cellIndex] = pathData.MurFinInfo;
            Properties[cellIndex] = pathData.MiscProperties;
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
        [JsonPropertyName("costs")] public sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] public sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] public sbyte[] Properties { get; set; }
        [JsonPropertyName("cells")] public int[] Cells { get; set; }
        [JsonPropertyName("type")] public string Type => "topologyMapBi";
        
        public TopologyMapBi()
        {
            Header = (2 << 4) | MethodBi;
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

        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return;
            
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
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            visibilityData.Z = Z;
            visibilityData.CanViewThrough = false;
            visibilityData.Height = 0;
        }
        
        public override int GetZCount(int x, int y)
        {
            return 1;
        }
        
        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {

            var cellIndex = GetIndex(pathData.X, pathData.Y);
            Costs[cellIndex] = pathData.Cost;
            MurFins[cellIndex] = pathData.MurFinInfo;
            Properties[cellIndex] = pathData.MiscProperties;
        }
        
        public override TopologyMapC ConvertToC()
        {
            var costs = new sbyte[MapConstants.NumCells];
            var murfins = new sbyte[MapConstants.NumCells];
            var properties = new sbyte[MapConstants.NumCells];
            var movLos = new sbyte[MapConstants.NumCells];
            var zs = new short[MapConstants.NumCells];
            var heights = new sbyte[MapConstants.NumCells];

            for (var x = X; x < X + MapConstants.MapWidth; ++x)
            {
                for (var y = Y; y < Y + MapConstants.MapLength; ++y)
                {
                    var cellIndex = GetIndex(x, y);
                    var positionIndex = GetPositionIndex(x, y);
                    costs[positionIndex] = Costs[cellIndex];
                    murfins[positionIndex] = MurFins[cellIndex];
                    properties[positionIndex] = Properties[cellIndex];
                    zs[positionIndex] = Z;
                }
            }

            return new TopologyMapC
            {
                X = X,
                Y = Y,
                Z = Z,
                Costs = costs,
                MurFins = murfins,
                Properties = properties,
                MovLos = movLos,
                Zs = zs,
                Heights = heights
            };
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }
        
        private int GetPositionIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            return yIndex * MapConstants.MapWidth + xIndex;
        }
    }

    public class TopologyMapC : TopologyMapBlockedCells
    {
        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;

        [JsonPropertyName("costs")] public sbyte[] Costs { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("wallCells")] public sbyte[] MurFins { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("properties")] public sbyte[] Properties { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("movLos")] public sbyte[] MovLos { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("zs")] public short[] Zs { get; set; } = new short[MapConstants.NumCells];
        [JsonPropertyName("heights")] public sbyte[] Heights { get; set; } = new sbyte[MapConstants.NumCells];
        [JsonPropertyName("type")] public string Type => "topologyMapC";
        private int[] _orders = new int[MapConstants.NumCells];
        
        public TopologyMapC()
        {
            Header = (2 << 4) | MethodC;
        }

        public TopologyMapC(int x, int y)
        {
            X = x;
            Y = y;
            
            for (var i = 0; i < MapConstants.NumCells; ++i)
            {
                Zs[i] = short.MinValue;
                Costs[i] = -1;
            }
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

        public void AddElement(GfxData.Element element)
        {
            if (GlobalData.Instance.IgnoreGfxIds.Contains(element.CommonData.GfxId))
                return;
                
            var index = GetIndex(element.CellX, element.CellY);
            var z = Zs[index];
            var baseZ = z - Heights[index];

            if (z == short.MinValue)
            {
                Zs[index] = element.CellZ;
                Heights[index] = element.Height;
                Costs[index] = (sbyte)(element.CommonData.Walkable ? 0 : -1);
                _orders[index] = element.AltitudeOrder;
                return;
            }

            if (baseZ - element.CellZ >= 6 && element.CommonData.Walkable && Costs[index] < 0)
            {
                Zs[index] = element.CellZ;
                Heights[index] = element.Height;
                Costs[index] = 0;
                _orders[index] = element.AltitudeOrder;
                return;
            }

            if (element.CellZ > z && element.CommonData.Walkable)
            {
                Zs[index] = element.CellZ;
                Heights[index] = element.Height;
                Costs[index] = 0;
                _orders[index] = element.AltitudeOrder;
                return;
            }

            if (element.CellZ > z && Costs[index] < 0)
            {
                Zs[index] = element.CellZ;
                Heights[index] = element.Height;
                _orders[index] = element.AltitudeOrder;
                return;
            }

            if (element.CellZ > z && baseZ - element.CellZ < 6)
            {
                Zs[index] = element.CellZ;
                Heights[index] = element.Height;
                Costs[index] = (sbyte)(element.CommonData.Walkable ? 0 : -1);
                _orders[index] = element.AltitudeOrder;
            }
            
            if (element.CellZ == z && element.AltitudeOrder > _orders[index])
            {
                Heights[index] = element.Height;
                Costs[index] = (sbyte)(element.CommonData.Walkable ? 0 : -1);
                _orders[index] = element.AltitudeOrder;
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

        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return;
            
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
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            
            var cellIndex = GetIndex(x, y);
            visibilityData.Z = Zs[cellIndex];
            visibilityData.CanViewThrough = (MovLos[cellIndex] & LosMask) == LosMask;
            visibilityData.Height = Heights[cellIndex];
        }
        
        public override int GetZCount(int x, int y)
        {
            return 1;
        }
        
        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {
            var cellIndex = GetIndex(pathData.X, pathData.Y);
            Costs[cellIndex] = pathData.Cost;
            MurFins[cellIndex] = pathData.MurFinInfo;
            Properties[cellIndex] = pathData.MiscProperties;
            MovLos[cellIndex] = (sbyte)((pathData.CanMoveThrough ? MovMask : 0) | (visibilityData.CanViewThrough ? LosMask : 0));
            Zs[cellIndex] = pathData.Z;
            Heights[cellIndex] = pathData.Height;
        }
        
        public override TopologyMapC ConvertToC()
        {
            return this;
        }

        public bool IsEmpty()
        {
            return Zs.All(z => z == short.MinValue);
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
        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;

        [JsonPropertyName("costs")] public sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] public sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] public sbyte[] Properties { get; set; }
        [JsonPropertyName("movLos")] public sbyte[] MovLos { get; set; }
        [JsonPropertyName("zs")] public short[] Zs { get; set; }
        [JsonPropertyName("heights")] public sbyte[] Heights { get; set; }
        [JsonPropertyName("cells")] public long[] Cells { get; set; }
        [JsonPropertyName("type")] public string Type => "topologyMapCi";

        public TopologyMapCi()
        {
            Header = (2 << 4) | MethodCi;
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

        public void LoadFromGfx(GfxData.Partition partition)
        {
            X = partition.X;
            Y = partition.Y;
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
        
        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            if (!CheckPathData(x, y, cellPathData))
                return;

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
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            if (!CheckVisibilityData(x, y, cellVisibilityData))
                return;
            
            var visibilityData = cellVisibilityData[index];
            visibilityData.X = x;
            visibilityData.Y = y;
            
            var tab = GetIndex(x, y);
            visibilityData.Z = Zs[tab];
            visibilityData.CanViewThrough = (MovLos[tab] & LosMask) == LosMask;
            visibilityData.Height = Heights[tab];
        }
        
        public override int GetZCount(int x, int y)
        {
            return 1;
        }
        
        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {
            var cellIndex = GetIndex(pathData.X, pathData.Y);
            Costs[cellIndex] = pathData.Cost;
            MurFins[cellIndex] = pathData.MurFinInfo;
            Properties[cellIndex] = pathData.MiscProperties;
            MovLos[cellIndex] = (sbyte)((pathData.CanMoveThrough ? MovMask : 0) | (visibilityData.CanViewThrough ? LosMask : 0));
            Zs[cellIndex] = pathData.Z;
            Heights[cellIndex] = pathData.Height;
        }
        
        public override TopologyMapC ConvertToC()
        {
            var costs = new sbyte[MapConstants.NumCells];
            var murfins = new sbyte[MapConstants.NumCells];
            var properties = new sbyte[MapConstants.NumCells];
            var movLos = new sbyte[MapConstants.NumCells];
            var zs = new short[MapConstants.NumCells];
            var heights = new sbyte[MapConstants.NumCells];

            for (var x = X; x < X + MapConstants.MapWidth; ++x)
            {
                for (var y = Y; y < Y + MapConstants.MapLength; ++y)
                {
                    var cellIndex = GetIndex(x, y);
                    var positionIndex = GetPositionIndex(x, y);
                    costs[positionIndex] = Costs[cellIndex];
                    murfins[positionIndex] = MurFins[cellIndex];
                    properties[positionIndex] = Properties[cellIndex];
                    movLos[positionIndex] = MovLos[cellIndex];
                    zs[positionIndex] = Zs[cellIndex];
                    heights[positionIndex] = Heights[cellIndex];
                }
            }

            return new TopologyMapC
            {
                X = X,
                Y = Y,
                Z = Z,
                Costs = costs,
                MurFins = murfins,
                Properties = properties,
                MovLos = movLos,
                Zs = zs,
                Heights = heights
            };
        }

        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }
        
        private int GetPositionIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            return yIndex * MapConstants.MapWidth + xIndex;
        }
    }

    public class TopologyMapDi : TopologyMapBlockedCells
    {
        private static readonly List<int> Indexes = [];
        private static readonly Lock Mutex = new();

        private const sbyte MovMask = 0x01;
        private const sbyte LosMask = 0x02;
        private const int IndexOffset = 1;

        [JsonPropertyName("costs")] public sbyte[] Costs { get; set; }
        [JsonPropertyName("wallCells")] public sbyte[] MurFins { get; set; }
        [JsonPropertyName("properties")] public sbyte[] Properties { get; set; }
        [JsonPropertyName("movLos")] public sbyte[] MovLos { get; set; }
        [JsonPropertyName("zs")] public short[] Zs { get; set; }
        [JsonPropertyName("heights")] public sbyte[] Heights { get; set; }
        [JsonPropertyName("cells")] public long[] Cells { get; set; }
        [JsonPropertyName("cellsWithMultiZ")] public int[] CellsWithMultiZ { get; set; }
        [JsonPropertyName("type")] public string Type => "topologyMapDi";
        
        public TopologyMapDi()
        {
            Header = (2 << 4) | MethodDi;
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
        
        public override void GetPathData(int x, int y, CellPathData[] cellPathData, int index)
        {
            var cellIndex = GetIndex(x, y);
            if (cellIndex != 0)
            {
                var pathData = cellPathData[index];
                pathData.X = x;
                pathData.Y = y;
                FillPathData(pathData, cellIndex - IndexOffset);
                return;
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
            }
        }

        public override void GetVisibilityData(int x, int y, CellVisibilityData[] cellVisibilityData, int index)
        {
            var cellIndex = GetIndex(x, y);
            if (cellIndex != 0)
            {
                var visibilityData = cellVisibilityData[index];
                visibilityData.X = x;
                visibilityData.Y = y;
                FillVisibilityData(visibilityData, cellIndex - IndexOffset);
                return;
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
            }
        }
        
        public override int GetZCount(int x, int y)
        {
            return GetIndex(x, y) != 0 ? 1 : GetMultiIndex(x - X, y - Y, Indexes).Count;
        }
        
        public override void UpdateData(CellPathData pathData, CellVisibilityData visibilityData)
        {
            var cellIndex = GetIndex(pathData.X, pathData.Y);
            Costs[cellIndex] = pathData.Cost;
            MurFins[cellIndex] = pathData.MurFinInfo;
            Properties[cellIndex] = pathData.MiscProperties;
            MovLos[cellIndex] = (sbyte)((pathData.CanMoveThrough ? MovMask : 0) | (visibilityData.CanViewThrough ? LosMask : 0));
            Zs[cellIndex] = pathData.Z;
            Heights[cellIndex] = pathData.Height;
        }
        
        public override TopologyMapC ConvertToC()
        {
            var costs = new sbyte[MapConstants.NumCells];
            var murfins = new sbyte[MapConstants.NumCells];
            var properties = new sbyte[MapConstants.NumCells];
            var movLos = new sbyte[MapConstants.NumCells];
            var zs = new short[MapConstants.NumCells];
            var heights = new sbyte[MapConstants.NumCells];

            for (var x = X; x < X + MapConstants.MapWidth; ++x)
            {
                for (var y = Y; y < Y + MapConstants.MapLength; ++y)
                {
                    var cellIndex = GetIndex(x, y);
                    var positionIndex = GetPositionIndex(x, y);
                    if (cellIndex == 0)
                        cellIndex = GetValidIndex(x, y);
                    else
                        cellIndex -= IndexOffset;
                    if (cellIndex < 0 || cellIndex >= Costs.Length)
                        continue;
                    
                    
                    costs[positionIndex] = Costs[cellIndex];
                    murfins[positionIndex] = MurFins[cellIndex];
                    properties[positionIndex] = Properties[cellIndex];
                    movLos[positionIndex] = MovLos[cellIndex];
                    zs[positionIndex] = Zs[cellIndex];
                    heights[positionIndex] = Heights[cellIndex];
                }
            }

            return new TopologyMapC
            {
                X = X,
                Y = Y,
                Z = Z,
                Costs = costs,
                MurFins = murfins,
                Properties = properties,
                MovLos = movLos,
                Zs = zs,
                Heights = heights
            };
        }
        
        private int GetIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            var cellIndex = yIndex * MapConstants.MapWidth + xIndex;
            return TopologyIndexerHelper.GetIndex(Cells, cellIndex, Costs.Length);
        }

        private int GetPositionIndex(int x, int y)
        {
            var xIndex = x - X;
            var yIndex = y - Y;
            return yIndex * MapConstants.MapWidth + xIndex;
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

        private int GetValidIndex(int x, int y)
        {
            var indexes = GetMultiIndex(x - X, y - Y, Indexes);
            if (indexes.Count == 0)
                return -1;

            var valid = indexes.FindAll(i => Costs[i] >= 0);
            var invalid = indexes.FindAll(i => Costs[i] == -1);

            if (valid.Count == 0)
                return invalid.MaxBy(i => Zs[i]);
            if (invalid.Count == 0)
                return valid.MaxBy(i => Zs[i]);

            valid = valid.OrderByDescending(i => Zs[i]).ToList();
            foreach (var validIndex in valid)
            {
                var z = Zs[validIndex];
                if (z is < -13 or > 13)
                    continue;
                
                var isValid = true;
                foreach (var invalidIndex in invalid)
                {
                    var invalidZ = Zs[invalidIndex] - Heights[invalidIndex];
                    var zDiff = invalidZ - z;
                    if (zDiff is >= 0 and < 7)
                        isValid = false;
                }
                if (isValid) 
                    return validIndex;
            }
            return invalid.MaxBy(i => Zs[i]);
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
            
            var zCount = topologyMap.GetZCount(x, y);
            for (var i = 0; i < zCount; i++)
            {
                if (cellPathData[i].Z != z || cellPathData[i].CanMoveThrough)
                    continue;
                return (short)i;
            }
            return -1;
        }
        
        public long GetHash()
        {
            return (Y + 8192L & 0x3FFFL) << 34 |
                   (X + 8192L & 0x3FFFL) << 19;
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
    
    private class SByteArrayBase64Converter : JsonConverter<sbyte[]>
    {
        public override sbyte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
                return null;

            var base64String = reader.GetString();
            if (string.IsNullOrEmpty(base64String))
                return [];

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
    
    private class ShortArrayCompactConverter : JsonConverter<short[]>
    {
        public override short[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return reader.TokenType == JsonTokenType.Null ? null : JsonSerializer.Deserialize<short[]>(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, short[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append('[');
            for (var i = 0; i < value.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(value[i]);
            }
            sb.Append(']');
            writer.WriteRawValue(sb.ToString());
        }
    }
}
