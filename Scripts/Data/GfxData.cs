using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class GfxData
{
    private readonly List<string> _ignoreFiles = ["META-INF", "coord", "groups.lib"];
    public const int MapWidth = 1024;
    public const int MapHeight = 576;
    
    private int _coordMinX = int.MaxValue;
    private int _coordMinY = int.MaxValue;
    private short _coordMinZ = short.MaxValue;
    private int _coordMaxX = int.MinValue;
    private int _coordMaxY = int.MinValue;
    private short _coordMaxZ = short.MinValue;

    public GfxData(string path, string id)
    {
        LoadData(path, id);
    }

    private void LoadData(string path, string id)
    {
        using var archive = ZipFile.OpenRead($"{path}/gfx/{id}.jar");

        foreach (var entry in archive.Entries)
        {
            if (_ignoreFiles.Contains(entry.FullName))
                continue;
            
            using var stream = entry.Open();
            using var reader = new BinaryReader(stream);
            
            ReadBytes(reader);
        }
    }

    private void ReadBytes(BinaryReader reader)
    {
        _coordMinX = reader.ReadInt32();
        _coordMinY = reader.ReadInt32();
        _coordMinZ = reader.ReadInt16();
        _coordMaxX = reader.ReadInt32();
        _coordMaxY = reader.ReadInt32();
        _coordMaxZ = reader.ReadInt16();
        
        var baseX = reader.ReadInt32();
        var baseY = reader.ReadInt32();
        var maxZ = reader.ReadInt16() & 0xFFFF;

        for (var z = 0; z < maxZ; z++)
        {
            var minX = baseX + (reader.ReadByte() & 255);
            var maxX = baseX + (reader.ReadByte() & 255);
            var minY = baseY + (reader.ReadByte() & 255);
            var maxY = baseY + (reader.ReadByte() & 255);

            for (var x = minX; x < maxX; x++)
            {
                for (var y = minY; y < maxY; y++)
                {
                    var elementCount = reader.ReadByte() & 255;
                    for (var elementIndex = 0; elementIndex < elementCount; elementIndex++)
                    {
                        
                    }
                }
            }
        }
    }

    public class Partition
    {
        
    }

    public class Element
    {
        
    }
}
