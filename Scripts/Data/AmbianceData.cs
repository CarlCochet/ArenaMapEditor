using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public class AmbianceData
{
    public int Id { get; set; } = -1;
    public int AmbianceCount { get; set; }
    public Dictionary<int, AmbianceProperties> Properties { get; set; }

    public AmbianceData(BinaryReader reader, string fullName)
    {
        var splitName = fullName.Split('/');
        if (splitName.Length < 2)
            return;
        if (!int.TryParse(splitName[^2], out var id))
            return;
        Id = id;
        
        AmbianceCount = reader.ReadInt32();
        LoadData(reader);
    }

    public void LoadData(BinaryReader reader)
    {
        
    }


    public class AmbianceProperties
    {
        
    }
}
