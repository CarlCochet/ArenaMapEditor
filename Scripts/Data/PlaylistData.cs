using Godot;
using System;
using System.IO;

public class PlaylistData
{
    public short Id { get; set; }

    public void Load(BinaryReader reader)
    {
        Id = reader.ReadInt16();
        LoadMusic(reader);
        LoadMusic(reader);
        LoadMusic(reader);
    }

    public void Save(BinaryWriter writer)
    {
        
    }

    private void LoadMusic(BinaryReader reader)
    {
        var musicCount = reader.ReadInt16();
        for (var i = 0; i < musicCount; i++)
        {
            var music = new MusicData();
            music.Load(reader);
        }
    }


    public class MusicData
    {
        public long Id { get; set; }
        public long AlternateId { get; set; }
        public byte Volume { get; set; }
        public short SilenceDuration { get; set; }
        public byte Order { get; set; }
        public int NumLoops { get; set; }
        
        public void Load(BinaryReader reader)
        {
            Id = reader.ReadInt64();
            AlternateId = reader.ReadInt64();
            Volume = reader.ReadByte();
            SilenceDuration = reader.ReadInt16();
            Order = reader.ReadByte();
            NumLoops = reader.ReadInt32();
        }

        public void Save(BinaryWriter writer)
        {
            
        }
    }
}
