using System;
using System.Collections.Generic;

namespace JipperOverlayer;

internal static class MainThreadDispatcher
{
    private static readonly Queue<Action> _queue = new();

    public static void Enqueue(Action action)
    {
        lock (_queue) _queue.Enqueue(action);
    }

    public static void ExecutePending()
    {
        while (true)
        {
            Action action;
            lock (_queue)
            {
                if (_queue.Count == 0) return;
                action = _queue.Dequeue();
            }
            try { action(); }
            catch (Exception e) { Main.Mod?.Logger.Warning($"Dispatch error: {e.Message}"); }
        }
    }
}
