// RedditVideoGenerator.UI/MainWindow.xaml.cs
using RedditVideoGenerator.UI.ViewModels; // <<<< ENSURE THIS USING DIRECTIVE IS PRESENT
using System.Windows;

namespace RedditVideoGenerator.UI
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}
