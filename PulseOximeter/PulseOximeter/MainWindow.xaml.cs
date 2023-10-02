using PulseOximeter.Model;
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
            _model.Start();
        }
    }
}
