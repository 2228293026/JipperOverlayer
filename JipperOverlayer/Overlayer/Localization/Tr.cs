namespace JipperOverlayer.Overlayer.Localization;

public static class Tr
{
    public enum Key
    {
        Size, ShowProgress, ShowAccuracy, ShowXAccuracy,
        ShowMusicTime, ShowMapTime, ShowMapIfNo,
        TimeTextType, TimeTextKorean, TimeTextEnglish,
        ShowCheckpoint, ShowBest,
        ShowProgressBar, ShowCombo, EnableAutoCombo, ComboColorMax,
        ShowBpm, BpmColorMax, ShowJudgement, JudgementUp,
        ShowTimingScale, ShowAttempt, ShowFullAttempt,
        JongyeolMode, ShowFps, ShowAuthor, ShowState,
        ShowDeath, ShowStart, ShowTiming,
        HideDebugText, RemoveAutoReq, CheckPseudo, AllowELCombo, AllowOrangeCombo,
        LangLabel,
        Font, CustomPositions, ResetPositions,
        TextSettings, AlignMain, AlignBpm, AlignJudge, AlignCombo, AlignComboVal, AlignTiming, AlignAttempt, ApplyAlignment, AlignReset,
        StyleBold, StyleItalic, StyleUnderline, StyleStrike, StyleHighlight,
        ProgressColor, AccuracyColor, XaccuracyColor,
        MusicTimeColor, MapTimeColor, BestColor,
        ProgressBarColor, ProgressBarBgColor, ProgressBarBorderColor,
        ComboColor, BpmColor, AddColorStop, Delete, Percent,
        CustomLabels,
        General, Display, ProgressAccuracy, TimeSection,
        ProgressBarBest, ComboSection, BpmSection, JudgementOther,
        DisplayOptions, BehaviorOptions,
        FPSRefreshRate, DecimalPrecision,
        Count
    }

    private static readonly string[][] Data =
    [
        /* 0  English */ [ "Size", "Show Progress", "Show Accuracy", "Show XAccuracy",
            "Show Music Time", "Show Map Time", "Show Map Time If No Music",
            "Time Text Type", "Korean", "English",
            "Show Checkpoint", "Show Best",
            "Show Progress Bar", "Show Combo", "Enable Auto Combo", "Combo Color Max",
            "Show BPM", "BPM Color Max", "Show Judgement", "Judgement Location Up",
            "Show Timing Scale", "Show Attempt", "Show Full Attempt",
            "Jongyeol Mode", "Show FPS", "Show Author", "Show State",
            "Show Death", "Show Start", "Show Timing",
            "Hide Debug Text", "Remove Not Required In Auto", "Check Pseudo", "EL Judgment Combo", "VL Judgment Combo",
            "Language",
            "Font", "Custom Positions", "Reset Positions",
            "Text Settings", "Main", "BPM", "Judge", "Combo Title", "Combo Value", "Timing Scale", "Attempt", "Apply Alignment", "Reset",
            "B", "I", "U", "S", "H",
            "Progress Color", "Accuracy Color", "XAccuracy Color",
            "Music Time Color", "Map Time Color", "Best Color",
            "Progress Bar Color", "Progress Bar Background Color", "Progress Bar Border Color",
            "Combo Color", "BPM Color", "Add Color Stop", "Delete", "Percent", "Custom Labels",
            "General", "Display", "Progress && Accuracy", "Time",
            "Progress Bar && Best", "Combo", "BPM", "Judgement && Other",
            "Display Options", "Behavior Options", "FPS Refresh Rate", "Decimal Precision" ],

        /* 1  Korean */ [ "크기", "진행도 표시", "정확도 표시", "X정확도 표시",
            "음악 시간 표시", "맵 시간 표시", "음악 없을 때 맵 시간",
            "시간 텍스트 타입", "한국어", "영어",
            "체크포인트 표시", "최고 기록 표시",
            "진행 바 표시", "콤보 표시", "자동 콤보 활성화", "콤보 색상 최대",
            "BPM 표시", "BPM 색상 최대", "판정 표시", "판정 위치 위로",
            "타이밍 스케일 표시", "시도 횟수 표시", "전체 시도 표시",
            "종열 모드", "FPS 표시", "제작자 표시", "상태 표시",
            "사망 표시", "시작 표시", "타이밍 표시",
            "디버그 텍스트 숨기기", "자동에서 불필요한 것 제거", "의사 BPM 확인", "EL 판정 콤보", "VL 판정 콤보",
            "언어",
            "글꼴", "사용자 지정 위치", "위치 초기화",
            "텍스트 설정", "메인", "BPM", "판정", "콤보 제목", "콤보 값", "타이밍 스케일", "시도", "정렬 적용", "초기화",
            "B", "I", "U", "S", "H",
            "진행도 색상", "정확도 색상", "X정확도 색상",
            "음악 시간 색상", "맵 시간 색상", "최고 기록 색상",
            "진행 바 색상", "진행 바 배경 색상", "진행 바 테두리 색상",
            "콤보 색상", "BPM 색상", "색상 포인트 추가", "삭제", "퍼센트", "사용자 레이블",
            "일반", "디스플레이", "진행 및 정확도", "시간",
            "진행 바 및 최고 기록", "콤보", "BPM", "판정 및 기타",
            "표시 옵션", "동작 옵션", "FPS 새로고침 속도", "소수점 자릿수" ],

        /* 2  Chinese */ [ "大小", "显示进度", "显示准确率", "显示X准确率",
            "显示音乐时间", "显示地图时间", "无音乐时显示地图时间",
            "时间文本类型", "韩文", "英文",
            "显示检查点", "显示最佳",
            "显示进度条", "显示连击", "启用自动方块连击", "连击颜色最大值",
            "显示BPM", "BPM颜色最大值", "显示判定", "判定位置上移",
            "显示判定区间", "显示尝试次数", "显示总尝试次数",
            "Jongyeol模式", "显示FPS", "显示作者", "显示状态",
            "显示死亡", "显示开始", "显示时机",
            "隐藏调试文本", "自动模式简化显示", "检测伪BPM", "允许EL判定连击", "允许VL判定连击",
            "语言",
            "字体", "自定义位置", "重置位置",
            "文本设置", "主区域", "BPM", "判定", "连击标题", "连击数值", "判定区间", "尝试", "应用对齐", "重置",
            "B", "I", "U", "S", "H",
            "进度颜色", "准确率颜色", "X准确率颜色",
            "音乐时间颜色", "地图时间颜色", "最佳颜色",
            "进度条颜色", "进度条背景颜色", "进度条边框颜色",
            "连击颜色", "BPM颜色", "添加颜色点", "删除", "百分比", "自定义标签",
            "通用", "显示", "进度与准确率", "时间",
            "进度条与最佳", "连击", "BPM", "判定与其他",
            "显示选项", "行为选项", "FPS刷新率", "小数位数" ],
    ];

    public static string Get(Key key)
    {
        if (Main.Settings == null) return Data[0][(int)key];
        return Data[(int)Main.Settings.CurrentLanguage][(int)key];
    }
}
