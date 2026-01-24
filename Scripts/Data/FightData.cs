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
        var reader = GlobalData.Instance.GetReader($"{path}/{Id}", $"{Id}.fmd");

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
        var filePath = Path.Combine(path, $"{Id}.fmd");
        using var fileStream = File.Create(filePath);
        using var writer = new OutputBitStream(fileStream);
        SaveData(writer);
    }

    public (int placement, int bonus) GetData(int x, int y, int z)
    {
        var coord = GetCoord(x, y, z);
        if (coord == 0) 
            return (-1, -1);
        
        var blueIndex = StartPoints[0].IndexOf(coord);
        var redIndex = StartPoints[1].IndexOf(coord);
        var coachIndex = Array.IndexOf(CoachPoints, coord);
        var placement = blueIndex != -1 ? 0 :
            redIndex != -1 ? 1 :
            coachIndex >= 0 ? 2 : -1;
        
        var bonus = Bonus.TryGetValue(coord, out var bonusData) ? bonusData - 1002 : -1;
        return (placement, bonus);
    }

    public void AddStart(int x, int y, int z, int team)
    {
        if (team is < 0 or > 1)
            return;
        
        RemovePlacement(x, y, z);
        var coord = GetCoord(x, y, z);
        StartPoints[team].Add(coord);
    }

    public void AddCoach(int x, int y, int z)
    {
        var coord = GetCoord(x, y, z);
        for (var i = 0; i < CoachPoints.Length; i++)
        {
            if (CoachPoints[i] != 0) continue;
            CoachPoints[i] = coord;
            return;
        }

        for (var i = 0; i < CoachPoints.Length - 1; i++)
        {
            CoachPoints[i] = CoachPoints[i + 1];
        }
        CoachPoints[^1] = coord;
    }
    
    public void RemovePlacement(int x, int y, int z)
    {
        var coord = GetCoord(x, y, z);
        foreach (var start in StartPoints)
        {
            start.Remove(coord);
        }

        for (var i = 0; i < CoachPoints.Length; i++)
        {
            if (CoachPoints[i] == coord) CoachPoints[i] = 0;
        }
    }

    public void AddBonus(int x, int y, int z, int bonus)
    {
        var coord = GetCoord(x, y, z);
        Bonus.Remove(coord);
        Bonus.Add(coord, bonus);
    }

    public void RemoveBonus(int x, int y, int z)
    {
        var coord = GetCoord(x, y, z);
        Bonus.Remove(coord);
    }

    public FightData Copy()
    {
        return new FightData($"{Id}")
        {
            CoachPoints = (int[])CoachPoints.Clone(),
            StartPoints = StartPoints =
            [
                [..StartPoints[0]],
                [..StartPoints[1]]
            ],
            Bonus = new Dictionary<int, int>(Bonus)
        };
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
