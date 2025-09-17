using CommunityToolkit.Maui.Views;
using SL.Core;
using SL.UI.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace SL.UI.Pages;

public partial class SearchPage : ContentPage
{
	ObservableRangeCollection<Product> items = new ObservableRangeCollection<Product>();
	MainPage mp;
	string currentSearchText = "";

	private async void RefreshItems()
	{
		if (!itemSearchResults.IsLoaded) return;
		await Dispatcher.DispatchAsync(async () =>
		{
			string text = currentSearchText;
			if (ItemSearchBar.Text == "")
			{
				var products = new List<Product>();
				List<Product> batch = new();
				int count = 0;
				await foreach (var product in ProductList.GetItems())
				{
					if(text != currentSearchText)
					{
						await Dispatcher.DispatchAsync(items.Clear);
						return;
					}
					products.Add(product);
					if (count++ % 10 == 0)
					{
						Dispatcher.Dispatch(() => items.AddRange(batch));
						batch.Clear();
					}
					else batch.Add(product);
				}
				items.ResetWith(products);
			}
			else
			{

				var products = new List<Product>();
				List<Product> batch = new();
				int count = 0;
				await foreach (var product in ProductList.Search(ItemSearchBar.Text ?? ""))
				{
					if (text != currentSearchText)
					{
						await Dispatcher.DispatchAsync(items.Clear);
						return;
					}
					products.Add(product);
					if (count++ % 10 == 0)
					{
						Dispatcher.Dispatch(() => items.AddRange(batch));
						batch.Clear();
					}
					else batch.Add(product);
				}
				items.ResetWith(products);
			}
		});
	}
	public SearchPage()
	{
		InitializeComponent();
		Loaded += async (s, e) =>
		{
			await Dispatcher.DispatchAsync(async () =>
			{
				string text = currentSearchText;
				var products = new List<Product>();
				List<Product> batch = new();
				int count = 0;
				await Dispatcher.DispatchAsync(items.Clear);
				await foreach (var product in ProductList.GetItems())
				{
					if (text != currentSearchText)
					{
						await Dispatcher.DispatchAsync(items.Clear);
						return;
					}
					products.Add(product);
					if (count++ % 10 == 0)
					{
						await Dispatcher.DispatchAsync(() =>
						{
							string text = currentSearchText;
							if (text != currentSearchText) return;
							items.AddRange(batch);
						});
						batch.Clear();
					}
					else batch.Add(product);
				}
				items.ResetWith(products);          // 1 reset, gyors
			});
		};
		ItemSearchBar.Loaded += (s, e) =>
		{
			//items.Clear();
			//if (ItemSearchBar.Text == "") ProductList.GetItems().ForEach(items.Add);
			//else ProductList.Search(ItemSearchBar.Text ?? "").ForEach(items.Add);
			RefreshItems();
		};
		NavigatedTo += async (s, e) =>
		{
			await Dispatcher.DispatchAsync(async () =>
			{
				string text = currentSearchText;
				var products = new List<Product>();
				List<Product> batch = new();
				int count = 0;
				await Dispatcher.DispatchAsync(items.Clear);
				await foreach (var product in ProductList.GetItems())
				{
					if (text != currentSearchText)
					{
						await Dispatcher.DispatchAsync(items.Clear);
						return;
					}
					products.Add(product);
					if (count++ % 10 == 0)
					{
						await Dispatcher.DispatchAsync(() =>
						{
							if (text != currentSearchText) return;
							items.AddRange(batch);
						});
						batch.Clear();
					}
					else batch.Add(product);
				}
				items.ResetWith(products);
			});
		};
		itemSearchResults.ItemsSource = items; // egyszer elég beállítani
	}

	public SearchPage(MainPage mp) : this()
	{
		this.mp = mp;
	}

	private async void ItemSearchBar_TextChanged(object sender, TextChangedEventArgs e)
	{
		await Dispatcher.DispatchAsync(async () =>
		{
			currentSearchText = e.NewTextValue;

			var products = new List<Product>();
			List<Product> batch = new();
			int count = 0;
			await Dispatcher.DispatchAsync(items.Clear);
			await foreach (var product in ProductList.Search(e.NewTextValue))
			{
				if (e.NewTextValue != currentSearchText)
				{
					await Dispatcher.DispatchAsync(items.Clear);
					return;
				}
				products.Add(product);
				if (count++ % 10 == 0)
				{
					await Dispatcher.DispatchAsync(() => items.AddRange(batch));
					batch.Clear();
				}
				else batch.Add(product);
			}
			items.ResetWith(products);
		});
	}

	private void Grid_Focused(object sender, FocusEventArgs e)
	{
		Debug.WriteLine("Focused");
	}

	private async void CreateNewItemBtn_Clicked(object sender, EventArgs e)
	{
		//var value = await this.ShowPopupAsync(new ModifyItemPopup("kezdeti érték")) as string;
		//if (value is not null)
		//{
		//	// OK: felhasználói szöveg a 'value'-ban
		//}
		await Application.Current.MainPage.Navigation.PushAsync(new AddItem(), true);
	}

	private async void modify_item_Clicked(object sender, EventArgs e)
	{
		Product product = (sender as MenuItem).BindingContext as Product;
		string new_name = await DisplayPromptAsync("Név módosítás", $"Eredeti név: \"{product.name}\"", "Mentés", "Mégsem", initialValue: product.name);
		if (string.IsNullOrEmpty(new_name)) return;
		if (ProductList.GetItem(new_name ?? "") is not null)
		{
			await DisplayAlert("Error", "An item with that name already exists.", "OK");
			return;
		}

		Product p = ProductList.GetItem(product.name);
		ProductList.RemoveItem(p.name);
		ProductList.AddItem(new_name, p.prices.FirstOrDefault(-1));
		p.prices.Skip(1).ToList().ForEach(price => ProductList.AddItemPrice(new_name, price));

		RefreshItems();
	}

	private async void modify_item_price_Clicked(object sender, EventArgs e)
	{
		Product product = (sender as MenuItem).BindingContext as Product;
		string new_price = await DisplayPromptAsync("Ár megadás", $"Eredeti ár: {product.last_price} Ft / db-kg", "Mentés", "Mégsem", initialValue: product.last_price.ToString(), keyboard: Keyboard.Numeric);
		if (string.IsNullOrEmpty(new_price)) return;

		if (!int.TryParse(new_price, out int new_price_i))
		{
			await DisplayAlert("Error", "Only integers accepted", "OK");
			return;
		}

		ProductList.AddItemPrice(product.name, new_price_i);

		RefreshItems();
	}

	private async void delete_item_Clicked(object sender, EventArgs e)
	{
		var ctx = (sender as MenuItem)?.BindingContext
			  ?? (sender as SwipeItem)?.BindingContext;

		var item = ctx as ShoppingListItem;
		if (item is null && ctx is Product p)
		{
			// SearchPage: termék törlése a saját listából
			if (await DisplayAlert("Termék törlése", $"Biztosan törlöd: {p.name}?", "Igen", "Nem"))
			{
				ProductList.RemoveItem(p.name);
				RefreshItems();
			}
			return;
		}
		if (item is not null)
		{
			ShoppingList.RemoveItem(item.product.name);
			RefreshItems();
		}
	}

	private async void OnItemTapped(object sender, EventArgs e)
	{
		if ((sender as VisualElement)?.BindingContext is Product product)
		{
			if (ShoppingList.GetItem(product.name) is null)
				ShoppingList.AddItem(product.name);
			mp.RefreshItems();
			await Navigation.PopAsync(true);
		}
	}
}