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
using System.Windows.Shapes;

namespace PulseOximeter.View
{
    /// <summary>
    /// Interaction logic for Window_SetAlarms.xaml
    /// </summary>
    public partial class Window_SetAlarms : Window
    {
        public Window_SetAlarms(ApplicationModel model)
        {
            InitializeComponent();
            this.DataContext = new Window_SetAlarms_ViewModel(model);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as Window_SetAlarms_ViewModel;
            if (vm != null)
            {
                //Get the settings entered by the user
                var hr_min_string = HeartRateMinimumAlarmTextBox.Text;
                var hr_max_string = HeartRateMaximumAlarmTextBox.Text;
                var spo2_min_string = SpO2MinimumAlarmTextBox.Text;
                var spo2_max_string = SpO2MaximumAlarmTextBox.Text;

                //Convert the values into integers
                bool parse_success_hr_min = Int32.TryParse(hr_min_string, out int hr_min);
                bool parse_success_hr_max = Int32.TryParse(hr_max_string, out int hr_max);
                bool parse_success_spo2_min = Int32.TryParse(spo2_min_string, out int spo2_min);
                bool parse_success_spo2_max = Int32.TryParse(spo2_max_string, out int spo2_max);

                if (parse_success_hr_min && parse_success_hr_max && parse_success_spo2_min && parse_success_spo2_max)
                {
                    vm.ApplyAlarmSettings(hr_min, hr_max, spo2_min, spo2_max);
                    this.Close();
                }
                else
                {
                    ErrorMessageTextBlock.Text = "You must enter valid integer values. Please correct your entries and try again.";
                }
            }
        }
    }
}
