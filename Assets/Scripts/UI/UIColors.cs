using UnityEngine;
 
namespace Guskapaska.UI
{
    /// <summary>
    /// Shared UI color constants for cards, gems, timer, and result screens.
    /// Card colors are placeholders until Stage 6 card art is finalized.
    /// </summary>
    public static class UIColors
    {
        // 카드 모양별 색상 (임시 디자인 — 카드 아트가 들어오면 교체됨)
        public static readonly Color Scissors = new Color(0.95f, 0.55f, 0.20f);  // 주황
        public static readonly Color Rock     = new Color(0.55f, 0.55f, 0.60f);  // 회색
        public static readonly Color Paper    = new Color(0.95f, 0.95f, 0.85f);  // 연한 베이지
        public static readonly Color CardBack = new Color(0.30f, 0.25f, 0.50f);  // 어두운 보라
 
        // 보석/코인 셀 상태
        public static readonly Color GemFilled = new Color(0.40f, 0.85f, 0.95f); // 시안 (보석 있음)
        public static readonly Color GemEmpty  = new Color(0.20f, 0.20f, 0.25f); // 어두운 회색
 
        // 타이머 강조
        public static readonly Color TimerNormal = Color.white;
        public static readonly Color TimerUrgent = new Color(1.0f, 0.30f, 0.30f); // 3초 이하 빨강
 
        // 결과 화면
        public static readonly Color ResultWin  = new Color(0.40f, 0.85f, 0.40f);
        public static readonly Color ResultLose = new Color(0.85f, 0.40f, 0.40f);
        public static readonly Color ResultTie  = new Color(0.85f, 0.85f, 0.40f);
    }
}