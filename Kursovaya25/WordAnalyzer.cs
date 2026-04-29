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
}