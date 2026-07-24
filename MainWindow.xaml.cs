using System.ComponentModel;
using System.Windows;
using FreeSteamGames.ViewModels;
using Wpf.Ui.Controls;

namespace FreeSteamGames;

public partial class MainWindow : FluentWindow
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    private void FluentWindow_Closing(object? sender, CancelEventArgs e)
    {
        if (App.IsExiting) return;

        e.Cancel = true;
        Hide();
    }
}
