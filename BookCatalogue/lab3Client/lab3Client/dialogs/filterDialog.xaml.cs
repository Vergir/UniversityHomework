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
    public partial class filterDialog : Window
    {
        public filterDialog()
        {
            InitializeComponent();
            comboBox.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
            {
                App.Current.Properties["filter"] = CreateFilter(42);
                App.Current.Properties["filterDescription"] = CreateFilterDescription(42);
                DialogResult = true;
                return;
            }

            long X;
            if (!Int64.TryParse(inputBox.Text, out X) || X < 1)
            {
                MessageBox.Show(Properties.Resources.Filter_WrongInput);
                return;
            }

            App.Current.Properties["filter"] = CreateFilter(X);
            App.Current.Properties["filterDescription"] = CreateFilterDescription(X); 
            DialogResult = true;
        }

        private string CreateFilterDescription(long X)
        {
            var builder = new StringBuilder(Properties.Resources.FilterLabel);
            switch (comboBox.SelectedIndex)
            {
                case 1: return builder.Append(Properties.Resources.FilterLabelWordsX1 + " " + X + " " + Properties.Resources.FilterLabelWordsX2).ToString();
                case 2: return builder.Append(Properties.Resources.FieldWriteTime + " < " + X).ToString();
                case 3: return builder.Append(Properties.Resources.FieldWriteTime + " > " + X).ToString();
                case 4: return builder.Append(Properties.Resources.FieldISBN + " < " + X).ToString();
                case 5: return builder.Append(Properties.Resources.FieldISBN + " < " + X).ToString();
                default: return builder.Append("-").ToString();
            }
        }

        private Predicate<object> CreateFilter(long X)
        {
            switch (comboBox.SelectedIndex)
            {
                default: return (obj) => {return true;};
                case 1: return (obj) => 
                { 
                    string name = (obj as Dictionary<string, string>)["NAME"];
                    return (name.Split(new char[]{' '}).LongLength == X);
                };
                case 2: return (obj) =>
                {
                    long WriteTime = Int64.Parse((obj as Dictionary<string, string>)["WRITETIME"]);
                    return (WriteTime < X);
                };
                case 3: return (obj) =>
                {
                    long WriteTime = Int64.Parse((obj as Dictionary<string, string>)["WRITETIME"]);
                    return (WriteTime > X);
                };
                case 4: return (obj) =>
                {
                    long ISBN = Int64.Parse((obj as Dictionary<string, string>)["ISBN"]);
                    return (ISBN < X);
                };
                case 5: return (obj) =>
                {
                    long ISBN = Int64.Parse((obj as Dictionary<string, string>)["ISBN"]);
                    return (ISBN > X);
                };
            }
        }

        private void inputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (inputBox.Text == "X")
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
                inputBox.Text = "X";
            }
        }

        private void comboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (comboBox.SelectedIndex == 0)
                inputBox.Visibility = System.Windows.Visibility.Collapsed;
            else
                inputBox.Visibility = System.Windows.Visibility.Visible;
        }
    }
}
