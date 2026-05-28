using System.Collections.Generic;

namespace JipperOverlayer.Overlayer.Localization;

public static class Tr
{
    private static readonly Dictionary<string, string[]> _strings = new()
    {
        // ===== Settings UI =====
        ["size"] =             ["Size", "크기", "大小"],
        ["show_progress"] =    ["Show Progress", "진행도 표시", "显示进度"],
        ["show_accuracy"] =    ["Show Accuracy", "정확도 표시", "显示准确率"],
        ["show_xaccuracy"] =   ["Show XAccuracy", "X정확도 표시", "显示X准确率"],
        ["show_music_time"] =  ["Show Music Time", "음악 시간 표시", "显示音乐时间"],
        ["show_map_time"] =    ["Show Map Time", "맵 시간 표시", "显示地图时间"],
        ["show_map_if_no"] =   ["Show Map Time If No Music", "음악 없을 때 맵 시간", "无音乐时显示地图时间"],
        ["time_text_type"] =   ["Time Text Type", "시간 텍스트 타입", "时间文本类型"],
        ["show_checkpoint"] =  ["Show Checkpoint", "체크포인트 표시", "显示检查点"],
        ["show_best"] =        ["Show Best", "최고 기록 표시", "显示最佳"],
        ["show_progress_bar"] =["Show Progress Bar", "진행 바 표시", "显示进度条"],
        ["show_combo"] =       ["Show Combo", "콤보 표시", "显示连击"],
        ["enable_auto_combo"] =["Enable Auto Combo", "자동 콤보 활성화", "启用自动连击"],
        ["combo_color_max"] =  ["Combo Color Max", "콤보 색상 최대", "连击颜色最大值"],
        ["show_bpm"] =         ["Show BPM", "BPM 표시", "显示BPM"],
        ["bpm_color_max"] =    ["BPM Color Max", "BPM 색상 최대", "BPM颜色最大值"],
        ["show_judgement"] =   ["Show Judgement", "판정 표시", "显示判定"],
        ["judgement_up"] =     ["Judgement Location Up", "판정 위치 위로", "判定位置上移"],
        ["show_timing_scale"] =["Show Timing Scale", "타이밍 스케일 표시", "显示时机缩放"],
        ["show_attempt"] =     ["Show Attempt", "시도 횟수 표시", "显示尝试次数"],
        ["show_full_attempt"] =["Show Full Attempt", "전체 시도 표시", "显示总尝试次数"],
        ["jongyeol_mode"] =    ["Jongyeol Mode", "종열 모드", "Jongyeol模式"],
        ["show_fps"] =         ["Show FPS", "FPS 표시", "显示FPS"],
        ["show_author"] =      ["Show Author", "제작자 표시", "显示作者"],
        ["show_state"] =       ["Show State", "상태 표시", "显示状态"],
        ["show_death"] =       ["Show Death", "사망 표시", "显示死亡"],
        ["show_start"] =       ["Show Start", "시작 표시", "显示开始"],
        ["show_timing"] =      ["Show Timing", "타이밍 표시", "显示时机"],
        ["hide_debug_text"] =  ["Hide Debug Text", "디버그 텍스트 숨기기", "隐藏调试文本"],
        ["remove_auto_req"] =  ["Remove Not Required In Auto", "자동에서 불필요한 것 제거", "自动模式简化显示"],
        ["check_pseudo"] =     ["Check Pseudo", "의사 BPM 확인", "检测伪BPM"],
        ["yellow_combo"] =     ["Yellow Combo", "노란 콤보", "黄色连击"],

        // ===== Language selector =====
        ["lang_label"] = ["Language", "언어", "语言"],

        // ===== Color labels =====
        ["progress_color"] =           ["Progress Color", "진행도 색상", "进度颜色"],
        ["accuracy_color"] =           ["Accuracy Color", "정확도 색상", "准确率颜色"],
        ["xaccuracy_color"] =          ["XAccuracy Color", "X정확도 색상", "X准确率颜色"],
        ["music_time_color"] =         ["Music Time Color", "음악 시간 색상", "音乐时间颜色"],
        ["map_time_color"] =           ["Map Time Color", "맵 시간 색상", "地图时间颜色"],
        ["best_color"] =               ["Best Color", "최고 기록 색상", "最佳颜色"],
        ["progress_bar_color"] =       ["Progress Bar Color", "진행 바 색상", "进度条颜色"],
        ["progress_bar_bg_color"] =    ["Progress Bar Background Color", "진행 바 배경 색상", "进度条背景颜色"],
        ["progress_bar_border_color"] =["Progress Bar Border Color", "진행 바 테두리 색상", "进度条边框颜色"],
        ["combo_color"] =              ["Combo Color", "콤보 색상", "连击颜色"],
        ["bpm_color"] =                ["BPM Color", "BPM 색상", "BPM颜色"],
        ["add_color_stop"] =           ["Add Color Stop", "색상 포인트 추가", "添加颜色点"],
        ["delete"] =                   ["Delete", "삭제", "删除"],
        ["percent"] =                  ["Percent", "퍼센트", "百分比"],
    };

    public static string Get(string key)
    {
        var lang = Main.Settings.Language;
        if (_strings.TryGetValue(key, out var values) && (int)lang < values.Length)
            return values[(int)lang];
        return key;
    }
}
