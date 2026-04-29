using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Kursovaya25
{
    public partial class Form1 : Form
    {
        private readonly WordAnalyzer _analyzer = new WordAnalyzer();
        private AnalysisResult? _result;
        private string _sourceFile = "";

        // цвета для облака слов
        private static readonly Color[] CloudColors =
        {
            Color.FromArgb(31, 119, 180),
            Color.FromArgb(255, 127, 14),
            Color.FromArgb(44, 160, 44),
            Color.FromArgb(214, 39, 40),
            Color.FromArgb(148, 103, 189),
            Color.FromArgb(140, 86, 75),
            Color.FromArgb(227, 119, 194),
            Color.FromArgb(188, 189, 34),
            Color.FromArgb(23, 190, 207)
        };

        public Form1()
        {
            InitializeComponent();
        }

        // Открыть файл
        private void BtnOpen_Click(object? sender, EventArgs e)
        {
            using var dlg = new OpenFileDialog
            {
                Title = "Выберите текстовый файл",
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            _sourceFile = dlg.FileName;
            var info = new FileInfo(_sourceFile);
            lblFile.Text = $"{Path.GetFileName(_sourceFile)}  ({info.Length / 1024.0:F1} КБ)";
            lblStatus.Text = "Файл загружен. Нажмите «Анализировать».";
            lblStatus.ForeColor = Color.DarkGreen;
        }

        // Анализировать
        private void BtnAnalyze_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_sourceFile))
            {
                MessageBox.Show("Сначала выберите файл.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                lblStatus.Text = "Анализ...";
                lblStatus.ForeColor = Color.DarkBlue;
                Application.DoEvents();

                _analyzer.UseStopWords = chkStopWords.Checked;
                _analyzer.SortAlgorithm = cmbSort.SelectedItem?.ToString() ?? "QuickSort";

                var sw = Stopwatch.StartNew();
                string text = _analyzer.LoadFile(_sourceFile);
                _result = _analyzer.Analyze(text);
                sw.Stop();

                FillTopTable();
                FillStats(sw.ElapsedMilliseconds);
                pnlCloud.Invalidate();
                pnlHist.Invalidate();

                btnSave.Enabled = true;
                lblStatus.Text = $"Готово за {sw.ElapsedMilliseconds} мс. " +
                                 $"Слов: {_result.TotalWords}, уникальных: {_result.UniqueWords}.";
                lblStatus.ForeColor = Color.DarkGreen;
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Ошибка: " + ex.Message;
                lblStatus.ForeColor = Color.Red;
            }
        }

        // Сохранить отчёт
        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (_result == null) return;

            using var dlg = new SaveFileDialog
            {
                Title = "Сохранить отчёт",
                Filter = "Текстовый файл (*.txt)|*.txt",
                FileName = "word_analysis_report.txt"
            };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            try
            {
                _analyzer.SaveResults(dlg.FileName, _result, _sourceFile);
                MessageBox.Show("Отчёт сохранён:\n" + dlg.FileName, "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Заполнение таблицы топ-50
        private void FillTopTable()
        {
            if (_result == null) return;
            dgvTop.Rows.Clear();

            int maxCount = _result.TopWords.Length > 0 ? _result.TopWords[0].Count : 1;

            for (int i = 0; i < _result.TopWords.Length; i++)
            {
                var w = _result.TopWords[i];
                // полоска из символов ?
                int barLen = (int)(40.0 * w.Count / maxCount);
                string bar = new string('?', barLen);
                // TF в процентах с 4 знаками
                string tfStr = $"{w.TF * 100:F4}%";
                dgvTop.Rows.Add(i + 1, w.Word, w.Count, tfStr, bar);

                // цветовая заливка строки по рангу
                var row = dgvTop.Rows[i];
                if (i < 3)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 255, 200);
                else if (i < 10)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(240, 255, 240);
            }
        }

        // Статистика
        private void FillStats(long ms)
        {
            if (_result == null) return;
            txtStats.Clear();

            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("         СТАТИСТИКА АНАЛИЗА ТЕКСТА", Color.DarkBlue, bold: true);
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("");
            AppendLine($"  Файл:                  {Path.GetFileName(_sourceFile)}");
            AppendLine($"  Алгоритм сортировки:   {_analyzer.SortAlgorithm}");
            AppendLine($"  Фильтрация стоп-слов:  {(_analyzer.UseStopWords ? "Да" : "Нет")}");
            AppendLine($"  Время анализа:         {ms} мс");
            AppendLine("");
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("  ОСНОВНЫЕ ПОКАЗАТЕЛИ", Color.DarkBlue, bold: true);
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine($"  Всего слов (без стоп):  {_result.TotalWords}");
            AppendLine($"  Уникальных слов:        {_result.UniqueWords}");
            AppendLine($"  Средняя длина слова:    {_result.AverageWordLength:F2} символов");
            AppendLine($"  Самое длинное слово:    «{_result.LongestWord}» ({_result.LongestWord.Length} букв)");
            AppendLine("");
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("  ТОП-10 СЛОВ  (TF = count / totalWords)", Color.DarkBlue, bold: true);
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine($"  {"№",-4} {"Слово",-25} {"Частота",7}  {"TF",9}");
            AppendLine("  " + new string('-', 50));

            int show = Math.Min(10, _result.TopWords.Length);
            for (int i = 0; i < show; i++)
            {
                var w = _result.TopWords[i];
                AppendLine($"  {i + 1,-4} {w.Word,-25} {w.Count,7}  {w.TF,8:F6}");
            }

            AppendLine("");
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("  ЧТО ТАКОЕ TF (Term Frequency)", Color.DarkBlue, bold: true);
            AppendLine("???????????????????????????????????????????????????", Color.Gray);
            AppendLine("  TF(слово) = количество_вхождений / всего_слов");
            AppendLine("  Показывает относительную важность слова в тексте.");
            AppendLine("  Значение от 0 до 1; чем выше — тем чаще встречается.");
            AppendLine($"  Сумма TF всех уникальных слов ? 1.0");
        }

        private void AppendLine(string text, Color? color = null, bool bold = false)
        {
            txtStats.SelectionStart = txtStats.TextLength;
            txtStats.SelectionLength = 0;
            txtStats.SelectionColor = color ?? Color.Black;
            txtStats.SelectionFont = bold
                ? new Font("Consolas", 11, FontStyle.Bold)
                : new Font("Consolas", 11);
            txtStats.AppendText(text + "\n");
        }

        // Облако слов
        private void PnlCloud_Paint(object? sender, PaintEventArgs e)
        {
            if (_result == null || _result.TopWords.Length == 0) return;

            var g = e.Graphics;
            g.Clear(Color.White);
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            int maxCount = _result.TopWords[0].Count;
            int minCount = _result.TopWords[_result.TopWords.Length - 1].Count;
            if (maxCount == minCount) minCount = 0;

            var rng = new Random(42);
            int panelW = pnlCloud.ClientSize.Width;
            int panelH = pnlCloud.ClientSize.Height;

            // Список уже занятых прямоугольников для предотвращения наложений
            var placed = new List<RectangleF>();

            int show = Math.Min(50, _result.TopWords.Length);

            for (int i = 0; i < show; i++)
            {
                var entry = _result.TopWords[i];
                double ratio = maxCount == minCount ? 1.0
                    : (double)(entry.Count - minCount) / (maxCount - minCount);

                float fontSize = (float)(10 + ratio * 38); // от 10 до 48
                var font = new Font("Segoe UI", fontSize, FontStyle.Bold);
                var color = CloudColors[i % CloudColors.Length];
                var brush = new SolidBrush(color);

                SizeF sz = g.MeasureString(entry.Word, font);

                // Пытаемся разместить слово без наложений (до 200 попыток)
                bool ok = false;
                RectangleF rect = RectangleF.Empty;
                for (int attempt = 0; attempt < 200; attempt++)
                {
                    float x = rng.Next(4, Math.Max(5, panelW - (int)sz.Width - 4));
                    float y = rng.Next(4, Math.Max(5, panelH - (int)sz.Height - 4));
                    rect = new RectangleF(x, y, sz.Width, sz.Height);

                    bool overlap = false;
                    foreach (var p in placed)
                    {
                        if (p.IntersectsWith(rect)) { overlap = true; break; }
                    }
                    if (!overlap) { ok = true; break; }
                }

                if (ok)
                {
                    placed.Add(rect);
                    g.DrawString(entry.Word, font, brush, rect.Location);
                }

                font.Dispose();
                brush.Dispose();
            }
        }

        // Смена вкладки
        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            pnlCloud.Invalidate();
            pnlHist.Invalidate();
        }

        // Гистограмма длин слов
        private void PnlHist_Paint(object? sender, PaintEventArgs e)
        {
            if (_result == null) return;

            var g = e.Graphics;
            g.Clear(Color.White);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var dist = _result.LengthDistribution;

            // Найдём диапазон ненулевых длин
            int minLen = 1, maxLen = dist.Length - 1;
            while (minLen < dist.Length && dist[minLen] == 0) minLen++;
            while (maxLen > 0 && dist[maxLen] == 0) maxLen--;
            if (minLen > maxLen) return;

            int bars = maxLen - minLen + 1;
            int maxVal = 0;
            for (int i = minLen; i <= maxLen; i++)
                if (dist[i] > maxVal) maxVal = dist[i];
            if (maxVal == 0) return;

            int W = pnlHist.ClientSize.Width;
            int H = pnlHist.ClientSize.Height;
            int marginL = 70, marginR = 20, marginT = 30, marginB = 50;
            int chartW = W - marginL - marginR;
            int chartH = H - marginT - marginB;

            var axisPen = new Pen(Color.Black, 1.5f);
            var gridPen = new Pen(Color.LightGray, 1f);
            var barBrush = new SolidBrush(Color.FromArgb(70, 130, 180));
            var labelFont = new Font("Segoe UI", 8);
            var titleFont = new Font("Segoe UI", 10, FontStyle.Bold);

            // Заголовок
            g.DrawString("Распределение длин слов", titleFont,
                Brushes.Black, marginL, 6);

            // Оси
            g.DrawLine(axisPen, marginL, marginT, marginL, marginT + chartH);
            g.DrawLine(axisPen, marginL, marginT + chartH, marginL + chartW, marginT + chartH);

            // Горизонтальные линии сетки (5 делений)
            int gridLines = 5;
            for (int gi = 0; gi <= gridLines; gi++)
            {
                int yy = marginT + chartH - (int)((double)gi / gridLines * chartH);
                g.DrawLine(gridPen, marginL, yy, marginL + chartW, yy);
                int val = (int)((double)gi / gridLines * maxVal);
                g.DrawString(val.ToString(), labelFont, Brushes.Gray,
                    2, yy - 7);
            }

            // Столбцы
            float barW = (float)chartW / bars;
            for (int i = 0; i < bars; i++)
            {
                int len = minLen + i;
                int cnt = dist[len];
                int barH = (int)((double)cnt / maxVal * chartH);
                float x = marginL + i * barW;
                float y = marginT + chartH - barH;

                // Градиент по высоте
                double ratio = (double)i / Math.Max(1, bars - 1);
                var c = Color.FromArgb(
                    (int)(70 + ratio * 100),
                    (int)(130 - ratio * 30),
                    (int)(180 - ratio * 80));
                using var br = new SolidBrush(c);
                g.FillRectangle(br, x + 1, y, barW - 2, barH);
                g.DrawRectangle(Pens.SteelBlue, x + 1, y, barW - 2, barH);

                // Подпись длины
                var lbl = len.ToString();
                var lblSz = g.MeasureString(lbl, labelFont);
                g.DrawString(lbl, labelFont, Brushes.Black,
                    x + barW / 2 - lblSz.Width / 2,
                    marginT + chartH + 4);

                // Значение над столбцом (только если достаточно места)
                if (barH > 14)
                {
                    var valLbl = cnt.ToString();
                    var valSz = g.MeasureString(valLbl, labelFont);
                    g.DrawString(valLbl, labelFont, Brushes.DarkSlateGray,
                        x + barW / 2 - valSz.Width / 2, y - 13);
                }
            }

            // Подписи осей
            g.DrawString("Длина слова (символов)", labelFont, Brushes.Black,
                marginL + chartW / 2 - 60, H - 18);

            // Вертикальная подпись оси Y
            g.TranslateTransform(12, marginT + chartH / 2);
            g.RotateTransform(-90);
            g.DrawString("Количество слов", labelFont, Brushes.Black, -50, 0);
            g.ResetTransform();

            axisPen.Dispose();
            gridPen.Dispose();
            barBrush.Dispose();
            labelFont.Dispose();
            titleFont.Dispose();
        }
    }
}
