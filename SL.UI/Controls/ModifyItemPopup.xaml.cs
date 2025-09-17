using CommunityToolkit.Maui.Views;

namespace SL.UI.Controls;

public partial class ModifyItemPopup : Popup
{
	public ModifyItemPopup(string initial = "")
	{
		InitializeComponent();
		Input.Text = initial;
	}
	void OnOk(object sender, EventArgs e) => Close(Input.Text);
	void OnCancel(object sender, EventArgs e) => Close(null);
}