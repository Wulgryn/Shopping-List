using PandoraIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SL.Core
{
	public record Product(string name, int last_price, params int[] prices)
	{
		public double AveragePrice => prices.Length == 0 ? 0 : prices.Where(p => p > -1).Average();
		public int PriceVariance => prices.Length == 0 ? 0 : (int)prices.Where(p => p > -1).Select(p => (p - AveragePrice) * (p - AveragePrice)).Average();
		public int PriceStdDev => (int)Math.Sqrt(PriceVariance);
	}
	public class ProductList
	{
		public static void Initialize()
		{
#if ANDROID
			string path = FileSystem.AppDataDirectory + "/data/products.cfg";
#else
			string path = "/data/products.cfg";
#endif
			ConfigManager.CreateNamed("products", path, true);
#if DEBUG
			ProductList.AddItem("Alma", 10);
			ProductList.AddItem("Körte", 20);
			ProductList.AddItem("Banán", 30);
			ProductList.AddItem("Dinnye", 40);
			ProductList.AddItem("Szilva", 50);
			for(int i = 0; i < 1000; i++)
			{
				ProductList.AddItem("Alma" + i, 10);
			}
#endif

#if RELEASE
			// Clean up debug items if they exist.
			for (int i = 0; i < 1000; i++)
			{
				ProductList.RemoveItem("Alma" + i);
			}
#endif
		}

		public static void AddItem(string name, int price = -1)
		{
			ConfigManager.Instances["products"].SetValue(name, price == -1 ? "NI" : price.ToString());
		}

		public static void RemoveItem(string name)
		{
			ConfigManager.Instances["products"].RemoveKey(name);
		}

		public static void AddItemPrice(string name, int price)
		{
			string prices = ConfigManager.Instances["products"].GetValue(name) ?? "NI";
			if (prices == "NI")
			{
				prices = price.ToString();
			}
			else
			{
				prices += $",{price}";
			}
			ConfigManager.Instances["products"].SetValue(name, prices.Trim(','));
		}

		public static void ClearItemPrices(string name)
		{
			ConfigManager.Instances["products"].SetValue(name, GetItem(name)?.prices.Last().ToString() ?? "NI");
		}

		public static Product? GetItem(string name)
		{
			string prices = ConfigManager.Instances["products"].GetValue(name) ?? "NI";
			if (prices == "NI") return null;
			int[] priceArray = prices.Split(',').Select(p => int.TryParse(p, out int val) ? val : -1).Where(p => p != -1).ToArray();
			if (priceArray.Length == 0) return null;
			return new Product(ConfigManager.Instances["products"].GetKey(name), priceArray.Last(), priceArray);
		}

		public static async IAsyncEnumerable<Product> GetItems(int max = -1)
		{
			int count = 0;
			foreach (string key in ConfigManager.Instances["products"].GetKeys())
			{
				yield return GetItem(key) ?? new Product(key, -1);

				if (max > 0 && ++count >= max)
					yield break;

				// Make iterator truly asynchronous and responsive.
				await Task.Yield();
			}
		}

		// Pseudocode:
		// - Add an async iterator overload (SearchAsync) that returns IAsyncEnumerable<string>.
		// - Accept IEnumerable<string> (and optional CancellationToken).
		// - Normalize the query to lowercase invariant.
		// - First pass: iterate the source in order, yield items whose lowercase starts with the query.
		// - Await Task.Yield() each iteration to keep it responsive.
		// - Second pass: iterate again, yield items that contain the query but don't start with it.
		// - Keep output lowercase to match existing behavior.
		// - Provide an overload that accepts IAsyncEnumerable<string> as input, buffering results to preserve grouping order and single-pass sources.
		// - Support cancellation via [EnumeratorCancellation].

		static async IAsyncEnumerable<string> SearchAsync(string szoveg, IEnumerable<string> list, [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
		{
			if (list is null) yield break;

			string needle = (szoveg ?? string.Empty).ToLowerInvariant();

			// First pass: items starting with the needle
			foreach (var s in list)
			{
				cancellationToken.ThrowIfCancellationRequested();

				string lower = (s ?? string.Empty).ToLowerInvariant();
				if (lower.StartsWith(needle))
					yield return lower;

				// Make iterator truly asynchronous and responsive.
				await Task.Yield();
			}

			// Second pass: items that contain the needle but don't start with it
			foreach (var s in list)
			{
				cancellationToken.ThrowIfCancellationRequested();

				string lower = (s ?? string.Empty).ToLowerInvariant();
				if (!lower.StartsWith(needle) && lower.Contains(needle))
					yield return lower;

				await Task.Yield();
			}

			string[] parts = needle.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length > 0)
			{
				foreach (var s in list)
				{
					cancellationToken.ThrowIfCancellationRequested();

					string lower = (s ?? string.Empty).ToLowerInvariant();
					if (parts.Any(p => lower.Contains(p)))
						yield return lower;

					await Task.Yield();
				}
			}
		}

		static async IAsyncEnumerable<string> SearchAsync(string szoveg, IAsyncEnumerable<string> list, [System.Runtime.CompilerServices.EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
		{
			if (list is null) yield break;

			string needle = (szoveg ?? string.Empty).ToLowerInvariant();

			// Buffer to preserve ordering by groups when input can't be iterated twice.
			var starts = new List<string>();
			var contains = new List<string>();

			await foreach (var s in list.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				string lower = (s ?? string.Empty).ToLowerInvariant();
				if (lower.StartsWith(needle)) starts.Add(lower);
				else if (lower.Contains(needle)) contains.Add(lower);

				// Keep responsive between items.
				await Task.Yield();
			}

			foreach (var s in starts)
				yield return s;

			foreach (var s in contains)
				yield return s;
		}

		public static async IAsyncEnumerable<Product> Search(string name)
		{
			await foreach(string s in SearchAsync(name, ConfigManager.Instances["products"].GetKeys().ToList()))
			{
				yield return GetItem(s) ?? new Product(s, -1);
				await Task.Yield();
			}
		}
	}
}
