using UnityEngine;

namespace Guskapaska.Tutorial
{
    /// <summary>
    /// 튜토리얼 한 단계의 데이터. Inspector에서 직렬화되어 시퀀스로 편집된다.
    /// 게임 규칙·용어는 00_GameDesign.md를 따르며 여기에 중복 기술하지 않는다.
    /// </summary>
    [System.Serializable]
    public class TutorialStep
    {
        // 안내 패널에 표시할 본문 텍스트.
        [TextArea(2, 5)]
        public string bodyText;

        // 이 단계에서 강조할 대상. null이면 화면 전체 딤만 표시하고 하이라이트 박스는 숨긴다.
        public RectTransform highlightTarget;

        // 다음 단계로 넘어가는 조건.
        public TutorialAdvanceTrigger advanceTrigger = TutorialAdvanceTrigger.NextButton;

        // 이 단계 동안 플레이어 카드 드래그를 허용할지 여부.
        // 보통 "다음 버튼" 안내 단계는 false, "직접 카드를 내보세요" 단계는 true.
        public bool allowPlayerDrag = false;
    }
}