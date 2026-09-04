using System;
using UnityEngine;

namespace JipperOverlayer.Overlayer.Util;

/// <summary>
/// 判定时间窗口（±x ms）计算器
/// 直接复用游戏函数 scrMisc.GetAdjustedAngleBoundaryInDeg，保证与真实判定一致
/// 同时增加对XPerfect的支持，但手动计算
/// </summary>
internal static class TimingWindowCalculator
{
	public readonly struct Result
	{
		public readonly float XPerfectMs;
		public readonly float PerfectMs;
		public readonly float GreatMs;
		public readonly float GoodMs;
		public readonly bool XPerfectValid;

		public Result(float perfectMs, float xPerfectMs, float greatMs, float goodMs, bool xPerfectValid)
		{
			PerfectMs = perfectMs;
			XPerfectMs = xPerfectMs;
			GreatMs = greatMs;
			GoodMs = goodMs;
			XPerfectValid = xPerfectValid;
		}

		public readonly bool Valid => PerfectMs > 0 || GreatMs > 0 || GoodMs > 0;
	}

	// 输入缓存：Calculate 每帧被调用，输入未变时直接复用上次结果，跳过游戏函数调用
	private static double _cacheBpm = -1.0, _cachePitch = -1.0;
	private static float _cacheMargin = -1f, _cacheSpeedTrial = -1f, _cacheCounted = -1f;
	private static int _cacheDifficulty = -1;
	private static bool _cacheXp, _warned;
	private static Result _cache;

	public static Result Calculate(scrFloor floor)
	{
		if (floor == null) return default;
		var conductor = GameRefs.ConductorInstance;
		var ctrl = GameRefs.ControllerInstance;
		if (conductor == null || ctrl == null) return default;

		double bpmTimesSpeed = conductor.bpm * VersionSafe.GetPlanetSpeed(ctrl);
		double conductorPitch = GameRefs.SongPitch;
		float marginScale = (float)floor.marginScale;

		if (bpmTimesSpeed <= 0 || conductorPitch <= 0) return default;

		// GetAdjustedAngleBoundaryInDeg 除参数外还读取 GCS.difficulty / currentSpeedTrial /
		// HITMARGIN_COUNTED（isMobile 运行期不变），这些隐藏输入必须一并纳入缓存键
		int difficulty = (int)GCS.difficulty;
		float speedTrial = GCS.currentSpeedTrial;
		float counted = GCS.HITMARGIN_COUNTED;
		bool xp = XPerfectIntegration.IsAvailable;

		if (bpmTimesSpeed == _cacheBpm && conductorPitch == _cachePitch && marginScale == _cacheMargin &&
			difficulty == _cacheDifficulty && speedTrial == _cacheSpeedTrial && counted == _cacheCounted && xp == _cacheXp)
			return _cache;

		// 角度(度) → 时间(秒)：deg = time * pitch * bpm * speed * 3 → time = deg / (3 * bpm * speed * pitch)
		double denom = 3.0 * bpmTimesSpeed * conductorPitch;

		try
		{
			float perfectMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Pure, bpmTimesSpeed, conductorPitch, marginScale), denom);
			float greatMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Perfect, bpmTimesSpeed, conductorPitch, marginScale), denom);
			float goodMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Counted, bpmTimesSpeed, conductorPitch, marginScale), denom);

			float xPerfectMs = 0f;
			bool xPerfectValid = false;
			// 大 p 边界是确定公式；仅当 XPerfect 已安装并启用时展示
			if (xp)
			{
				double xBoundaryDeg = XPerfectBoundaryDeg(bpmTimesSpeed, conductorPitch, marginScale);
				xPerfectMs = AngleToMs(xBoundaryDeg, denom);
				xPerfectValid = xPerfectMs > 0;
			}

			_cacheBpm = bpmTimesSpeed; _cachePitch = conductorPitch; _cacheMargin = marginScale;
			_cacheDifficulty = difficulty; _cacheSpeedTrial = speedTrial; _cacheCounted = counted; _cacheXp = xp;
			_cache = new Result(perfectMs, xPerfectMs, greatMs, goodMs, xPerfectValid);
			_warned = false;
			return _cache;
		}
		catch (Exception e)
		{
			// 每帧调用，持续失败只警告一次，避免日志刷屏
			if (!_warned) { _warned = true; Loader.Warning($"TimingWindow: 游戏函数调用失败 ({e.Message})"); }
			return default;
		}
	}

	// 静态方法而非捕获 denom 的局部 lambda：lambda 每次调用都会分配一个闭包对象
	private static float AngleToMs(double deg, double denom) => (float)(deg * 1000.0 / denom);

	/// <summary>XPerfect 大 p 边界角度（度）：max(15° × margin, 16.67ms 换算的角度)</summary>
	private static double XPerfectBoundaryDeg(double bpmTimesSpeed, double conductorPitch, float marginScale)
	{
		const double xPerfectBaseDeg = 15.0;
		const double xPerfectMinTimeSec = 0.01667;

		double xMinTimeDeg = scrMisc.TimeToAngleInRad(xPerfectMinTimeSec, bpmTimesSpeed, conductorPitch, false) * Mathf.Rad2Deg;
		return Math.Max(xPerfectBaseDeg * marginScale, xMinTimeDeg);
	}
}
