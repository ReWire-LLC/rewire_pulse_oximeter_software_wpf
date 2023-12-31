﻿using PulseOximeter.Model;
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
                //Toggle the mute on or off
                vm.ToggleMute();
            }
        }

        private void SetAlarmsButton_Click(object sender, RoutedEventArgs e)
        {
            //Open the "Alarms" window to allow the user to set some alarms
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
            var vm = this.DataContext as MainWindowViewModel;
            if (vm != null)
            {
                //Check to see if we are already recording
                if (vm.IsRecording)
                {
                    //If already recording, then request to stop recording
                    vm.StopRecording();
                }
                else
                {
                    //Request a file from the user

                    // Configure save file dialog box
                    Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
                    dlg.FileName = "NewRecording"; // Default file name
                    dlg.DefaultExt = ".csv"; // Default file extension
                    dlg.Filter = "CSV files (.csv)|*.csv"; // Filter files by extension

                    // Show save file dialog box
                    Nullable<bool> result = dlg.ShowDialog();

                    // Process save file dialog box results
                    if (result == true)
                    {
                        // Save document
                        string filename = dlg.FileName;

                        vm.StartRecording(filename);
                    }
                }
            }
        }

        private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Close this window (and the application)
            this.Close();
        }

        private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
        {
            //Show the "About" window to the user
            Window_About about_window = new Window_About();
            about_window.Owner = this;
            about_window.ShowDialog();
        }
    }
}
