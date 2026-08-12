namespace JipperOverlayer.Overlayer.Util;

internal static class BpmCalculator
{
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
        var conductor = GameRefs.ConductorInstance;
        if (conductor == null) return new Result(0, 0, 0);
        float bpm = (float)(conductor.bpm * planetSpeed);
        float cbpm = floor.nextfloor
            ? (float)(60.0 / (floor.nextfloor.entryTime - floor.entryTime) * GameRefs.SongPitch)
            : bpm;
        float kps = cbpm / 60;
        return new Result(bpm, cbpm, kps);
    }
}