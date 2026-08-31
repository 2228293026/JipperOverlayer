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
		// 角度(度) → 时间(秒)：deg = time * pitch * bpm * speed * 3 → time = deg / (3 * bpm * speed * pitch)
		double denom = 3.0 * bpmTimesSpeed * conductorPitch;

		float AngleToMs(double deg) => (float)(deg * 1000.0 / denom);

		try
		{
			float perfectMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Pure, bpmTimesSpeed, conductorPitch, marginScale));
			float greatMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Perfect, bpmTimesSpeed, conductorPitch, marginScale));
			float goodMs = AngleToMs(scrMisc.GetAdjustedAngleBoundaryInDeg(HitMarginGeneral.Counted, bpmTimesSpeed, conductorPitch, marginScale));

			float xPerfectMs = 0f;
			bool xPerfectValid = false;
			// 大 p 边界是确定公式；仅当 XPerfect 已安装并启用时展示
			if (XPerfectIntegration.IsAvailable)
			{
				double xBoundaryDeg = XPerfectBoundaryDeg(bpmTimesSpeed, conductorPitch, marginScale);
				xPerfectMs = AngleToMs(xBoundaryDeg);
				xPerfectValid = xPerfectMs > 0;
			}

			return new Result(perfectMs, xPerfectMs, greatMs, goodMs, xPerfectValid);
		}
		catch (Exception e)
		{
			Loader.Warning($"TimingWindow: 游戏函数调用失败 ({e.Message})");
			return default;
		}
	}

	/// <summary>XPerfect 大 p 边界角度（度）：max(15° × margin, 16.67ms 换算的角度)</summary>
	private static double XPerfectBoundaryDeg(double bpmTimesSpeed, double conductorPitch, float marginScale)
	{
		const double xPerfectBaseDeg = 15.0;
		const double xPerfectMinTimeSec = 0.01667;

		double xMinTimeDeg = scrMisc.TimeToAngleInRad(xPerfectMinTimeSec, bpmTimesSpeed, conductorPitch, false) * Mathf.Rad2Deg;
		return Math.Max(xPerfectBaseDeg * marginScale, xMinTimeDeg);
	}
}
