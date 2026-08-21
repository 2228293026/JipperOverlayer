// VerifyGameApi — checks JipperOverlayer's game-DLL dependencies against Libs/*.dll
// Run:  dotnet run -c Release            (from this folder)
//       VerifyGameApi.exe                (works from anywhere: searches upward for Libs/)
//       VerifyGameApi.exe <libs-folder>  (verify an arbitrary set of game DLLs)
//
// What it checks: every game type / member the mod references actually EXISTS in the
// game assemblies. This is the precise definition of a "mod vs game conflict" — a
// missing/renamed type or member would break the overlay (compile error, or a
// Harmony patch / reflection getter that silently fails at runtime).
//
// Note on Harmony patch targets: they are registered by [HarmonyPatch(typeof(T), nameof(T.M))],
// i.e. by NAME. A Postfix binds to ANY overload of that name, so we verify name-existence
// only (not exact parameter signature). Compile-time direct member accesses (GameRefs /
// VersionSafe) are also verified by name, since the mod's reflection getters are
// try/catch-wrapped and degrade gracefully if a member is absent.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using dnlib.DotNet;

class Program
{
    static readonly Dictionary<string, TypeDef> Types = new(StringComparer.Ordinal);
    static int Total, FailCount;

    static int Main(string[] args)
    {
        string libs = FindLibs(args);
        if (libs == null)
        {
            Console.Error.WriteLine("Libs/ not found — pass the folder as an argument or run from inside the repo.");
            return 2;
        }
        Console.WriteLine("Using Libs: " + Path.GetFullPath(libs));
        Load(Path.Combine(libs, "Assembly-CSharp.dll"));
        Load(Path.Combine(libs, "UnityEngine.AudioModule.dll"));
        Load(Path.Combine(libs, "UnityEngine.CoreModule.dll"));

        void CT(string n) { if (Types.ContainsKey(n)) Pass("TYPE  " + n); else Fail("TYPE  " + n); }
        // member = field OR property OR method, by name (existence only)
        void CM(string tn, string m)
        {
            if (!Types.TryGetValue(tn, out var t)) { Fail($"MEMBER {tn}.{m} (type missing)"); return; }
            if (HasMember(t, m)) Pass($"MEMBER {tn}.{m}"); else Fail($"MEMBER {tn}.{m}");
        }

        Console.WriteLine("=== Game types referenced by mod ===");
        foreach (var n in new[] {
            "ADOBase","RDC","scrController","scrConductor","scrLevelMaker","scnGame","scnEditor",
            "scrPlanet","scrShowIfDebug","scrEnableIfBeta","scrMisc","scrPressToStart","scrUIController",
            "scrPlayer","scrMarginTracker","scrMistakesManager","scrPlayerManager","scrFloor","GCS",
            "RDConstants","MonsterLove.StateMachine.StateBehaviour","States","HitMargin","Platform",
            "UnityEngine.AudioSource","PlanetarySystem","PlanetColor" })
            CT(n);

        Console.WriteLine("\n=== GameRefs reflection members (v141 default path) ===");
        CM("ADOBase","controller"); CM("ADOBase","conductor"); CM("ADOBase","lm"); CM("ADOBase","isScnGame"); CM("ADOBase","playerManager"); CM("ADOBase","platform");
        CM("scrController","instance"); CM("scrController","_instance"); CM("scrController","paused"); CM("scrController","currentSeqID");
        CM("scrController","currFloor"); CM("scrController","firstFloor"); CM("scrController","noFail"); CM("scrController","percentComplete");
        CM("scrController","checkpointsUsed"); CM("scrController","playerOne"); CM("scrController","mistakesManager");
        CM("scrConductor","instance"); CM("scrConductor","song"); CM("scrConductor","isGameWorld"); CM("scrConductor","addoffset"); CM("scrConductor","songposition_minusi");
        CM("UnityEngine.AudioSource","pitch"); CM("RDC","auto"); CM("scnGame","instance"); CM("scnGame","levelData"); CM("scnEditor","instance");

        Console.WriteLine("\n=== VersionSafe reflection members ===");
        CM("scrMarginTracker","hitMarginsCount"); CM("scrMarginTracker","AddHit"); CM("scrMarginTracker","Reset"); CM("scrMarginTracker","CalculatePercentAcc");
        CM("scrMistakesManager","marginTrackers"); CM("scrMistakesManager","percentAcc"); CM("scrMistakesManager","percentXAcc");
        CM("scrPlayerManager","playerColors"); CM("scrPlayerManager","playerCount"); CM("scrPlayerManager","instance"); CM("scrPlayerManager","allPlayers");
        CM("scrPlayer","planetarySystem"); CM("PlanetarySystem","speed"); CM("scrFloor","seqID"); CM("scrFloor","nextfloor"); CM("scrFloor","prevfloor"); CM("scrFloor","angleLength");
        CM("PlanetColor","ToRealColor");

        Console.WriteLine("\n=== Harmony patch targets (name-bound; Postfix works on any overload) ===");
        CM("MonsterLove.StateMachine.StateBehaviour","ChangeState");
        CM("scnGame","Play"); CM("scrPressToStart","ShowText"); CM("scrUIController","WipeToBlack");
        CM("scnEditor","ResetScene"); CM("scrController","StartLoadingScene"); CM("scrPlanet","MoveToNextFloor");
        CM("scrShowIfDebug","Update"); CM("scrShowIfDebug","Awake"); CM("scrShowIfDebug","txt");
        CM("RDC","auto"); CM("scrMisc","GetHitMargin");
        CM("scrEnableIfBeta","Awake"); CM("scrEnableIfBeta","setBuildText");
        CM("scrPlayer","Hit"); CM("scrMarginTracker","AddHit"); CM("scrMarginTracker","Reset"); CM("scrMarginTracker","CalculatePercentAcc");
        CM("scrMistakesManager","SetPlayerCount");

        Console.WriteLine("\n=== States enum values ===");
        if (Types.TryGetValue("States", out var st))
            foreach (var f in st.Fields) if (!f.Name.StartsWith("value__")) Console.WriteLine($"  {f.Name} = {f.Constant?.Value}");

        int rc = FailCount == 0 ? 0 : 1;
        Console.WriteLine($"\n==== RESULT: {FailCount} missing / {Total} checked  -> {(rc == 0 ? "NO CONFLICTS" : "CONFLICTS FOUND")} ====");
        return rc;
    }

    // Libs location: explicit argument wins; otherwise walk up from the executable
    // so the tool works from bin/, the project folder, or the repo root regardless
    // of the caller's working directory.
    static string FindLibs(string[] args)
    {
        if (args.Length > 0 && File.Exists(Path.Combine(args[0], "Assembly-CSharp.dll")))
            return args[0];
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "Libs");
            if (File.Exists(Path.Combine(candidate, "Assembly-CSharp.dll"))) return candidate;
        }
        return null;
    }

    static void Load(string path)
    {
        try
        {
            var m = ModuleDefMD.Load(path);
            foreach (var t in m.GetTypes())
                if (t.FullName != null && !Types.ContainsKey(t.FullName)) Types[t.FullName] = t;
        }
        catch (Exception e) { Console.WriteLine($"  [warn] cannot load {path}: {e.Message}"); }
    }
    static void Pass(string s) { Total++; Console.WriteLine("  OK   " + s); }
    static void Fail(string s) { Total++; FailCount++; Console.WriteLine("  FAIL " + s); }
    static bool HasMember(TypeDef t, string name)
    {
        foreach (var f in t.Fields) if (f.Name == name) return true;
        foreach (var p in t.Properties) if (p.Name == name) return true;
        foreach (var m in t.Methods) if (m.Name == name) return true;
        return false;
    }
}
