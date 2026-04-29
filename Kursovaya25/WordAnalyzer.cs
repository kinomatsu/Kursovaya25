using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Kursovaya25
{
    /// <summary>
    /// Результат анализа текста
    /// </summary>
    public class AnalysisResult
    {
        public WordEntry[] TopWords { get; set; } = Array.Empty<WordEntry>();
        public int TotalWords { get; set; }
        public int UniqueWords { get; set; }
        public double AverageWordLength { get; set; }
        public string LongestWord { get; set; } = "";
        public int[] LengthDistribution { get; set; } = Array.Empty<int>();
    }

    /// <summary>
    /// Пара слово-частота + TF
    /// </summary>
    public class WordEntry
    {
        public string Word { get; set; } = "";
        public int Count { get; set; }

        /// <summary>
        /// Term Frequency: TF(t) = count(t) / totalWords
        /// Показывает долю данного слова среди всех слов документа.
        /// </summary>
        public double TF { get; set; }
    }

    /// <summary>
    /// Хеш-таблица для подсчёта частот слов.
    /// Открытая адресация с линейным пробированием.
    /// </summary>
    public class WordHashTable
    {
        private const int DEFAULT_CAPACITY = 131071;
        private const double LOAD_FACTOR = 0.7;

        private string?[] _keys;
        private int[] _values;
        private int _capacity;
        private int _count;

        public int Count => _count;

        public WordHashTable(int capacity = DEFAULT_CAPACITY)
        {
            _capacity = capacity;
            _keys = new string?[_capacity];
            _values = new int[_capacity];
        }

        /// <summary>
        /// Полиномиальный хеш строки 
        /// </summary>
        private int Hash(string key)
        {
            unchecked
            {
                int hash = 5381;
                foreach (char c in key)
                    hash = hash * 33 ^ c;
                return Math.Abs(hash) % _capacity;
            }
        }

        /// <summary>
        /// Увеличить счётчик слова на 1 
        /// </summary>
        public void Increment(string key)
        {
            if ((double)_count / _capacity >= LOAD_FACTOR)
                Resize();

            int idx = Hash(key);
            int start = idx;

            while (_keys[idx] != null)
            {
                if (_keys[idx] == key)
                {
                    _values[idx]++;
                    return;
                }
                idx = (idx + 1) % _capacity;
                if (idx == start) break;
            }

            _keys[idx] = key;
            _values[idx] = 1;
            _count++;
        }

        /// <summary>
        /// Получить все пары ключ-значение
        /// </summary>
        public WordEntry[] GetAllEntries()
        {
            var result = new WordEntry[_count];
            int ri = 0;
            for (int i = 0; i < _capacity; i++)
            {
                if (_keys[i] != null)
                    result[ri++] = new WordEntry { Word = _keys[i]!, Count = _values[i] };
            }
            return result;
        }

        private void Resize()
        {
            int newCapacity = NextPrime(_capacity * 2);
            var oldKeys = _keys;
            var oldValues = _values;

            _capacity = newCapacity;
            _keys = new string?[_capacity];
            _values = new int[_capacity];
            _count = 0;

            for (int i = 0; i < oldKeys.Length; i++)
            {
                if (oldKeys[i] != null)
                {
                    // вставляем напрямую без Increment чтобы не потерять значение
                    int idx = Hash(oldKeys[i]!);
                    while (_keys[idx] != null)
                        idx = (idx + 1) % _capacity;
                    _keys[idx] = oldKeys[i];
                    _values[idx] = oldValues[i];
                    _count++;
                }
            }
        }

        private static int NextPrime(int n)
        {
            if (n < 2) return 2;
            if (n % 2 == 0) n++;
            while (!IsPrime(n)) n += 2;
            return n;
        }

        private static bool IsPrime(int n)
        {
            if (n < 2) return false;
            if (n == 2) return true;
            if (n % 2 == 0) return false;
            for (int i = 3; (long)i * i <= n; i += 2)
                if (n % i == 0) return false;
            return true;
        }
    }
    /// <summary>
    /// Алгоритмы сортировки
    /// </summary>
    public static class SortAlgorithms
    {
        /// <summary>
        /// Быстрая сортировка по убыванию Count
        /// </summary>
        public static void QuickSort(WordEntry[] arr, int left, int right)
        {
            if (left >= right) return;
            int pivot = Partition(arr, left, right);
            QuickSort(arr, left, pivot - 1);
            QuickSort(arr, pivot + 1, right);
        }

        private static int Partition(WordEntry[] arr, int left, int right)
        {
            int pivotVal = arr[right].Count;
            int i = left - 1;
            for (int j = left; j < right; j++)
            {
                if (arr[j].Count >= pivotVal) // убывание
                {
                    i++;
                    (arr[i], arr[j]) = (arr[j], arr[i]);
                }
            }
            (arr[i + 1], arr[right]) = (arr[right], arr[i + 1]);
            return i + 1;
        }

        /// <summary>
        /// Сортировка слиянием по убыванию Count (возвращает новый массив)
        /// </summary>
        public static WordEntry[] MergeSort(WordEntry[] arr)
        {
            if (arr.Length <= 1) return arr;
            int mid = arr.Length / 2;

            var left = new WordEntry[mid];
            var right = new WordEntry[arr.Length - mid];
            Array.Copy(arr, 0, left, 0, mid);
            Array.Copy(arr, mid, right, 0, right.Length);

            left = MergeSort(left);
            right = MergeSort(right);
            return Merge(left, right);
        }

        private static WordEntry[] Merge(WordEntry[] left, WordEntry[] right)
        {
            var result = new WordEntry[left.Length + right.Length];
            int i = 0, j = 0, k = 0;
            while (i < left.Length && j < right.Length)
            {
                if (left[i].Count >= right[j].Count)
                    result[k++] = left[i++];
                else
                    result[k++] = right[j++];
            }
            while (i < left.Length) result[k++] = left[i++];
            while (j < right.Length) result[k++] = right[j++];
            return result;
        }
    }
    /// <summary>
    /// Основной анализатор текста
    /// </summary>
    public class WordAnalyzer
    {
        // Стоп-слова (русские и английские)
        private static readonly HashSet<string> StopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // русские предлоги, союзы, местоимения
            "и","в","не","на","я","что","тот","быть","с","а","весь","это","как","она",
            "по","но","они","к","у","же","вы","за","бы","по","из","он","мы","при",
            "о","от","так","его","если","уже","или","ни","был","то","ещё","бы","для",
            "нет","до","вот","ну","ли","да","со","её","их","там","где","есть","раз",
            "тут","под","над","без","про","через","между","перед","после","около",
            "себя","себе","свой","своя","своё","свои","этот","эта","эти","эту",
            "который","которая","которое","которые","такой","такая","такое","такие",
            "один","одна","одно","одни","все","всё","всем","всех","всей","всему",
            "мне","меня","тебя","тебе","него","ней","нему","нас","вас","им","ими",
            "чем","чего","чему","чём","кто","кого","кому","кем","когда","куда",
            "откуда","почему","зачем","потому","поэтому","хотя","чтобы","будто",
            "словно","пока","лишь","только","даже","именно","просто","очень","уж",
            "ведь","вдруг","вообще","здесь","сейчас","теперь","тогда","иногда",
            "всегда","никогда","нигде","никуда","нет","нельзя","надо","можно",
            // английские
            "the","a","an","and","or","but","in","on","at","to","for","of","with",
            "is","are","was","were","be","been","being","have","has","had","do",
            "does","did","will","would","could","should","may","might","shall",
            "it","its","this","that","these","those","i","you","he","she","we","they",
            "me","him","her","us","them","my","your","his","our","their","what",
            "which","who","whom","when","where","why","how","not","no","so","if",
            "as","by","from","up","about","into","through","during","before","after"
        };

        private bool _useStopWords = true;
        private string _sortAlgorithm = "QuickSort";

        public bool UseStopWords
        {
            get => _useStopWords;
            set => _useStopWords = value;
        }

        public string SortAlgorithm
        {
            get => _sortAlgorithm;
            set => _sortAlgorithm = value;
        }

        /// <summary>
        /// Токенизация: разбивает текст на слова, удаляет пунктуацию, приводит к нижнему регистру
        /// </summary>
        public List<string> Tokenize(string text)
        {
            var words = new List<string>();
            var sb = new StringBuilder();

            foreach (char c in text)
            {
                if (char.IsLetter(c) || c == '-' || c == '\'')
                {
                    sb.Append(char.ToLowerInvariant(c));
                }
                else
                {
                    if (sb.Length > 0)
                    {
                        // убираем дефисы/апострофы по краям
                        string word = sb.ToString().Trim('-', '\'');
                        if (word.Length > 0)
                            words.Add(word);
                        sb.Clear();
                    }
                }
            }
            if (sb.Length > 0)
            {
                string word = sb.ToString().Trim('-', '\'');
                if (word.Length > 0)
                    words.Add(word);
            }

            return words;
        }
        /// <summary>
        /// Анализ текста: возвращает результат с топ-50 словами и статистикой
        /// </summary>
        public AnalysisResult Analyze(string text)
        {
            var tokens = Tokenize(text);

            var table = new WordHashTable();
            int totalLength = 0;
            string longestWord = "";

            foreach (var word in tokens)
            {
                if (_useStopWords && StopWords.Contains(word))
                    continue;
                if (word.Length < 2) // пропускаем однобуквенные
                    continue;

                table.Increment(word);
                totalLength += word.Length;

                if (word.Length > longestWord.Length)
                    longestWord = word;
            }

            var entries = table.GetAllEntries();

            // Сортировка выбранным алгоритмом
            if (_sortAlgorithm == "MergeSort")
                entries = SortAlgorithms.MergeSort(entries);
            else
                SortAlgorithms.QuickSort(entries, 0, entries.Length - 1);

            // Топ-50
            int topCount = Math.Min(50, entries.Length);
            var top = new WordEntry[topCount];
            Array.Copy(entries, 0, top, 0, topCount);

            // Распределение длин слов (макс длина 30)
            int maxLen = 30;
            var dist = new int[maxLen + 1];
            foreach (var e in entries)
            {
                int len = Math.Min(e.Word.Length, maxLen);
                dist[len] += e.Count;
            }

            int totalWords = 0;
            foreach (var e in entries) totalWords += e.Count;

            // Вычисляем TF для каждого слова: TF(t) = count(t) / totalWords
            // Сортировка уже выполнена, поэтому проходим по всем entries
            if (totalWords > 0)
            {
                foreach (var e in entries)
                    e.TF = (double)e.Count / totalWords;
            }

            return new AnalysisResult
            {
                TopWords = top,
                TotalWords = totalWords,
                UniqueWords = entries.Length,
                AverageWordLength = totalWords > 0 ? (double)totalLength / totalWords : 0,
                LongestWord = longestWord,
                LengthDistribution = dist
            };
        }

        /// <summary>
        /// Загрузка текста из файла (до 10 МБ)
        /// </summary>
        public string LoadFile(string path)
        {
            var info = new FileInfo(path);
            if (info.Length > 10 * 1024 * 1024)
                throw new IOException("Файл превышает 10 МБ.");
            return File.ReadAllText(path, Encoding.UTF8);
        }

        /// <summary>
        /// Сохранение результатов анализа в текстовый файл
        /// </summary>
        public void SaveResults(string path, AnalysisResult result, string sourceFile)
        {
            using var sw = new StreamWriter(path, false, Encoding.UTF8);
            sw.WriteLine("=== АНАЛИЗ ЧАСТОТНОСТИ СЛОВ ===");
            sw.WriteLine($"Источник: {sourceFile}");
            sw.WriteLine($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sw.WriteLine();
            sw.WriteLine("--- СТАТИСТИКА ---");
            sw.WriteLine($"Всего слов (без стоп-слов): {result.TotalWords}");
            sw.WriteLine($"Уникальных слов:            {result.UniqueWords}");
            sw.WriteLine($"Средняя длина слова:        {result.AverageWordLength:F2}");
            sw.WriteLine($"Самое длинное слово:        {result.LongestWord}");
            sw.WriteLine();
            sw.WriteLine("--- ТОП-50 СЛОВ ---");
            sw.WriteLine($"{"№",-4} {"Слово",-30} {"Частота",8} {"TF",10}");
            sw.WriteLine(new string('-', 56));
            for (int i = 0; i < result.TopWords.Length; i++)
            {
                var w = result.TopWords[i];
                sw.WriteLine($"{i + 1,-4} {w.Word,-30} {w.Count,8} {w.TF,10:F6}");
            }
            sw.WriteLine();
            sw.WriteLine("--- РАСПРЕДЕЛЕНИЕ ДЛИН СЛОВ ---");
            sw.WriteLine($"{"Длина",-8} {"Количество",10}");
            for (int i = 1; i < result.LengthDistribution.Length; i++)
                if (result.LengthDistribution[i] > 0)
                    sw.WriteLine($"{i,-8} {result.LengthDistribution[i],10}");
        }
    }
}