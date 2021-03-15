using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using Tesseract;
using ImageFormat = System.Drawing.Imaging.ImageFormat;

namespace DzhuMenuWebApp
{
	internal static class MenuParser
	{
		public static List<(string, int)> Parse(Bitmap bitmap)
		{
			var lines = GetLines(bitmap);
			var entryBorders = GetBorders(lines, bitmap);
			var entries = GetEntries(entryBorders, bitmap);

			return entries;
		}

		/// <summary>
		/// Возвращает список y-координат, на которых найдены точки.
		/// </summary>
		/// <param name="bitmap">Картинка с меню.</param>
		/// <returns>Список значений высоты точек-разделителей в меню в пикселях.</returns>
		private static List<int> GetLines(Bitmap bitmap)
		{
			var dots = new List<uint>();
			for (var y = bitmap.Height - 1; y >= 0; y--)
			{
				uint sum = 0;
				for (var x = 0; x < bitmap.Width; x++)
				{
					sum += (uint) (bitmap.GetPixel(x, y).GetBrightness() * 255);
				}

				dots.Add(sum);
			}

			dots.Reverse();

			var lines = new List<int>();

			var absDiffValues = dots.Select(x => x - dots.Min()).ToArray();
			var lastNonEmpty = dots.Count;
			for (var y = absDiffValues.Length - 1; y >= 0; y--)
			{
				if (absDiffValues[y] > 16000 || lastNonEmpty - y < 10)
				{
					continue;
				}

				lastNonEmpty = y;
				lines.Add(y);
			}

#if DEBUG
			var st1Sample = (Bitmap) bitmap.Clone();
			lines.ForEach(y => st1Sample.SetPixel(5, y, Color.Brown));
			st1Sample.Save("step1_linemap.jpg");
			st1Sample.Dispose();
#endif

			lines.Reverse();

			return lines;
		}

		/// <summary>
		/// Возвращает границы областей с названием и ценой.
		/// </summary>
		/// <param name="lines">Список значений высоты точек-разделителей в меню в пикселях.</param>
		/// <param name="bitmap">Картинка с меню.</param>
		/// <returns>Список пар прямоугольников, содержавщих название и цену соответственно.</returns>
		private static List<(Rect name, Rect cost)> GetBorders(List<int> lines, Bitmap bitmap)
		{
			var entryBorders = lines
				.Select(lineMidpoint =>
				{
					var name = GetNameRect(bitmap, lineMidpoint);
					var cost = GetCostRect(bitmap, lineMidpoint);
					return (name, cost);
				})
				.ToList();

#if DEBUG
			var st2Sample = (Bitmap) bitmap.Clone();
			foreach (var (name, cost) in entryBorders)
			{
				st2Sample.SetPixel(name.X1, name.Y1, Color.Brown);
				st2Sample.SetPixel(name.X2, name.Y2, Color.Brown);
				st2Sample.SetPixel(cost.X1, cost.Y1, Color.Brown);
				st2Sample.SetPixel(cost.X2, cost.Y2, Color.Brown);
			}

			st2Sample.Save("3.jpg");
#endif

			return entryBorders;
		}

		/// <summary>
		/// Возвращает название и цену блюд.
		/// </summary>
		/// <param name="entryBorders">Список пар прямоугольников, содержавщих название и цену соответственно.</param>
		/// <param name="bitmap">Картинка с меню.</param>
		/// <returns>Список пар названий и цен.</returns>
		private static List<(string, int)> GetEntries(List<(Rect name, Rect cost)> entryBorders, Image bitmap)
		{
			var entries = new List<(string, int)>();
			var pix = Pix.LoadFromMemory(bitmap.ToByteArray(ImageFormat.Png));
			var r = new TesseractEngine("tessdata", "rus");
			foreach (var (nameRect, costRect) in entryBorders)
			{
				var nameProcessor = r.Process(pix, nameRect);
				var name = Regex.Replace(
					nameProcessor.GetText().Trim(' ', '\n', '.', ',', '\''),
					@"[^а-яА-Я(,\s/]",
					string.Empty);
				nameProcessor.Dispose();

				var costProcessor = r.Process(pix, costRect, PageSegMode.SingleWord);
				var costRaw = costProcessor.GetText();
				costProcessor.Dispose();

				var cost = Regex.Replace(
					costRaw
						.Trim(' ', '\n', '.', ',')
						.Split('/').Last()
						.Split('р').First()
						.Replace('б', '6')
						.Replace('о', '0')
						.Replace('з', '3'),
					@"[^\d]",
					string.Empty);

				var pair = (name, int.Parse(cost));
				entries.Add(pair);
			}

			return entries;
		}

		/// <summary>
		/// Возвращает границы участка, содержащего начало имени (не блиннее половины страницы).
		/// </summary>
		/// <param name="bitmap">Картинка с меню.</param>
		/// <param name="lineMidpoint">y-координата строки пикселей, содержащая точки-разделители для данного элемента меню.</param>
		/// <returns>Границы участка, содержащего начало имени.</returns>
		private static Rect GetNameRect(Bitmap bitmap, int lineMidpoint)
		{
			int left = 0, right = 0;
			for (var x = 0; x < bitmap.Width / 2; x++)
			{
				if (bitmap.IsWhite(x, lineMidpoint - 3)) continue;

				left = x - 5;
				break;
			}


			for (var x = bitmap.Width / 3; x >= left + 10; x--)
			{
				if (bitmap.IsWhite(x, lineMidpoint - 3)) continue;

				right = x + 5;
				break;
			}

			var top = lineMidpoint - 10;
			var width = right - left;
			Rect nameRect = new Rect(left, top, width, 16);

			return nameRect;
		}

		/// <summary>
		/// Возвращает границы участка, содержащего вес и цену.
		/// </summary>
		/// <param name="bitmap">Картинка с меню.</param>
		/// <param name="lineMidpoint">y-координата строки пикселей, содержащая точки-разделители для данного элемента меню.</param>
		/// <returns>Границы участка, содержащего вес и цену.</returns>
		private static Rect GetCostRect(Bitmap bitmap, int lineMidpoint)
		{
			int left, right;
			for (right = bitmap.Width - 1; right >= bitmap.Width / 5; right--)
			{
				if (bitmap.IsWhite(right, lineMidpoint - 3)) continue;

				right += 5;
				break;
			}

			for (left = right - 5; left >= bitmap.Width / 5; left--)
			{
				var isSpace = true;
				for (var offset = 0; offset < 7; offset++)
				{
					var isWhite1 = bitmap.IsWhite(left - offset, lineMidpoint - 2);
					var isWhite2 = bitmap.IsWhite(left - offset, lineMidpoint - 3);
					var isWhite3 = bitmap.IsWhite(left - offset, lineMidpoint - 4);

					var isVertWhite = isWhite1 && isWhite2 && isWhite3;

					isSpace &= isVertWhite;
				}

				if (!isSpace)
				{
					continue;
				}

				left -= 5;
				break;
			}

			var top = lineMidpoint - 10;
			var width = right - left;
			Rect costRect = new Rect(left, top, width, 16);

			return costRect;
		}
	}
}