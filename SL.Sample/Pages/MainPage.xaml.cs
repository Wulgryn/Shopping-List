using SL.Sample.Models;
using SL.Sample.PageModels;

namespace SL.Sample.Pages
{
    public partial class MainPage : ContentPage
    {
        public MainPage(MainPageModel model)
        {
            InitializeComponent();
            BindingContext = model;
        }
    }
}