using Godot;
using System;
using System.Collections.Generic;

public class PlaylistData
{
    public short Id { get; set; }

    public List<MusicData> MusicsDay { get; set; } = [];
    public List<MusicData> MusicsNight { get; set; } = [];
    
    public MusicData AmbienceDay { get; set; }
    public MusicData NightAmbience { get; set; }
    public MusicData Fight { get; set; }
    public MusicData BossFight { get; set; }

    public void Load(ExtendedDataInputStream reader)
    {
        Id = reader.ReadShort();
        LoadMusic(reader, 1);
        LoadMusic(reader, 2);
        LoadMusic(reader, 3);
    }

    public void Save(OutputBitStream writer)
    {
        writer.WriteShort(Id);
        
        SaveMusic(writer, MusicsDay, AmbienceDay);
        SaveMusic(writer, MusicsNight, NightAmbience);
        
        var fightMusics = new List<MusicData>();
        if (Fight != null) fightMusics.Add(Fight);
        if (BossFight != null) fightMusics.Add(BossFight);
        SaveMusic(writer, fightMusics, null);
    }

    private void LoadMusic(ExtendedDataInputStream reader, int musicType)
    {
        var musicCount = reader.ReadShort();
        for (var i = 0; i < musicCount; i++)
        {
            var music = new MusicData();
            music.Load(reader);
            AddMusicData(musicType, music);
        }
    }

    private void SaveMusic(OutputBitStream writer, List<MusicData> musics, MusicData ambience)
    {
        short counter = 0;
        if (musics != null) counter += (short)musics.Count;
        if (ambience != null) counter++;
        writer.WriteShort(counter);

        if (musics == null) return;
        foreach (var music in musics)
        {
            music.Save(writer);
        }
    }

    private void AddMusicData(int musicType, MusicData music)
    {
        switch (music.Order)
        {
            case -2:
                BossFight = music;
                break;
            case -1:
                Fight = music;
                break;
            case 0:
                if (musicType == 1) AmbienceDay = music;
                if (musicType == 2) NightAmbience = music;
                break;
            default:
                AddMusic(musicType, music);
                break;
        }
    }

    private void AddMusic(int musicType, MusicData music)
    {
        if (musicType == 1)
        {
            MusicsDay.Add(music);
            MusicsDay.Sort((x, y) => (int)(x.Id - y.Id));
        }
            
        if (musicType != 2)
            return;
        
        MusicsNight.Add(music);
        MusicsNight.Sort((x, y) => (int)(x.Id - y.Id));
    }


    public class MusicData
    {
        public long Id { get; set; }
        public long AlternateId { get; set; }
        public sbyte Volume { get; set; }
        public short SilenceDuration { get; set; }
        public sbyte Order { get; set; }
        public int NumLoops { get; set; }
        
        public void Load(ExtendedDataInputStream reader)
        {
            Id = reader.ReadLong();
            AlternateId = reader.ReadLong();
            Volume = reader.ReadByte();
            SilenceDuration = reader.ReadShort();
            Order = reader.ReadByte();
            NumLoops = reader.ReadInt();
        }

        public void Save(OutputBitStream writer)
        {
            writer.WriteLong(Id);
            writer.WriteLong(AlternateId);
            writer.WriteByte(Volume);
            writer.WriteShort(SilenceDuration);
            writer.WriteByte(Order);
            writer.WriteInt(NumLoops);
        }
    }
}
