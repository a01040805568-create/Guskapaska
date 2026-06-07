namespace Guskapaska.Core
{
    /// <summary>
    /// AI 카드 선택 전략의 공통 인터페이스.
    /// 라운드 시작 시 AI 손패에서 낼 카드 한 장을 선택한다.
    /// 무작위 전략(AiRandomStrategy)과 튜토리얼용 스크립트 전략(ScriptedAiStrategy)이 구현한다.
    /// </summary>
    public interface IAiStrategy
    {
        /// <summary>
        /// AI 손패에서 이번 라운드에 낼 카드를 선택한다. 손패는 변경하지 않는다.
        /// </summary>
        /// <param name="aiHand">AI의 현재 손패. 비어 있으면 안 된다.</param>
        /// <returns>선택된 카드.</returns>
        Card SelectCard(Hand aiHand);
    }
}