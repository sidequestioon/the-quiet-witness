using QuietWitness.Core;

namespace QuietWitness.Board
{
    // One card on the board: a found clue or a reached conclusion.
    public class BoardCard
    {
        public string Flag { get; }
        public string Title { get; }
        public string Description { get; }
        public bool IsConclusion { get; }

        private BoardCard(string flag, string title, string description, bool isConclusion)
        {
            Flag = flag;
            Title = title;
            Description = description;
            IsConclusion = isConclusion;
        }

        public static BoardCard From(ClueData clue) =>
            new BoardCard(clue.Flag, clue.Title, clue.Description, false);

        public static BoardCard From(ConclusionData conclusion) =>
            new BoardCard(conclusion.Flag, conclusion.Title, conclusion.Description, true);
    }
}
