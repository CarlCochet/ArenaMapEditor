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
        using var archive = ZipFile.OpenRead($"{path}/fight/{Id}.jar");
        var entry = archive.GetEntry($"{Id}.fmd");
        if (entry == null)
            return;
        
        using var stream = entry.Open();
        using var reader = new BinaryReader(stream);

        for (var i = 0; i < 6; i++)
        {
            CoachPoints[i] = reader.ReadInt32();
        }

        var startPointsCount = reader.ReadInt16() & 0xFFFF;
        var team1Count = startPointsCount >>> 8;
        var team2Count = team1Count & 0xFF;
        StartPoints[0] = new List<int>(team1Count);
        StartPoints[1] = new List<int>(team2Count);

        for (var i = 0; i < team1Count; i++)
        {
            StartPoints[0].Add(reader.ReadInt32());
        }

        for (var i = 0; i < team2Count; i++)
        {
            StartPoints[1].Add(reader.ReadInt32());
        }
        
        StartPoints[0].Sort();
        StartPoints[1].Sort();
        
        var bonusCount = reader.ReadByte() & 255;
        for (var i = 0; i < bonusCount; i++)
        {
            var position = reader.ReadInt32();
            var type = reader.ReadInt32();
            Bonus.Add(position, type);
        }
    }
}
