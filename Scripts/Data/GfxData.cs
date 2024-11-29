using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

public class GfxData
{
    private readonly List<string> _ignoreFiles = ["META-INF", "coord", "groups.lib"]; 

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
        
    }

    public class Partition
    {
        
    }

    public class Element
    {
        
    }
}
