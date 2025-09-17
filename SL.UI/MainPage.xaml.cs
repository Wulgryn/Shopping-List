using CommunityToolkit.Maui.Core.Extensions;
using PandoraIO;
using SL.Core;
using SL.UI.Pages;
using System.Collections.ObjectModel;

namespace SL.UI
{
	//SemanticScreenReader.Announce(CounterBtn.Text);
	public partial class MainPage : ContentPage
	{
		public ObservableRangeCollection<ShoppingListItem> Items { get; } = new();

		public async void RefreshItems(ShoppingListItem item = null)
		{
			if (!itemList.IsLoaded) return;
			if (item is not null) ShoppingList.AddItem(item.product.name);
			await Dispatcher.DispatchAsync(() =>
			{
				_Checkboxevent = false;

				Items.ResetWith(ShoppingList.GetAllItems());
				_Checkboxevent = true;
			});

			sum.Text = $"Összesen:\n{Items.Where(item_ => !item_.IsPurchased).Sum(_item => _item.TotalPrice).ToString()} Ft";
			//itemList.ItemsSource = Items;
		}

		public MainPage()
		{
			InitializeComponent();
			ProductList.Initialize();
			ShoppingList.Initialize();
			Loaded += (s, e) =>
			{
				RefreshItems();
			};
			itemList.Loaded += (s, e) =>
			{
				RefreshItems();
			};
			itemList.ItemsSource = Items;
		}

		private async void AddItemBtn_Clicked(object sender, EventArgs e)
		{
			await Navigation.PushAsync(new SearchPage(this), true);
		}

		void OnPlusClicked(object sender, EventArgs e)
		{
			if (((Button)sender).BindingContext is ShoppingListItem it)
			{
				it.Quantity++;
				ShoppingList.UpdateItem(it.product.name, it.Quantity, it.IsPurchased);
				RefreshItems();
			}
		}

		void OnMinusClicked(object sender, EventArgs e)
		{
			if (((Button)sender).BindingContext is ShoppingListItem it && it.Quantity > 1)
			{
				it.Quantity--;
				ShoppingList.UpdateItem(it.product.name, it.Quantity, it.IsPurchased);
				RefreshItems();
			}
			else if (((Button)sender).BindingContext is ShoppingListItem _it && _it.Quantity == 1)
			{
				ShoppingList.RemoveItem(_it.product.name);
				RefreshItems();
			}
		}
		bool _Checkboxevent = true;
		void RepositionAfterToggle(ShoppingListItem it, bool isChecked)
		{
			// 1) vedd ki
			var old = Items.IndexOf(it);
			if (old >= 0) Items.RemoveAt(old);
			it.IsPurchased = isChecked;
			if (it.IsPurchased)
			{
				// 2/a) megvettek a lista VÉGÉN – egyszerűen add hozzá
				Items.Add(it);
			}
			else
			{
				// 2/b) nem-vett (false) elemek az ELEJÉN
				//     ha nem kell ABC-sorrend, elég az első megvett elem indexe
				int firstPurchased = Items.TakeWhile(x => !x.IsPurchased).Count();
				Items.Insert(firstPurchased, it);
			}
		}
		private async void CheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
		{
			if (!((CheckBox)sender).IsLoaded) return;
			//if (!_Checkboxevent) return;
			if (((CheckBox)sender).BindingContext is ShoppingListItem it && _Checkboxevent)
			{
				it.IsPurchased = e.Value;
				//RepositionAfterToggle(it,e.Value);

				if (e.Value)
				{
					Items.Remove(it);
					Items.Add(it);
				}
				else
				{
					Items.Remove(it);
					Items.Insert(0, it);
					//Items.Insert(Items.Count(i => i.IsPurchased), it);
					await Dispatcher.DispatchAsync(() =>
					{
						itemList.ItemsSource = null;
						itemList.ItemsSource = Items;
					});
				}

				ShoppingList.UpdateItem(it.product.name, it.Quantity, it.IsPurchased);

				sum.Text = $"Összesen:\n{Items.Where(item_ => !item_.IsPurchased).Sum(_item => _item.TotalPrice).ToString()} Ft";
			}
		}

		private void delete_item_Clicked(object sender, EventArgs e)
		{
			ShoppingListItem item = (sender as MenuItem).BindingContext as ShoppingListItem;
			ShoppingList.RemoveItem(item.product.name);
			RefreshItems();
		}
	}
}
