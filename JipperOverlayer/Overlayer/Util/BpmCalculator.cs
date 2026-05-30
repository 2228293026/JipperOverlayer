using UnityEngine;

namespace JipperOverlayer.Overlayer.Util;

internal static class BpmCalculator
{
    private static readonly char[] HexChars = ['0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'];

    public readonly struct Result
    {
        public readonly float TileBpm;
        public readonly float CurrentBpm;
        public readonly float Kps;

        public Result(float tileBpm, float currentBpm, float kps)
        {
            TileBpm = tileBpm;
            CurrentBpm = currentBpm;
            Kps = kps;
        }
    }

    public static Result Calculate(scrFloor floor, float planetSpeed)
    {
        var conductor = scrConductor.instance;
        float bpm = (float)(conductor.bpm * planetSpeed);
        float cbpm = floor.nextfloor
            ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * conductor.song.pitch)
            : bpm;
        float kps = cbpm / 60;
        return new Result(bpm, cbpm, kps);
    }

    public static string ColorToHex(Color c)
    {
        int r = Mathf.RoundToInt(c.r * 255);
        int g = Mathf.RoundToInt(c.g * 255);
        int b = Mathf.RoundToInt(c.b * 255);
        int a = c.a == 1 ? -1 : Mathf.RoundToInt(c.a * 255);
        int len = a >= 0 ? 8 : 6;
        char[] chars = new char[len];
        chars[0] = HexChars[r >> 4]; chars[1] = HexChars[r & 0xF];
        chars[2] = HexChars[g >> 4]; chars[3] = HexChars[g & 0xF];
        chars[4] = HexChars[b >> 4]; chars[5] = HexChars[b & 0xF];
        if (a >= 0) { chars[6] = HexChars[a >> 4]; chars[7] = HexChars[a & 0xF]; }
        return new string(chars);
    }
}
