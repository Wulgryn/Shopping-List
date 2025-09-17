using PandoraIO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SL.Core
{
	public record ShoppingListItem(Product product)
	{
		public int Quantity { get; set; } = 1;
		public bool IsPurchased { get; set; } = false;
		public int TotalPrice => product.last_price * Quantity;
	}


	public class ShoppingList
	{
		public static void Initialize()
		{
#if ANDROID
			string path = FileSystem.AppDataDirectory + "/data/shopItems.cfg";
#else
			string path = "/data/shopItems.cfg";
#endif
			ConfigManager.CreateNamed("shopItems", path, true);
		}

		public static void AddItem(string productName, int quantity = 1, bool isPurchased = false)
		{
			ConfigManager.Instances["shopItems"].SetValue(productName, $"{quantity}|{(isPurchased ? 1 : 0)}");
		}

		public static void RemoveItem(string productName)
		{
			ConfigManager.Instances["shopItems"].RemoveKey(productName);
		}

		public static ShoppingListItem? GetItem(string productName)
		{
			var cfg = ConfigManager.Instances["shopItems"];
			var value = cfg.GetValue(productName);
			if (value is null) return null;
			var parts = value.Split('|');
			if (parts.Length != 2) return null;
			if (!int.TryParse(parts[0], out int quantity)) quantity = 1;
			if (!int.TryParse(parts[1], out int isPurchasedInt)) isPurchasedInt = 0;
			bool isPurchased = isPurchasedInt == 1;
			var product = ProductList.GetItem(productName);
			if (product is not null)
			{
				var item = new ShoppingListItem(product)
				{
					Quantity = quantity,
					IsPurchased = isPurchased
				};
				return item;
			}
			return null;
		}

		public static List<ShoppingListItem> GetAllItems()
		{
			var items = new List<ShoppingListItem>();
			var cfg = ConfigManager.Instances["shopItems"];
			foreach (var key in cfg.GetKeys())
			{
				var value = cfg.GetValue(key);
				if (value is null) continue;
				var parts = value.Split('|');
				if (parts.Length != 2) continue;
				if (!int.TryParse(parts[0], out int quantity)) quantity = 1;
				if (!int.TryParse(parts[1], out int isPurchasedInt)) isPurchasedInt = 0;
				bool isPurchased = isPurchasedInt == 1;
				var product = ProductList.GetItem(key);
				if (product is not null)
				{
					var item = new ShoppingListItem(product)
					{
						Quantity = quantity,
						IsPurchased = isPurchased
					};
					items.Add(item);
				}
			}


			return items.OrderBy(i => i.IsPurchased).ThenBy(i => i.product.name).ToList();
		}

		public static void UpdateItem(string productName, int quantity, bool isPurchased)
		{
			if (GetAllItems().Any(i => i.product.name == productName))
			{
				AddItem(productName, quantity, isPurchased);
			}
		}


	}
}
