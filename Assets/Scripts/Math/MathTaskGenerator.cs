using System.Collections.Generic;
using UnityEngine;
using VacuumVille.Data;

namespace VacuumVille.Math
{
    /// <summary>
    /// Generates MathProblem instances for a given topic and difficulty.
    /// Uses adaptive difficulty signals from TopicAccuracy to step up/down.
    /// </summary>
    public static class MathTaskGenerator
    {
        private static readonly System.Random Rng = new();

        public static MathProblem Generate(MathTopic topic, TopicAccuracy accuracy)
        {
            Difficulty diff = ComputeDifficulty(accuracy);
            return topic switch
            {
                MathTopic.Counting1To10       => GenerateCounting(1, 10, diff),
                MathTopic.Counting1To20       => GenerateCounting(1, 20, diff),
                MathTopic.AdditionTo10        => GenerateAddition(10, diff),
                MathTopic.SubtractionWithin10 => GenerateSubtraction(10, diff),
                MathTopic.AdditionTo20        => GenerateAddition(20, diff),
                MathTopic.SubtractionWithin20 => GenerateSubtraction(20, diff),
                MathTopic.Multiplication2x5x  => GenerateMultiplication(diff),
                MathTopic.DivisionBy2_3_5     => GenerateDivision(diff),
                MathTopic.NumberOrdering      => GenerateOrdering(diff),
                MathTopic.ShapeCounting       => GenerateShapeCounting(diff),
                MathTopic.MixedReview         => GenerateMixed(accuracy),
                _                             => GenerateCounting(1, 10, Difficulty.Easy)
            };
        }

        // ── Adaptive difficulty ─────────────────────────────────────────────────

        private static Difficulty ComputeDifficulty(TopicAccuracy accuracy)
        {
            if (accuracy == null) return Difficulty.Easy;

            float rolling = accuracy.RollingAccuracy;

            if (rolling >= 0.85f && accuracy.currentDifficulty == Difficulty.Easy)
                accuracy.currentDifficulty = Difficulty.Medium;
            else if (rolling >= 0.85f && accuracy.currentDifficulty == Difficulty.Medium)
                accuracy.currentDifficulty = Difficulty.Hard;
            else if (rolling < 0.55f && accuracy.currentDifficulty == Difficulty.Hard)
                accuracy.currentDifficulty = Difficulty.Medium;
            else if (rolling < 0.55f && accuracy.currentDifficulty == Difficulty.Medium)
                accuracy.currentDifficulty = Difficulty.Easy;

            return accuracy.currentDifficulty;
        }

        // ── Generators ──────────────────────────────────────────────────────────

        private static MathProblem GenerateCounting(int min, int max, Difficulty diff)
        {
            int rangeMax = diff switch
            {
                Difficulty.Easy   => System.Math.Min(min + 5, max),
                Difficulty.Medium => System.Math.Min(min + 9, max),
                _                 => max
            };

            int count = Rng.Next(min, rangeMax + 1);
            var choices = GenerateChoices(count, min, max);

            string visualKey = PickCountingVisual();

            return new MathProblem
            {
                topic          = count <= 10 ? MathTopic.Counting1To10 : MathTopic.Counting1To20,
                difficulty     = diff,
                format         = ProblemFormat.MultipleChoice,
                questionTextKey = "q_how_many",
                operands       = new[] { count },
                correctAnswer  = count,
                choices        = choices,
                visualAssetKey = visualKey,
                visualCount    = count,
                voiceLineKey   = "q_how_many"
            };
        }

        private static MathProblem GenerateAddition(int maxSum, Difficulty diff)
        {
            int a, b;
            switch (diff)
            {
                case Difficulty.Easy:
                    a = Rng.Next(1, 5); b = Rng.Next(1, maxSum / 2 - a + 1); break;
                case Difficulty.Medium:
                    a = Rng.Next(1, maxSum / 2); b = Rng.Next(1, maxSum - a + 1); break;
                default:
                    a = Rng.Next(maxSum / 2, maxSum); b = Rng.Next(0, maxSum - a + 1); break;
            }
            int sum = a + b;
            bool isMissing = diff == Difficulty.Hard && Rng.Next(2) == 0;
            int correct = isMissing ? a : sum;
            var choices = GenerateChoices(correct, 0, maxSum);

            return new MathProblem
            {
                topic          = maxSum <= 10 ? MathTopic.AdditionTo10 : MathTopic.AdditionTo20,
                difficulty     = diff,
                format         = isMissing ? ProblemFormat.FillBlank : ProblemFormat.MultipleChoice,
                questionTextKey = isMissing ? "q_missing_addition" : "q_addition",
                questionTextFallback = isMissing ? $"___ + {b} = {sum}" : $"{a} + {b} = ___",
                operands       = new[] { a, b },
                operatorSymbol = "+",
                correctAnswer  = correct,
                choices        = choices,
                voiceLineKey   = "q_addition"
            };
        }

        private static MathProblem GenerateSubtraction(int maxNum, Difficulty diff)
        {
            int a, b;
            switch (diff)
            {
                case Difficulty.Easy:
                    a = Rng.Next(2, maxNum / 2 + 1); b = Rng.Next(1, a); break;
                case Difficulty.Medium:
                    a = Rng.Next(maxNum / 2, maxNum + 1); b = Rng.Next(1, a); break;
                default:
                    a = Rng.Next(maxNum - 3, maxNum + 1); b = Rng.Next(a - 5, a); break;
            }
            b = System.Math.Max(0, b);
            int answer = a - b;
            var choices = GenerateChoices(answer, 0, maxNum);

            bool isMissing = diff == Difficulty.Hard && Rng.Next(2) == 0;

            return new MathProblem
            {
                topic          = maxNum <= 10 ? MathTopic.SubtractionWithin10 : MathTopic.SubtractionWithin20,
                difficulty     = diff,
                format         = isMissing ? ProblemFormat.FillBlank : ProblemFormat.MultipleChoice,
                questionTextKey = "q_subtraction",
                questionTextFallback = $"{a} - {b} = ___",
                operands       = new[] { a, b },
                operatorSymbol = "-",
                correctAnswer  = answer,
                choices        = choices,
                voiceLineKey   = "q_subtraction"
            };
        }

        private static MathProblem GenerateMultiplication(Difficulty diff)
        {
            int[] multipliers = diff == Difficulty.Easy ? new[] { 2 } : new[] { 2, 5 };
            int m = multipliers[Rng.Next(multipliers.Length)];
            int n = diff switch
            {
                Difficulty.Easy   => Rng.Next(1, 6),
                Difficulty.Medium => Rng.Next(1, 9),
                _                 => Rng.Next(1, 11)
            };
            int answer = m * n;
            var choices = GenerateChoices(answer, 0, 55);

            return new MathProblem
            {
                topic          = MathTopic.Multiplication2x5x,
                difficulty     = diff,
                format         = ProblemFormat.MultipleChoice,
                questionTextKey = "q_multiplication",
                questionTextFallback = $"{n} skupin po {m} = ___",
                operands       = new[] { m, n },
                operatorSymbol = "×",
                correctAnswer  = answer,
                choices        = choices,
                voiceLineKey   = "q_multiplication"
            };
        }

        private static MathProblem GenerateDivision(Difficulty diff)
        {
            int[] divisors = diff == Difficulty.Easy ? new[] { 2 } : new[] { 2, 3, 5 };
            int d = divisors[Rng.Next(divisors.Length)];
            int quotient = diff switch
            {
                Difficulty.Easy   => Rng.Next(1, 6),
                Difficulty.Medium => Rng.Next(1, 8),
                _                 => Rng.Next(1, 11)
            };
            int dividend = d * quotient;
            var choices = GenerateChoices(quotient, 1, 12);

            return new MathProblem
            {
                topic          = MathTopic.DivisionBy2_3_5,
                difficulty     = diff,
                format         = ProblemFormat.MultipleChoice,
                questionTextKey = "q_division",
                questionTextFallback = $"{dividend} ÷ {d} = ___",
                operands       = new[] { dividend, d },
                operatorSymbol = "÷",
                correctAnswer  = quotient,
                choices        = choices,
                voiceLineKey   = "q_division"
            };
        }

        private static MathProblem GenerateOrdering(Difficulty diff)
        {
            int count = diff == Difficulty.Easy ? 4 : diff == Difficulty.Medium ? 5 : 6;
            int skip  = diff == Difficulty.Easy ? 1 : diff == Difficulty.Medium
                ? new[] { 2, 5 }[Rng.Next(2)]
                : new[] { 2, 5, 10 }[Rng.Next(3)];

            int start = Rng.Next(1, 91);
            var seq = new List<int>();
            for (int i = 0; i < count; i++) seq.Add(start + i * skip);

            int missingIdx = Rng.Next(1, count - 1); // never first or last
            int answer = seq[missingIdx];
            seq[missingIdx] = -1; // placeholder

            var choices = GenerateChoices(answer, seq[0], seq[seq.Count - 1] + skip);

            return new MathProblem
            {
                topic          = MathTopic.NumberOrdering,
                difficulty     = diff,
                format         = ProblemFormat.FillBlank,
                questionTextKey = "q_ordering",
                sequence       = seq,
                missingIndex   = missingIdx,
                correctAnswer  = answer,
                choices        = choices,
                voiceLineKey   = "q_ordering"
            };
        }

        private static MathProblem GenerateShapeCounting(Difficulty diff)
        {
            string[] shapes = { "triangle", "square", "circle", "rectangle", "pentagon", "hexagon" };
            int shapeIdx = Rng.Next(diff == Difficulty.Easy ? 3 : shapes.Length);
            int count = diff switch
            {
                Difficulty.Easy   => Rng.Next(1, 6),
                Difficulty.Medium => Rng.Next(3, 10),
                _                 => Rng.Next(5, 15)
            };
            var choices = GenerateChoices(count, 0, 16);

            return new MathProblem
            {
                topic          = MathTopic.ShapeCounting,
                difficulty     = diff,
                format         = ProblemFormat.MultipleChoice,
                questionTextKey = "q_count_shapes",
                operands       = new[] { shapeIdx, count },
                correctAnswer  = count,
                choices        = choices,
                visualAssetKey = shapes[shapeIdx],
                visualCount    = count,
                voiceLineKey   = "q_count_shapes"
            };
        }

        private static MathProblem GenerateMixed(TopicAccuracy accuracy)
        {
            MathTopic[] allTopics = (MathTopic[])System.Enum.GetValues(typeof(MathTopic));
            // Exclude MixedReview itself
            var pool = new List<MathTopic>();
            foreach (var t in allTopics)
                if (t != MathTopic.MixedReview) pool.Add(t);

            MathTopic chosen = pool[Rng.Next(pool.Count)];
            return Generate(chosen, accuracy);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────

        private static int[] GenerateChoices(int correct, int min, int max)
        {
            var choices = new HashSet<int> { correct };
            int attempts = 0;
            while (choices.Count < 3 && attempts < 50)
            {
                int wrong = correct + (Rng.Next(2) == 0 ? 1 : -1) * Rng.Next(1, 4);
                wrong = System.Math.Max(min, System.Math.Min(max, wrong));
                if (wrong != correct) choices.Add(wrong);
                attempts++;
            }
            // If still not 3 (edge case), pad
            int pad = min;
            while (choices.Count < 3) { if (!choices.Contains(pad)) choices.Add(pad); pad++; }

            var arr = new List<int>(choices);
            // Shuffle
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int j = Rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr.ToArray();
        }

        private static readonly string[] CountingVisuals =
            { "star", "sock", "cushion", "toy", "crumb", "duck", "blocks", "flower" };

        private static string PickCountingVisual()
            => CountingVisuals[Rng.Next(CountingVisuals.Length)];
    }
}
