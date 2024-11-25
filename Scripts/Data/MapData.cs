using Godot;
using System;
using System.Collections.Generic;

public partial class MapData : Node
{
    public int Id { get; set; }
    public List<PartitionData> Partitions { get; set; } = [];

    public MapData(string path, int id)
    {
        Id = id;
    }

    public void LoadData()
    {
        
    }
}
