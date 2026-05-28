using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using ADOFAI;

namespace JipperOverlayer.Overlayer;

public class PlayCount
{
    public static Dictionary<Hash, PlayData> Datas;
    private static string FilePath => Path.Combine(Main.Mod.Path, "Plays.dat");
    private static readonly MD5 Md5 = MD5.Create();

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

    public static void Dispose()
    {
        Save();
        Datas = null;
    }

    public static void AddAttempts(Hash hash, float progress) => GetData(hash).AddAttempts(progress, Multiplier);
    public static void RemoveAttempts(Hash hash, float progress) => GetData(hash).RemoveAttempts(progress, Multiplier);
    public static void SetBest(Hash hash, float start, float cur, float multiplier) => GetData(hash).SetBest(start, cur, multiplier);

    public static void Save()
    {
        try
        {
            string path = FilePath;
            if (Datas == null) { Main.Mod.Logger.Warning("Save skipped: Datas is null"); return; }
            string tmpPath = path + ".tmp";
            using (FileStream fs = new(tmpPath, FileMode.Create))
            using (MemoryStream ms = new())
            {
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
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            File.Delete(path);
            File.Move(tmpPath, path);
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
        }

        public void RemoveAttempts(float progress, float multiplier)
        {
            if (!attempts.TryGetValue((progress, multiplier), out int value)) return;
            if (value == 1) attempts.Remove((progress, multiplier));
            else attempts[(progress, multiplier)]--;
            totalAttempts--;
        }

        public void SetBest(float start, float cur, float multiplier)
        {
            (float, float) key = (start, multiplier);
            if (best.ContainsKey(key))
            {
                if (!(best[key] < cur)) return;
            }
            best[key] = cur;
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
        public int GetAttempts()
        {
            int sum = 0;
            foreach (var v in attempts.Values) sum += v;
            return sum;
        }
        public static implicit operator int(PlayData data) => data.totalAttempts;
    }

    public static Hash GetMapHash()
    {
        lock (Md5) { return Md5.ComputeHash(ADOBase.isOfficialLevel ? Encoding.UTF8.GetBytes(ADOBase.currentLevel) : GetHash()); }
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
            for (int i = 0; i < data.Length; i++)
                if (data[i] != hash[i]) return false;
            return true;
        }
        public override int GetHashCode()
        {
            if (data == null) return 0;
            int h = 0;
            for (int i = 0; i < data.Length && i < 16; i++)
                h = h * 31 + data[i];
            return h;
        }
        public static bool operator ==(Hash left, Hash right) => left.Equals(right);
        public static bool operator !=(Hash left, Hash right) => !(left == right);
        public static implicit operator Hash(byte[] hash) => new(hash);
        public static implicit operator byte[](Hash hash) => hash.data;
        public override string ToString()
        {
            char[] chars = new char[data.Length * 2];
            for (int i = 0; i < data.Length; i++)
            {
                chars[i * 2] = "0123456789abcdef"[data[i] >> 4];
                chars[i * 2 + 1] = "0123456789abcdef"[data[i] & 0xF];
            }
            return new string(chars);
        }
    }
}

// Extension methods for binary I/O matching JALib's ByteTool
internal static class StreamExtensions
{
    public static int ReadInt(this Stream stream)
    {
        byte[] buf = new byte[4];
        if (stream.Read(buf, 0, 4) < 4) throw new EndOfStreamException();
        return buf[0] | (buf[1] << 8) | (buf[2] << 16) | (buf[3] << 24);
    }

    public static float ReadFloat(this Stream stream)
    {
        byte[] buf = new byte[4];
        if (stream.Read(buf, 0, 4) < 4) throw new EndOfStreamException();
        return BitConverter.ToSingle(buf, 0);
    }

    public static byte[] ReadBytes(this Stream stream, int count)
    {
        byte[] buf = new byte[count];
        int offset = 0;
        while (offset < count)
        {
            int read = stream.Read(buf, offset, count - offset);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
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
