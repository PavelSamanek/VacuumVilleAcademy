using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using VacuumVille.Data;
using VacuumVille.Math;

namespace VacuumVille.Tests
{
    /// <summary>
    /// Edit Mode tests for MathTaskGenerator.
    /// Run via Window > General > Test Runner > EditMode.
    /// </summary>
    public class MathTaskGeneratorTests
    {
        // ── Helpers ─────────────────────────────────────────────────────────────

        private static TopicAccuracy Accuracy(Difficulty d)
        {
            var ta = new TopicAccuracy { currentDifficulty = d };
            // Fill rolling window so difficulty won't change mid-test
            bool allCorrect = d == Difficulty.Hard || d == Difficulty.Medium;
            for (int i = 0; i < 10; i++)
                ta.recentResults.Enqueue(allCorrect);
            return ta;
        }

        private static MathProblem Gen(MathTopic topic, Difficulty diff = Difficulty.Easy)
            => MathTaskGenerator.Generate(topic, Accuracy(diff));

        // ── Invariant: correct answer is always in choices ───────────────────

        [Test]
        public void CorrectAnswer_IsAlwaysInChoices([Values(
            MathTopic.Counting1To10,
            MathTopic.Counting1To20,
            MathTopic.AdditionTo10,
            MathTopic.SubtractionWithin10,
            MathTopic.AdditionTo20,
            MathTopic.SubtractionWithin20,
            MathTopic.Multiplication2x5x,
            MathTopic.DivisionBy2_3_5,
            MathTopic.NumberOrdering,
            MathTopic.ShapeCounting
        )] MathTopic topic)
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(topic, (Difficulty)(run % 3));
                Assert.IsNotNull(p.choices, $"choices is null for {topic} run {run}");
                Assert.Contains(p.correctAnswer, p.choices,
                    $"{topic} run {run}: correctAnswer={p.correctAnswer} not in [{string.Join(",", p.choices)}]");
            }
        }

        [Test]
        public void CorrectAnswer_IsAlwaysInChoices_HardAddition()
        {
            // Hard addition can produce fill-blank (missing-addend) problems.
            // Before the fix the correctAnswer was the sum; now it must be the missing addend.
            for (int run = 0; run < 500; run++)
            {
                var p = Gen(MathTopic.AdditionTo10, Difficulty.Hard);
                Assert.Contains(p.correctAnswer, p.choices,
                    $"Hard AdditionTo10 run {run}: correctAnswer={p.correctAnswer} not in [{string.Join(",", p.choices)}]");
            }
        }

        // ── Exactly 3 distinct choices ───────────────────────────────────────

        [Test]
        public void Choices_AlwaysExactlyThreeDistinct([Values(
            MathTopic.Counting1To10,
            MathTopic.AdditionTo10,
            MathTopic.SubtractionWithin10,
            MathTopic.AdditionTo20,
            MathTopic.Multiplication2x5x,
            MathTopic.DivisionBy2_3_5
        )] MathTopic topic)
        {
            for (int run = 0; run < 100; run++)
            {
                var p = Gen(topic);
                Assert.AreEqual(3, p.choices.Length,
                    $"{topic}: expected 3 choices, got {p.choices.Length}");
                Assert.AreEqual(3, p.choices.Distinct().Count(),
                    $"{topic}: choices contain duplicates [{string.Join(",", p.choices)}]");
            }
        }

        // ── Addition ─────────────────────────────────────────────────────────

        [Test]
        public void Addition_NormalFormat_AnswerEqualsSum()
        {
            for (int run = 0; run < 300; run++)
            {
                var p = Gen(MathTopic.AdditionTo10);
                if (p.format != ProblemFormat.MultipleChoice) continue;
                int expected = p.operands[0] + p.operands[1];
                Assert.AreEqual(expected, p.correctAnswer,
                    $"AdditionTo10 normal: {p.operands[0]}+{p.operands[1]} expected {expected}, got {p.correctAnswer}");
            }
        }

        [Test]
        public void Addition_FillBlank_AnswerIsMissingAddend()
        {
            // For ___ + b = sum, correctAnswer must be 'a', so a + b == sum
            int fillBlankSeen = 0;
            for (int run = 0; run < 1000; run++)
            {
                var p = Gen(MathTopic.AdditionTo10, Difficulty.Hard);
                if (p.format != ProblemFormat.FillBlank) continue;
                fillBlankSeen++;
                int a = p.operands[0];
                int b = p.operands[1];
                int sum = a + b;
                Assert.AreEqual(a, p.correctAnswer,
                    $"FillBlank: correctAnswer should be missing addend {a}, got {p.correctAnswer} (b={b}, sum={sum})");
                Assert.LessOrEqual(sum, 10,
                    $"FillBlank: sum {sum} exceeds AdditionTo10 max");
            }
            // We should see at least some fill-blank problems in 1000 hard runs
            Assert.Greater(fillBlankSeen, 0, "No FillBlank addition problems generated in 1000 Hard runs");
        }

        [Test]
        public void Addition_AnswerDoesNotExceedMax([Values(
            MathTopic.AdditionTo10,
            MathTopic.AdditionTo20
        )] MathTopic topic)
        {
            int max = topic == MathTopic.AdditionTo10 ? 10 : 20;
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(topic, (Difficulty)(run % 3));
                if (p.format == ProblemFormat.MultipleChoice)
                {
                    Assert.LessOrEqual(p.correctAnswer, max,
                        $"{topic}: answer {p.correctAnswer} exceeds max {max}");
                    Assert.GreaterOrEqual(p.correctAnswer, 0);
                }
            }
        }

        // ── Subtraction ──────────────────────────────────────────────────────

        [Test]
        public void Subtraction_AnswerIsNonNegative([Values(
            MathTopic.SubtractionWithin10,
            MathTopic.SubtractionWithin20
        )] MathTopic topic)
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(topic, (Difficulty)(run % 3));
                Assert.GreaterOrEqual(p.correctAnswer, 0,
                    $"{topic}: answer {p.correctAnswer} is negative");
            }
        }

        [Test]
        public void Subtraction_AnswerEqualsAMinusB([Values(
            MathTopic.SubtractionWithin10,
            MathTopic.SubtractionWithin20
        )] MathTopic topic)
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(topic, (Difficulty)(run % 3));
                if (p.format != ProblemFormat.MultipleChoice) continue;
                int expected = p.operands[0] - p.operands[1];
                Assert.AreEqual(expected, p.correctAnswer,
                    $"{topic}: {p.operands[0]}-{p.operands[1]} expected {expected}, got {p.correctAnswer}");
            }
        }

        // ── Multiplication ───────────────────────────────────────────────────

        [Test]
        public void Multiplication_AnswerEqualsProduct()
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(MathTopic.Multiplication2x5x, (Difficulty)(run % 3));
                int expected = p.operands[0] * p.operands[1];
                Assert.AreEqual(expected, p.correctAnswer,
                    $"Multiplication: {p.operands[0]}×{p.operands[1]} expected {expected}, got {p.correctAnswer}");
            }
        }

        [Test]
        public void Multiplication_MultiplierIsOnly2Or5()
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(MathTopic.Multiplication2x5x, (Difficulty)(run % 3));
                int multiplier = p.operands[0];
                Assert.That(multiplier == 2 || multiplier == 5,
                    $"Multiplier {multiplier} is not 2 or 5");
            }
        }

        // ── Division ─────────────────────────────────────────────────────────

        [Test]
        public void Division_AnswerIsExactQuotient()
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(MathTopic.DivisionBy2_3_5, (Difficulty)(run % 3));
                int dividend = p.operands[0];
                int divisor  = p.operands[1];
                Assert.AreEqual(0, dividend % divisor,
                    $"Division {dividend}÷{divisor} is not exact");
                Assert.AreEqual(dividend / divisor, p.correctAnswer,
                    $"Division: {dividend}÷{divisor} expected {dividend / divisor}, got {p.correctAnswer}");
            }
        }

        [Test]
        public void Division_DivisorIsOnly2Or3Or5()
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(MathTopic.DivisionBy2_3_5, (Difficulty)(run % 3));
                int d = p.operands[1];
                Assert.That(d == 2 || d == 3 || d == 5,
                    $"Divisor {d} is not 2, 3, or 5");
            }
        }

        // ── Counting ─────────────────────────────────────────────────────────

        [Test]
        public void Counting_AnswerWithinRange([Values(
            MathTopic.Counting1To10,
            MathTopic.Counting1To20
        )] MathTopic topic)
        {
            int max = topic == MathTopic.Counting1To10 ? 10 : 20;
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(topic, (Difficulty)(run % 3));
                Assert.GreaterOrEqual(p.correctAnswer, 1,
                    $"{topic}: answer {p.correctAnswer} < 1");
                Assert.LessOrEqual(p.correctAnswer, max,
                    $"{topic}: answer {p.correctAnswer} > max {max}");
            }
        }

        // ── Number Ordering ──────────────────────────────────────────────────

        [Test]
        public void Ordering_MissingElementIsInSequence()
        {
            for (int run = 0; run < 200; run++)
            {
                var p = Gen(MathTopic.NumberOrdering, (Difficulty)(run % 3));
                Assert.IsNotNull(p.sequence, $"Ordering run {run}: sequence is null");
                // The missing index should hold -1
                Assert.AreEqual(-1, p.sequence[p.missingIndex],
                    $"Ordering: sequence[missingIndex] should be -1");
                // correctAnswer should make the sequence consistent
                var full = new List<int>(p.sequence) { [p.missingIndex] = p.correctAnswer };
                // Check that the sequence increases monotonically
                for (int i = 1; i < full.Count; i++)
                    Assert.Greater(full[i], full[i - 1],
                        $"Ordering: sequence not monotonically increasing at index {i}");
            }
        }

        // ── MixedReview ──────────────────────────────────────────────────────

        [Test]
        public void MixedReview_GeneratesValidProblem()
        {
            for (int run = 0; run < 100; run++)
            {
                var p = Gen(MathTopic.MixedReview);
                Assert.IsNotNull(p);
                Assert.IsNotNull(p.choices);
                Assert.Contains(p.correctAnswer, p.choices);
                Assert.AreNotEqual(MathTopic.MixedReview, p.topic,
                    "MixedReview should return a problem from a concrete topic");
            }
        }
    }
}
