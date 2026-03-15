using System.Collections.Generic;

namespace VacuumVille.Data
{
    public enum ProblemFormat
    {
        MultipleChoice,     // standard A/B/C choice
        DragDrop,           // drag items to answer
        FillBlank,          // tap correct number for a blank
        Ordering,           // drag tiles into correct order
        Matching            // match items to groups
    }

    public class MathProblem
    {
        // Core problem data
        public MathTopic topic;
        public Difficulty difficulty;
        public ProblemFormat format;

        // Display
        public string questionTextKey;          // localization key
        public string questionTextFallback;     // plain text fallback (no localization needed for pure math)
        public string equationText;             // the math expression shown in the equation area (e.g. "? + 1 = 20")
        public int[] operands;                  // e.g. [3, 5] for 3+5
        public string operatorSymbol;           // "+", "-", "×", "÷"
        public int correctAnswer;

        // For ordering/sequence problems
        public List<int> sequence;
        public int missingIndex;

        // Multiple choice
        public int[] choices;                   // always 3 choices, correctAnswer is one of them

        // Visual support
        public string visualAssetKey;           // sprite key for countable objects
        public int visualCount;                 // how many objects to show

        // Voice
        public string voiceLineKey;             // key to look up pre-recorded audio clip

        // Metadata
        public bool requiresReadAloud = true;
        public bool showHintButton = true;
    }
}
