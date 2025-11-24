using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class FightData
{
    public int Id { get; set; }
    public int[] CoachPoints = new int[6];
    public List<int>[] StartPoints = new List<int>[2];
    public Dictionary<int, int> Bonus = new();

    public FightData(string id)
    {
        if (!int.TryParse(id, out var worldId))
            return;
        Id = worldId;
    }

    public int GetCoord(int x, int y, int z)
    {
        if (x >= 2048 || y >= 2048)
            return 0;
        return x + 2047 << 20 | y + 2047 << 8 | z + 127;
    }

    public (int x, int y, int z) GetCoords(int coord)
    {
        return ((coord >>> 20 & 4095) - 2047, (coord >>> 8 & 4095) - 2047, (short)((coord & 0xFF) - 127)); 
    }

    public void Load(string path)
    {
        using var archive = ZipFile.OpenRead($"{path}/{Id}.jar");
        var entry = archive.GetEntry($"{Id}.fmd");
        if (entry == null)
            return;
        
        using var stream = entry.Open();
        var reader = new ExtendedDataInputStream(stream);

        for (var i = 0; i < 6; i++)
        {
            CoachPoints[i] = reader.ReadInt();
        }

        var startPointsCount = reader.ReadShort() & 0xFFFF;
        var team1Count = startPointsCount >>> 8;
        var team2Count = startPointsCount & 0xFF;
        StartPoints[0] = new List<int>(team1Count);
        StartPoints[1] = new List<int>(team2Count);

        for (var i = 0; i < team1Count; i++)
        {
            StartPoints[0].Add(reader.ReadInt());
        }

        for (var i = 0; i < team2Count; i++)
        {
            StartPoints[1].Add(reader.ReadInt());
        }
        
        StartPoints[0].Sort();
        StartPoints[1].Sort();
        
        var bonusCount = reader.ReadByte() & 0xFF;
        for (var i = 0; i < bonusCount; i++)
        {
            var position = reader.ReadInt();
            var type = reader.ReadInt();
            Bonus.TryAdd(position, type);
        }
    }
    
    public void Save(string path)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"gfx_{Id}_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        
        try
        {
            var fileName = $"{Id}.fmd";
            var filePath = Path.Combine(tempDir, fileName);

            {
                using var fileStream = File.Create(filePath);
                using var writer = new OutputBitStream(fileStream);
                SaveData(writer);
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

    private void SaveData(OutputBitStream writer)
    {
        foreach (var c in CoachPoints)
        {
            writer.WriteInt(c);
        }

        var startPointsCount = (StartPoints[0].Count << 8) | StartPoints[1].Count;
        writer.WriteShort(unchecked((short)startPointsCount));

        foreach (var s in StartPoints[0])
        {
            writer.WriteInt(s);
        }
        foreach (var s in StartPoints[1])
        {
            writer.WriteInt(s);
        }
        
        writer.WriteByte(unchecked((sbyte)Bonus.Count));
        foreach (var bonus in Bonus)
        {
            writer.WriteInt(bonus.Key);
            writer.WriteInt(bonus.Value);
        }
    }
}
