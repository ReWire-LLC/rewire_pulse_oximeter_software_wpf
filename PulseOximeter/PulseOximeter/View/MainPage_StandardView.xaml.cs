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

namespace PulseOximeter.View
{
    /// <summary>
    /// Interaction logic for MainPage_StandardView.xaml
    /// </summary>
    public partial class MainPage_StandardView : Page
    {
        public MainPage_StandardView(MainWindowViewModel view_model)
        {
            InitializeComponent();
            this.DataContext = view_model;
        }
    }
}
