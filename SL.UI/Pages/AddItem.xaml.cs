using SL.Core;
using System.Threading.Tasks;

namespace SL.UI.Pages;

public partial class AddItem : ContentPage
{
	SearchPage sp;

	public AddItem()
	{
		InitializeComponent();
	}

	public AddItem(SearchPage sp) : this()
	{
		this.sp = sp;
	}

	private async void save_item_Clicked(object sender, EventArgs e)
	{
		if(ProductList.GetItem(item_name.Text ?? "") is not null)
		{
			await DisplayAlert("Error", "An item with that name already exists.", "OK");
			return;
		}
		if(string.IsNullOrWhiteSpace(item_name.Text))
		{
			await DisplayAlert("Error", "Item name cannot be empty.", "OK");
			return;
		}
		if(!int.TryParse(item_price.Text, out int price))
		{
			await DisplayAlert("Error", "Invalid price.", "OK");
			return;
		}
		ProductList.AddItem(item_name.Text ?? "ERROR", price);
		await Application.Current.MainPage.Navigation.PopAsync(true);
	}
}