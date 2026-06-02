namespace JipperOverlayer.Overlayer;

public interface IOverlayTextManager
{
    void SetBest(float best);
    void CacheProgress(scrPlanet planet);
    void UpdateAccuracy(Overlay overlay, int index);
    void UpdateProgress(Overlay overlay);
    void UpdateProgressBar(Overlay overlay);
    void UpdateCheckpoint(Overlay overlay);
    void UpdateBest(Overlay overlay);
    float GetProgress();

    // Jongyeol-mode helpers (coop-aware)
    void UpdateDeath(Overlay overlay);
    void UpdateState(Overlay overlay, bool isPurePerfect);
    bool CheckPurePerfect(Overlay overlay);
    int GetTooJudgement(Overlay overlay);
}
