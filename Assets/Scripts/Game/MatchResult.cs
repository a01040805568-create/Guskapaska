namespace Guskapaska.Game
{
    /// <summary>
    /// Who won the overall match. Tie is rare but possible (equal gems at end).
    /// </summary>
    public enum MatchWinner
    {
        Player,
        Ai,
        Tie
    }

    /// <summary>
    /// Immutable summary of a finished match. Produced by GameManager on match end.
    /// </summary>
    public class MatchResult
    {
        public MatchWinner Winner { get; }
        public int PlayerGems { get; }
        public int AiGems { get; }
        public int TotalRounds { get; }

        // 종료 사유 문자열: "CenterEmpty" / "PlayerOutOfCards" / "AiOutOfCards"
        public string EndReason { get; }

        public MatchResult(MatchWinner winner, int playerGems, int aiGems, int totalRounds, string endReason)
        {
            Winner = winner;
            PlayerGems = playerGems;
            AiGems = aiGems;
            TotalRounds = totalRounds;
            EndReason = endReason;
        }
    }
}