using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace DzhuMenuWebApp.Pages
{
	public class IndexModel : PageModel
	{
		// Путь к картинке. TODO: вынести в конфиг, однажды.
		private const string imagePath = @"C:\DzhuMenu\menu.png";
		
		private readonly ILogger<IndexModel> _logger;
		private readonly object locker = new object();

		public IndexModel(ILogger<IndexModel> logger, IMemoryCache memoryCache)
		{
			MemoryCache = memoryCache;
			_logger = logger;
		}

		private IMemoryCache MemoryCache { get; }

		public byte[] MenuImage { get; private set; }

		public List<(string, int)> MenuContent { get; private set; }

		public void OnGet()
		{
			MenuImage = GetTodayMenuImage();
			MenuContent = MenuImage != null ? TodayMenu(MenuImage) : null;
		}

		private List<(string, int)> TodayMenu(byte[] imageBytes)
		{
			var menuImageHash = imageBytes.Length.ToString();

			lock (locker)
			{
				if (MemoryCache.Get(menuImageHash) is List<(string, int)> menuContent)
				{
					return menuContent;
				}

				using Stream stream = new MemoryStream(imageBytes);
				using var img = new Bitmap(Image.FromStream(stream));
				menuContent = MenuParser.Parse(img);
				MemoryCache.Set(menuImageHash, menuContent, TimeSpan.FromHours(6));

				return menuContent;
			}
		}

		private static byte[] GetTodayMenuImage()
		{
			return DateTime.Now - System.IO.File.GetLastWriteTime(imagePath) > TimeSpan.FromHours(18)
				? null
				: System.IO.File.ReadAllBytes(imagePath);
		}
	}
}