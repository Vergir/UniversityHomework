using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using BnC;

namespace lab3Client.dialogs
{
    public partial class addBook : Window
    {
        public addBook()
        {
            InitializeComponent();
        }

        private Book CheckBoxes()
        {
            string regexDatePattern = @"^[0-9]{4}$";
            string regexISBNPattern = @"^[0-9]{13}$";
            
            if (String.IsNullOrEmpty(nameBox.Text))
            {
                MessageBox.Show(Properties.Resources.Add_ErrorInfo);
                nameBox.BorderBrush = Brushes.Red;
                return null;
            }
            if (String.IsNullOrEmpty(authorBox.Text))
            {
                MessageBox.Show(Properties.Resources.Add_ErrorInfo);
                authorBox.BorderBrush = Brushes.Red;
                return null;
            }
            if (String.IsNullOrEmpty(genreBox.Text))
            {
                MessageBox.Show(Properties.Resources.Add_ErrorInfo);
                genreBox.BorderBrush = Brushes.Red;
                return null;
            }
            if (!Regex.IsMatch(writetimeBox.Text, regexDatePattern))
            {
                MessageBox.Show(Properties.Resources.Add_ErrorWriteTime);
                writetimeBox.BorderBrush = Brushes.Red;
                return null;
            }
            if (!Regex.IsMatch(isbnBox.Text, regexISBNPattern))
            {
                MessageBox.Show(Properties.Resources.Add_ErrorISBN);
                isbnBox.BorderBrush = Brushes.Red;
                return null;
            }
            string[] book = { nameBox.Text, authorBox.Text, genreBox.Text, writetimeBox.Text, isbnBox.Text, synopsisBox.Text};
            return new Book(book);
        }
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            Book book = CheckBoxes();
            if (book != null)
            {
                App.Current.Properties["book"] = book;
                DialogResult = true;
            }
            
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void tetBox_lostFocus(object sender, RoutedEventArgs e)
        {
            var box = sender as TextBox;
            box.ClearValue(BorderBrushProperty);
        }
    }
}
