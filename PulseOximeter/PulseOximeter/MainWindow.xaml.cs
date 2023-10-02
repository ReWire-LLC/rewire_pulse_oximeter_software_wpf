using PulseOximeter.Model;
using PulseOximeter.View;
using PulseOximeter.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PulseOximeter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ApplicationModel _model = new ApplicationModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel(_model);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainPage_StandardView standard_view = new MainPage_StandardView(this.DataContext as MainWindowViewModel);
            MainFrame.Navigate(standard_view);
            _model.Start();
        }

        private void DetailedViewButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.ToggleDetailedView();

                if (vm.DetailedView)
                {
                    MainPage_DetailedView detailed_view = new MainPage_DetailedView(this.DataContext as MainWindowViewModel);
                    MainFrame.Navigate(detailed_view);
                }
                else
                {
                    MainPage_StandardView standard_view = new MainPage_StandardView(this.DataContext as MainWindowViewModel);
                    MainFrame.Navigate(standard_view);
                }
            }
        }

        private void MuteButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (vm != null)
            {
                vm.ToggleMute();
            }
        }
    }
}
