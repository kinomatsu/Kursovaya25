using Kursovaya25;
using NUnit.Framework;

namespace Kursovaya25.Tests
{
    /// <summary>
    /// Тесты для хеш-таблицы WordHashTable.
    /// Алгоритм: открытая адресация с линейным пробированием.
    /// </summary>
    [TestFixture]
    public class WordHashTableTests
    {
        //1. Корректность на типичных данных
        [Test]
        public void Increment_NewWord_CountIsOne()
        {
            var table = new WordHashTable();
            table.Increment("алгоритм");

            var entries = table.GetAllEntries();
            Assert.That(entries.Length, Is.EqualTo(1));
            Assert.That(entries[0].Word, Is.EqualTo("алгоритм"));
            Assert.That(entries[0].Count, Is.EqualTo(1));
        }

        [Test]
        public void Increment_SameWordMultipleTimes_CountAccumulates()
        {
            var table = new WordHashTable();
            table.Increment("слово");
            table.Increment("слово");
            table.Increment("слово");

            var entries = table.GetAllEntries();
            Assert.That(entries.Length, Is.EqualTo(1));
            Assert.That(entries[0].Count, Is.EqualTo(3));
        }

        [Test]
        public void Increment_MultipleDistinctWords_AllStoredCorrectly()
        {
            var table = new WordHashTable();
            string[] words = { "кот", "пёс", "рыба", "птица" };
            foreach (var w in words)
                table.Increment(w);

            Assert.That(table.Count, Is.EqualTo(4));

            var entries = table.GetAllEntries();
            var dict = new System.Collections.Generic.Dictionary<string, int>();
            foreach (var e in entries)
                dict[e.Word] = e.Count;

            foreach (var w in words)
            {
                Assert.That(dict.ContainsKey(w), Is.True, $"Слово '{w}' не найдено");
                Assert.That(dict[w], Is.EqualTo(1));
            }
        }

        //2. Граничный случай: пустая таблица
        [Test]
        public void GetAllEntries_EmptyTable_ReturnsEmptyArray()
        {
            var table = new WordHashTable();
            var entries = table.GetAllEntries();
            Assert.That(entries, Is.Empty);
        }

        //3. Граничный случай: одно слово
        [Test]
        public void Count_AfterOneIncrement_IsOne()
        {
            var table = new WordHashTable();
            table.Increment("x");
            Assert.That(table.Count, Is.EqualTo(1));
        }

        //4. Большой объём данных — нет зависания
        [Test]
        [Timeout(5000)]
        public void Increment_LargeNumberOfUniqueWords_CompletesInTime()
        {
            var table = new WordHashTable();
            for (int i = 0; i < 50_000; i++)
                table.Increment("word" + i);

            Assert.That(table.Count, Is.EqualTo(50_000));
        }

        //5. Специфический случай: коллизии (слова с одинаковым префиксом)
        [Test]
        public void Increment_ManyWordsWithSamePrefix_AllStoredCorrectly()
        {
            var table = new WordHashTable();
            for (int i = 0; i < 1000; i++)
                table.Increment("prefix" + i);

            Assert.That(table.Count, Is.EqualTo(1000));
        }

        //6. Повторное добавление после большого набора
        [Test]
        public void Increment_ExistingWordAfterManyInserts_CountCorrect()
        {
            var table = new WordHashTable();
            for (int i = 0; i < 500; i++)
                table.Increment("filler" + i);

            table.Increment("target");
            table.Increment("target");

            var entries = table.GetAllEntries();
            int targetCount = 0;
            foreach (var e in entries)
                if (e.Word == "target") targetCount = e.Count;

            Assert.That(targetCount, Is.EqualTo(2));
        }
    }
}
