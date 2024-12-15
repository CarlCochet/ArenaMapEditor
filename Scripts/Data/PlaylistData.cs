using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public class PlaylistData
{
    public short Id { get; set; }

    public List<MusicData> Musics1 { get; set; } = [];
    public List<MusicData> Musics2 { get; set; } = [];
    
    public MusicData Music1 { get; set; }
    public MusicData Music2 { get; set; }
    public MusicData Music3 { get; set; }
    public MusicData Music4 { get; set; }

    public void Load(BinaryReader reader)
    {
        Id = reader.ReadInt16();
        LoadMusic(reader, 1);
        LoadMusic(reader, 2);
        LoadMusic(reader, 3);
    }

    public void Save(BinaryWriter writer)
    {
        
    }

    private void LoadMusic(BinaryReader reader, int iteration)
    {
        var musicCount = reader.ReadInt16();
        for (var i = 0; i < musicCount; i++)
        {
            var music = new MusicData();
            music.Load(reader);
            OrderDispatch(iteration, music);
        }
    }

    private void OrderDispatch(int iteration, MusicData music)
    {
        switch ((int)music.Order)
        {
            case -2:
                Music4 = music;
                break;
            case -1:
                Music3 = music;
                break;
            case 0:
                if (iteration == 1) Music1 = music;
                if (iteration == 2) Music2 = music;
                break;
            default:
                AddMusicToList(iteration, music);
                break;
        }
    }

    private void AddMusicToList(int iteration, MusicData music)
    {
        if (iteration == 1)
        {
            Musics1.Add(music);
            Musics1.Sort((x, y) => (int)(x.Id - y.Id));
        }
            
        if (iteration != 2)
            return;
        
        Musics2.Add(music);
        Musics2.Sort((x, y) => (int)(x.Id - y.Id));
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
