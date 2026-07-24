using FreeSteamGames.ViewModels;
using Wpf.Ui.Controls;

namespace FreeSteamGames;

public partial class SettingsWindow : FluentWindow
{
    public SettingsWindow(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
