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

namespace lab3Client.dialogs
{
    public partial class FieldsSearch : Window
    {
        public FieldsSearch()
        {
            InitializeComponent();
        }

        private void inputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (inputBox.Text == Properties.Resources.FS_WhatToSearch)
            {
                inputBox.ClearValue(ForegroundProperty);
                inputBox.Text = null;
            }
        }
        private void inputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(inputBox.Text))
            {
                inputBox.SetValue(ForegroundProperty, Brushes.Gray);
                inputBox.Text = Properties.Resources.FS_WhatToSearch;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(inputBox.Text) || inputBox.Text == Properties.Resources.FS_WhatToSearch)
            {
                MessageBox.Show(Properties.Resources.FS_WrongInput);
                return;
            }
            string field;
            switch (comboBox.SelectedIndex)
            {
                case 0: field = "name"; break;
                case 1: field = "author"; break;
                case 2: field = "genre"; break;
                case 3: field = "writetime"; break;
                case 4: field = "synopsis"; break;
                default: field = ""; break;
            }
            App.Current.Properties["fieldsSearchRequest"] = field + ";" + inputBox.Text;
            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
