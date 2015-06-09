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
    public partial class keywordsSearch : Window
    {
        public keywordsSearch()
        {
            InitializeComponent();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrEmpty(inputBox.Text))
            {
                MessageBox.Show(Properties.Resources.FS_WrongInput);
                return;
            }
            App.Current.Properties["keywordsSearchRequest"] = inputBox.Text;
            DialogResult = true;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
