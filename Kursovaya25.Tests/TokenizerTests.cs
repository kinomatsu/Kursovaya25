using Kursovaya25;
using NUnit.Framework;

namespace Kursovaya25.Tests
{
    /// <summary>
    /// Тесты для алгоритма токенизации текста (WordAnalyzer.Tokenize).
    /// Токенизация: разбиение на слова, удаление пунктуации.
    /// </summary>
    [TestFixture]
    public class TokenizerTests
    {
        private WordAnalyzer _analyzer = null!;

        [SetUp]
        public void SetUp()
        {
            _analyzer = new WordAnalyzer();
        }

        [Test]
        public void Tokenize_SimpleText_ReturnsCorrectWords()
        {
            var words = _analyzer.Tokenize("Привет мир");
            Assert.That(words, Is.EqualTo(new[] { "привет", "мир" }));
        }

        [Test]
        public void Tokenize_TextWithPunctuation_PunctuationRemoved()
        {
            var words = _analyzer.Tokenize("Привет, мир! Как дела?");
            Assert.That(words, Is.EqualTo(new[] { "привет", "мир", "как", "дела" }));
        }

        [Test]
        public void Tokenize_UpperCase_ConvertedToLower()
        {
            var words = _analyzer.Tokenize("АЛГОРИТМ Сортировки");
            Assert.That(words, Is.EqualTo(new[] { "алгоритм", "сортировки" }));
        }

        [Test]
        public void Tokenize_EmptyString_ReturnsEmptyList()
        {
            var words = _analyzer.Tokenize("");
            Assert.That(words, Is.Empty);
        }

        [Test]
        public void Tokenize_OnlyPunctuation_ReturnsEmptyList()
        {
            var words = _analyzer.Tokenize("!!! ??? ... ---");
            Assert.That(words, Is.Empty);
        }

        [Test]
        public void Tokenize_SingleWord_ReturnsSingleToken()
        {
            var words = _analyzer.Tokenize("программирование");
            Assert.That(words.Count, Is.EqualTo(1));
            Assert.That(words[0], Is.EqualTo("программирование"));
        }

        [Test]
        public void Tokenize_HyphenatedWord_TreatedAsOneToken()
        {
            var words = _analyzer.Tokenize("хеш-таблица");
            Assert.That(words.Count, Is.EqualTo(1));
            Assert.That(words[0], Is.EqualTo("хеш-таблица"));
        }

        [Test]
        public void Tokenize_NumbersInText_NumbersIgnored()
        {
            var words = _analyzer.Tokenize("версия 3.14 алгоритма");
            Assert.That(words, Does.Not.Contain("3"));
            Assert.That(words, Does.Not.Contain("14"));
            Assert.That(words, Does.Contain("версия"));
            Assert.That(words, Does.Contain("алгоритма"));
        }

        [Test]
        [Timeout(3000)]
        public void Tokenize_LargeText_CompletesInTime()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 100_000; i++)
                sb.Append("слово ");
            var words = _analyzer.Tokenize(sb.ToString());
            Assert.That(words.Count, Is.EqualTo(100_000));
        }
    }
}
