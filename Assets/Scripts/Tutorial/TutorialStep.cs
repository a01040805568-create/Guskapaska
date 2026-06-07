using UnityEngine;
using Guskapaska.Core;

namespace Guskapaska.Tutorial
{
    /// <summary>이 단계에서 플레이어 카드 드래그를 어떻게 제한할지.</summary>
    public enum DragGate
    {
        /// <summary>모든 드래그 차단 (안내 단계).</summary>
        Block,
        /// <summary>모든 카드 드래그 허용.</summary>
        AllowAll,
        /// <summary>지정 모양 카드만 허용.</summary>
        AllowShape
    }

    /// <summary>
    /// 튜토리얼 한 단계의 데이터.
    /// 게임 규칙·용어는 00_GameDesign.md를 따르며 여기에 중복 기술하지 않는다.
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        // 안내 패널에 표시할 본문 텍스트.
        [TextArea(2, 5)]
        public string bodyText;

        // 강조할 대상. null이면 화면 전체 딤만 표시한다.
        public RectTransform highlightTarget;

        // 다음 단계로 넘어가는 조건.
        public TutorialAdvanceTrigger advanceTrigger = TutorialAdvanceTrigger.NextButton;

        // 이 단계의 입력 게이팅 모드.
        public DragGate dragGate = DragGate.Block;

        // dragGate가 AllowShape일 때 허용할 카드 모양.
        public CardShape allowedShape = CardShape.Scissors;
    }
}