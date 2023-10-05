﻿using PulseOximeter.Model;
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
    /// Interaction logic for Window_About.xaml
    /// </summary>
    public partial class Window_About : Window
    {
        public Window_About()
        {
            InitializeComponent();
            ApplicationVersionTextBox.Text = ApplicationConfiguration.GetApplicationVersion();
            ApplicationBuildDateTextBox.Text = ApplicationConfiguration.GetBuildDate().ToString();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
