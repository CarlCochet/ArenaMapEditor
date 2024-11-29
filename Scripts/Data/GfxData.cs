using Godot;
using System;
using System.Collections.Generic;

public partial class GfxData : Node
{
    private List<string> _ignoreFiles = ["META-INF", "coord", "groups.lib"]; 

    public GfxData(string path, string id)
    {
        LoadData(path, id);
    }

    private void LoadData(string path, string id)
    {
        var archivePath = $"{path}/gfx/{id}.jar";
        
    }
}
