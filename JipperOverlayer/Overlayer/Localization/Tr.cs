namespace JipperOverlayer.Overlayer.Localization;

public static class Tr
{
    public enum Key
    {
        Size, ShowProgress, ShowAccuracy, ShowXAccuracy,
        ShowMusicTime, ShowMapTime, ShowMapIfNo,
        TimeTextType, ShowCheckpoint, ShowBest,
        ShowProgressBar, ShowCombo, EnableAutoCombo, ComboColorMax,
        ShowBpm, BpmColorMax, ShowJudgement, JudgementUp,
        ShowTimingScale, ShowAttempt, ShowFullAttempt,
        JongyeolMode, ShowFps, ShowAuthor, ShowState,
        ShowDeath, ShowStart, ShowTiming,
        HideDebugText, RemoveAutoReq, CheckPseudo, YellowCombo,
        LangLabel,
        ProgressColor, AccuracyColor, XaccuracyColor,
        MusicTimeColor, MapTimeColor, BestColor,
        ProgressBarColor, ProgressBarBgColor, ProgressBarBorderColor,
        ComboColor, BpmColor, AddColorStop, Delete, Percent,
        Count
    }

    private static readonly string[][] Data =
    [
        /* 0  English */ [ "Size", "Show Progress", "Show Accuracy", "Show XAccuracy",
            "Show Music Time", "Show Map Time", "Show Map Time If No Music",
            "Time Text Type", "Show Checkpoint", "Show Best",
            "Show Progress Bar", "Show Combo", "Enable Auto Combo", "Combo Color Max",
            "Show BPM", "BPM Color Max", "Show Judgement", "Judgement Location Up",
            "Show Timing Scale", "Show Attempt", "Show Full Attempt",
            "Jongyeol Mode", "Show FPS", "Show Author", "Show State",
            "Show Death", "Show Start", "Show Timing",
            "Hide Debug Text", "Remove Not Required In Auto", "Check Pseudo", "Yellow Combo",
            "Language",
            "Progress Color", "Accuracy Color", "XAccuracy Color",
            "Music Time Color", "Map Time Color", "Best Color",
            "Progress Bar Color", "Progress Bar Background Color", "Progress Bar Border Color",
            "Combo Color", "BPM Color", "Add Color Stop", "Delete", "Percent" ],

        /* 1  Korean */ [ "크기", "진행도 표시", "정확도 표시", "X정확도 표시",
            "음악 시간 표시", "맵 시간 표시", "음악 없을 때 맵 시간",
            "시간 텍스트 타입", "체크포인트 표시", "최고 기록 표시",
            "진행 바 표시", "콤보 표시", "자동 콤보 활성화", "콤보 색상 최대",
            "BPM 표시", "BPM 색상 최대", "판정 표시", "판정 위치 위로",
            "타이밍 스케일 표시", "시도 횟수 표시", "전체 시도 표시",
            "종열 모드", "FPS 표시", "제작자 표시", "상태 표시",
            "사망 표시", "시작 표시", "타이밍 표시",
            "디버그 텍스트 숨기기", "자동에서 불필요한 것 제거", "의사 BPM 확인", "노란 콤보",
            "언어",
            "진행도 색상", "정확도 색상", "X정확도 색상",
            "음악 시간 색상", "맵 시간 색상", "최고 기록 색상",
            "진행 바 색상", "진행 바 배경 색상", "진행 바 테두리 색상",
            "콤보 색상", "BPM 색상", "색상 포인트 추가", "삭제", "퍼센트" ],

        /* 2  Chinese */ [ "大小", "显示进度", "显示准确率", "显示X准确率",
            "显示音乐时间", "显示地图时间", "无音乐时显示地图时间",
            "时间文本类型", "显示检查点", "显示最佳",
            "显示进度条", "显示连击", "启用自动连击", "连击颜色最大值",
            "显示BPM", "BPM颜色最大值", "显示判定", "判定位置上移",
            "显示时机缩放", "显示尝试次数", "显示总尝试次数",
            "Jongyeol模式", "显示FPS", "显示作者", "显示状态",
            "显示死亡", "显示开始", "显示时机",
            "隐藏调试文本", "自动模式简化显示", "检测伪BPM", "黄色连击",
            "语言",
            "进度颜色", "准确率颜色", "X准确率颜色",
            "音乐时间颜色", "地图时间颜色", "最佳颜色",
            "进度条颜色", "进度条背景颜色", "进度条边框颜色",
            "连击颜色", "BPM颜色", "添加颜色点", "删除", "百分比" ],
    ];

    public static string Get(Key key)
    {
        if (Main.Settings == null) return Data[0][(int)key];
        return Data[(int)Main.Settings.CurrentLanguage][(int)key];
    }

    public static string Get(string legacyKey)
    {
        if (Main.Settings == null) return legacyKey;
        return Data[(int)Main.Settings.CurrentLanguage][(int)_keyMap[legacyKey]];
    }

    private static readonly System.Collections.Generic.Dictionary<string, Key> _keyMap = new()
    {
        {"size", Key.Size}, {"show_progress", Key.ShowProgress}, {"show_accuracy", Key.ShowAccuracy},
        {"show_xaccuracy", Key.ShowXAccuracy}, {"show_music_time", Key.ShowMusicTime},
        {"show_map_time", Key.ShowMapTime}, {"show_map_if_no", Key.ShowMapIfNo},
        {"time_text_type", Key.TimeTextType}, {"show_checkpoint", Key.ShowCheckpoint},
        {"show_best", Key.ShowBest}, {"show_progress_bar", Key.ShowProgressBar},
        {"show_combo", Key.ShowCombo}, {"enable_auto_combo", Key.EnableAutoCombo},
        {"combo_color_max", Key.ComboColorMax}, {"show_bpm", Key.ShowBpm},
        {"bpm_color_max", Key.BpmColorMax}, {"show_judgement", Key.ShowJudgement},
        {"judgement_up", Key.JudgementUp}, {"show_timing_scale", Key.ShowTimingScale},
        {"show_attempt", Key.ShowAttempt}, {"show_full_attempt", Key.ShowFullAttempt},
        {"jongyeol_mode", Key.JongyeolMode}, {"show_fps", Key.ShowFps},
        {"show_author", Key.ShowAuthor}, {"show_state", Key.ShowState},
        {"show_death", Key.ShowDeath}, {"show_start", Key.ShowStart},
        {"show_timing", Key.ShowTiming}, {"hide_debug_text", Key.HideDebugText},
        {"remove_auto_req", Key.RemoveAutoReq}, {"check_pseudo", Key.CheckPseudo},
        {"yellow_combo", Key.YellowCombo}, {"lang_label", Key.LangLabel},
        {"progress_color", Key.ProgressColor}, {"accuracy_color", Key.AccuracyColor},
        {"xaccuracy_color", Key.XaccuracyColor}, {"music_time_color", Key.MusicTimeColor},
        {"map_time_color", Key.MapTimeColor}, {"best_color", Key.BestColor},
        {"progress_bar_color", Key.ProgressBarColor},
        {"progress_bar_bg_color", Key.ProgressBarBgColor},
        {"progress_bar_border_color", Key.ProgressBarBorderColor},
        {"combo_color", Key.ComboColor}, {"bpm_color", Key.BpmColor},
        {"add_color_stop", Key.AddColorStop}, {"delete", Key.Delete}, {"percent", Key.Percent},
    };
}
