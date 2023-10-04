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
        private MainPage_StandardView? _standard_view = null;
        private MainPage_DetailedView? _detailed_view = null;

        public MainWindow()
        {
            InitializeComponent();

            var vm = new MainWindowViewModel(_model);

            DataContext = vm;
            _detailed_view = new MainPage_DetailedView(vm);
            _standard_view = new MainPage_StandardView(vm);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(_standard_view);
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
                    MainFrame.Navigate(_detailed_view);
                }
                else
                {
                    MainFrame.Navigate(_standard_view);
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

        private void SetAlarmsButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as MainWindowViewModel;
            if (vm != null)
            {
                Window_SetAlarms set_alarms_window = new Window_SetAlarms(_model);
                set_alarms_window.Owner = this;
                set_alarms_window.ShowDialog();
            }
        }

        private void RecordButton_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
