using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ADOFAI;

namespace JipperOverlayer.Overlayer;

public class PlayCount
{
    public static Dictionary<Hash, PlayData> Datas;
    private static string FilePath => Path.Combine(Main.Mod.Path, "Plays.dat");

    public static float Multiplier => (float)(ADOBase.conductor.song.pitch * VersionSafe.GetPlanetSpeed(scrController.instance));

    public static void Load()
    {
        string path = FilePath;
        Datas = new Dictionary<Hash, PlayData>();
        if (File.Exists(path))
        {
            try { LoadFile(path); return; }
            catch (Exception e) { Main.Mod.Logger.Warning($"Error loading play data: {e.Message}"); Datas.Clear(); }
        }
        path += ".bak";
        if (!File.Exists(path)) return;
        try { LoadFile(path); }
        catch (Exception e) { Main.Mod.Logger.Warning($"Error loading backup: {e.Message}"); }
    }

    private static void LoadFile(string path)
    {
        using FileStream fs = File.OpenRead(path);
        int version = fs.ReadByte();
        int count = fs.ReadInt();
        for (int i = 0; i < count; i++)
        {
            Hash key = fs.ReadBytes(16);
            (Datas[key] = new PlayData()).Read(fs, version);
        }
    }

    public static void Dispose() => Datas = null;

    public static void AddAttempts(Hash hash, float progress) => GetData(hash).AddAttempts(progress, Multiplier);
    public static void RemoveAttempts(Hash hash, float progress) => GetData(hash).RemoveAttempts(progress, Multiplier);
    public static void SetBest(Hash hash, float start, float cur, float multiplier) => GetData(hash).SetBest(start, cur, multiplier);

    public static void Save()
    {
        try
        {
            string path = FilePath;
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            using FileStream fs = File.OpenWrite(path);
            using MemoryStream ms = new();
            ms.WriteByte(1);
            ms.WriteInt(Datas.Count);
            foreach (var pair in Datas)
            {
                if (pair.Value == null) continue;
                ms.Write(pair.Key.data, 0, pair.Key.data.Length);
                pair.Value.Write(ms);
            }
            ms.WriteTo(fs);
        }
        catch (Exception e) { Main.Mod.Logger.Warning($"Error saving play data: {e.Message}"); }
    }

    public static PlayData GetData(Hash hash)
    {
        if (!Datas.ContainsKey(hash)) Datas[hash] = new PlayData();
        return Datas[hash];
    }

    public class PlayData
    {
        public int totalAttempts;
        public Dictionary<(float, float), int> attempts = new();
        public Dictionary<(float, float), float> best = new();

        public void AddAttempts(float progress, float multiplier)
        {
            var key = (progress, multiplier);
            if (attempts.ContainsKey(key)) attempts[key]++;
            else attempts[key] = 1;
            totalAttempts++;
            Save();
        }

        public void RemoveAttempts(float progress, float multiplier)
        {
            if (!attempts.TryGetValue((progress, multiplier), out int value)) return;
            if (value == 1) attempts.Remove((progress, multiplier));
            else attempts[(progress, multiplier)]--;
            totalAttempts--;
            Save();
        }

        public void SetBest(float start, float cur, float multiplier)
        {
            (float, float) key = (start, multiplier);
            if (best.ContainsKey(key))
            {
                if (!(best[key] < cur)) return;
            }
            best[key] = cur;
            Save();
        }

        public void Write(Stream stream)
        {
            stream.WriteInt(totalAttempts);
            stream.WriteInt(attempts.Count);
            foreach (var pair in attempts)
            {
                stream.WriteFloat(pair.Key.Item1);
                stream.WriteFloat(pair.Key.Item2);
                stream.WriteInt(pair.Value);
            }
            stream.WriteInt(best.Count);
            foreach (var pair in best)
            {
                stream.WriteFloat(pair.Key.Item1);
                stream.WriteFloat(pair.Key.Item2);
                stream.WriteFloat(pair.Value);
            }
        }

        public void Read(Stream stream, int version)
        {
            totalAttempts = stream.ReadInt();
            int size = stream.ReadInt();
            for (int i = 0; i < size; i++)
            {
                if (version == 0) stream.ReadByte();
                attempts[(stream.ReadFloat(), stream.ReadFloat())] = stream.ReadInt();
            }
            size = stream.ReadInt();
            for (int i = 0; i < size; i++)
            {
                if (version == 0) stream.ReadByte();
                best[(stream.ReadFloat(), stream.ReadFloat())] = stream.ReadFloat();
            }
        }

        public float GetBest(float start, float multiplier)
        {
            var key = (start, multiplier);
            return best.ContainsKey(key) ? best[key] : 0;
        }

        public int GetAttempts(float progress)
        {
            var key = (progress, Multiplier);
            return attempts.ContainsKey(key) ? attempts[key] : 0;
        }
        public int GetAttempts() => attempts.Values.Sum();
        public static implicit operator int(PlayData data) => data.totalAttempts;
    }

    public static Hash GetMapHash()
    {
        using MD5 md5 = MD5.Create();
        return md5.ComputeHash(ADOBase.isOfficialLevel ? Encoding.UTF8.GetBytes(ADOBase.currentLevel) : GetHash());
    }

    private static byte[] GetHash()
    {
        using MemoryStream ms = new();
        scrLevelMaker lm = ADOBase.lm;
        if (lm.isOldLevel) ms.WriteUTF(lm.leveldata);
        else ms.WriteObject(lm.floorAngles);
        foreach (LevelEvent levelEvent in ADOBase.customLevel.events)
        {
            switch (levelEvent.eventType)
            {
                case LevelEventType.SetSpeed:
                    ms.WriteInt(levelEvent.floor);
                    ms.WriteByte(0);
                    ms.WriteByte((byte)(SpeedType)levelEvent["speedType"]);
                    ms.WriteFloat((float)levelEvent[(SpeedType)levelEvent["speedType"] == SpeedType.Bpm ? "beatsPerMinute" : "bpmMultiplier"]);
                    break;
                case LevelEventType.Twirl:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(1); break;
                case LevelEventType.Hold:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(2);
                    ms.WriteInt((int)levelEvent["duration"]); break;
                case LevelEventType.MultiPlanet:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(3);
                    ms.WriteByte((byte)(PlanetCount)levelEvent["planets"]); break;
                case LevelEventType.Pause:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(4);
                    ms.WriteFloat((float)levelEvent["duration"]); break;
                case LevelEventType.AutoPlayTiles:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(5);
                    ms.WriteBoolean((bool)levelEvent["enabled"]); break;
                case LevelEventType.ScaleMargin:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(6);
                    ms.WriteFloat((float)levelEvent["scale"]); break;
                case LevelEventType.Multitap:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(7);
                    ms.WriteFloat((float)levelEvent["taps"]); break;
                case LevelEventType.KillPlayer:
                    ms.WriteInt(levelEvent.floor); ms.WriteByte(8); break;
            }
        }
        return ms.ToArray();
    }

    public readonly struct Hash(byte[] data) : IEquatable<Hash>
    {
        public readonly byte[] data = data;
        public override bool Equals(object obj) => obj is Hash h ? Equals(h) : obj is byte[] b && Equals(b);
        public bool Equals(Hash other) => Equals(other.data);
        public bool Equals(byte[] hash)
        {
            if (data == hash) return true;
            if (data == null || hash == null || data.Length != hash.Length) return false;
            return !data.Where((t, i) => t != hash[i]).Any();
        }
        public override int GetHashCode() => data != null ? ToString().GetHashCode() : 0;
        public static bool operator ==(Hash left, Hash right) => left.Equals(right);
        public static bool operator !=(Hash left, Hash right) => !(left == right);
        public static implicit operator Hash(byte[] hash) => new(hash);
        public static implicit operator byte[](Hash hash) => hash.data;
        public override string ToString() => string.Concat(data.Select(b => b.ToString("x2")));
    }
}

// Extension methods for binary I/O matching JALib's ByteTool
internal static class StreamExtensions
{
    public static int ReadInt(this Stream stream)
    {
        byte[] buf = new byte[4];
        stream.Read(buf, 0, 4);
        return buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24);
    }

    public static float ReadFloat(this Stream stream)
    {
        byte[] buf = new byte[4];
        stream.Read(buf, 0, 4);
        return BitConverter.ToSingle(buf, 0);
    }

    public static byte[] ReadBytes(this Stream stream, int count)
    {
        byte[] buf = new byte[count];
        stream.Read(buf, 0, count);
        return buf;
    }

    public static void WriteInt(this Stream stream, int value)
    {
        stream.WriteByte((byte)(value & 0xFF));
        stream.WriteByte((byte)((value >> 8) & 0xFF));
        stream.WriteByte((byte)((value >> 16) & 0xFF));
        stream.WriteByte((byte)((value >> 24) & 0xFF));
    }

    public static void WriteFloat(this Stream stream, float value)
    {
        byte[] buf = BitConverter.GetBytes(value);
        stream.Write(buf, 0, 4);
    }

    public static void WriteBoolean(this Stream stream, bool value) =>
        stream.WriteByte((byte)(value ? 1 : 0));

    public static void WriteUTF(this Stream stream, string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value ?? "");
        stream.WriteInt(bytes.Length);
        stream.Write(bytes, 0, bytes.Length);
    }

    public static void WriteObject(this Stream stream, object value)
    {
        if (value is float[] arr)
        {
            stream.WriteInt(arr.Length);
            foreach (float f in arr) stream.WriteFloat(f);
        }
    }
}
