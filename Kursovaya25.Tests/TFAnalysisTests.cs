using Kursovaya25;
using NUnit.Framework;

namespace Kursovaya25.Tests
{
    /// <summary>
    /// Тесты для алгоритма TF (Term Frequency) и метода Analyze в целом.
    /// TF(t) = count(t) / totalWords
    /// </summary>
    [TestFixture]
    public class TFAnalysisTests
    {
        private WordAnalyzer _analyzer = null!;

        [SetUp]
        public void SetUp()
        {
            _analyzer = new WordAnalyzer();
            _analyzer.UseStopWords = false;
        }

        [Test]
        public void Analyze_TF_CorrectForKnownText()
        {
            var result = _analyzer.Analyze("кот кот пёс");

            WordEntry? kotEntry = null;
            foreach (var e in result.TopWords)
                if (e.Word == "кот") { kotEntry = e; break; }

            Assert.That(kotEntry, Is.Not.Null, "Слово 'кот' должно быть в результатах");
            Assert.That(kotEntry!.TF, Is.EqualTo(2.0 / 3.0).Within(1e-9));
        }

        [Test]
        public void Analyze_TF_SumApproximatelyOne()
        {
            var result = _analyzer.Analyze("яблоко груша банан яблоко слива груша яблоко");

            double sum = 0;
            foreach (var e in result.TopWords)
                sum += e.TF;

            Assert.That(sum, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void Analyze_SingleUniqueWord_TFIsOne()
        {
            var result = _analyzer.Analyze("алгоритм алгоритм алгоритм");

            Assert.That(result.TopWords.Length, Is.EqualTo(1));
            Assert.That(result.TopWords[0].TF, Is.EqualTo(1.0).Within(1e-9));
        }

        [Test]
        public void Analyze_EmptyText_ReturnsZeroStats()
        {
            var result = _analyzer.Analyze("");

            Assert.That(result.TotalWords, Is.EqualTo(0));
            Assert.That(result.UniqueWords, Is.EqualTo(0));
            Assert.That(result.TopWords, Is.Empty);
        }

        [Test]
        public void Analyze_OnlyPunctuation_ReturnsEmptyResult()
        {
            var result = _analyzer.Analyze("!!! ??? ... ,,, ---");

            Assert.That(result.TotalWords, Is.EqualTo(0));
            Assert.That(result.TopWords, Is.Empty);
        }

        [Test]
        public void Analyze_Statistics_CorrectValues()
        {
            var result = _analyzer.Analyze("кот пёс рыба кот");

            Assert.That(result.TotalWords, Is.EqualTo(4));
            Assert.That(result.UniqueWords, Is.EqualTo(3));
        }

        [Test]
        public void Analyze_LongestWord_FoundCorrectly()
        {
            var result = _analyzer.Analyze("кот программирование пёс");
            Assert.That(result.LongestWord, Is.EqualTo("программирование"));
        }

        [Test]
        public void Analyze_TopWords_NotMoreThan50()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 100; i++)
                sb.Append("уникальноеслово" + i + " ");
            var result = _analyzer.Analyze(sb.ToString());

            Assert.That(result.TopWords.Length, Is.LessThanOrEqualTo(50));
        }

        [Test]
        public void Analyze_TopWords_SortedByCountDescending()
        {
            var result = _analyzer.Analyze("яблоко яблоко яблоко груша груша банан");

            for (int i = 1; i < result.TopWords.Length; i++)
                Assert.That(result.TopWords[i].Count,
                    Is.LessThanOrEqualTo(result.TopWords[i - 1].Count),
                    $"Нарушение порядка на позиции {i}");
        }

        [Test]
        public void Analyze_WithStopWords_StopWordsExcluded()
        {
            _analyzer.UseStopWords = true;
            var result = _analyzer.Analyze("алгоритм и сортировка в массиве");

            foreach (var e in result.TopWords)
            {
                Assert.That(e.Word, Is.Not.EqualTo("и"));
                Assert.That(e.Word, Is.Not.EqualTo("в"));
            }
        }

        [Test]
        [Timeout(5000)]
        public void Analyze_LargeText_CompletesInTime()
        {
            var sb = new System.Text.StringBuilder();
            var rng = new System.Random(42);
            string[] vocab = { "алгоритм", "данные", "структура", "сортировка",
                               "поиск", "граф", "дерево", "хеш", "стек", "очередь" };
            for (int i = 0; i < 50_000; i++)
                sb.Append(vocab[rng.Next(vocab.Length)] + " ");

            var result = _analyzer.Analyze(sb.ToString());
            Assert.That(result.TotalWords, Is.GreaterThan(0));
            Assert.That(result.TopWords.Length, Is.EqualTo(10));
        }

        [Test]
        public void Analyze_LengthDistribution_CorrectBuckets()
        {
            var result = _analyzer.Analyze("кот пёс рыба");

            Assert.That(result.LengthDistribution[3], Is.EqualTo(2));
            Assert.That(result.LengthDistribution[4], Is.EqualTo(1));
        }
    }
}
