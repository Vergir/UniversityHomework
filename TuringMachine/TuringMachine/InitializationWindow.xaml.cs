using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace TuringMachine
{
   

    public partial class InitializationWindow : Window
    {
        bool? program;
        ObservableCollection<dataGridCell> dataGridItemsSource;

        public InitializationWindow()
        {
            InitializeComponent();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox it = sender as TextBox;
            if ((((tapeStackPanel.Children[tapeStackPanel.Children.Count - 1] as StackPanel).Children[1]) as TextBox).Text != "")
            {
                StackPanel tmpSP = new StackPanel();

                CheckBox tmpCB = new CheckBox();
                tmpCB.Checked += CheckBox_Checked;
                tmpCB.Visibility = System.Windows.Visibility.Hidden;

                TextBox tmpTB = new TextBox();
                tmpTB.TextChanged += TextBox_TextChanged;

                tmpSP.Children.Add(tmpCB);
                tmpSP.Children.Add(tmpTB);

                tapeStackPanel.Children.Add(tmpSP);
            }
            if (it.Text == "")
                tapeStackPanel.Children.Remove((it.Parent as StackPanel));
            else
            {
                ((it.Parent as StackPanel).Children[0] as CheckBox).Visibility = System.Windows.Visibility.Visible;
            }
        }
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            CheckBox it = sender as CheckBox;
            foreach (StackPanel stackPanel in tapeStackPanel.Children)
                if ((stackPanel.Children[0] as CheckBox) != it)
                    (stackPanel.Children[0] as CheckBox).IsChecked = false;
        }

        private void showTextBox(object sender, RoutedEventArgs e)
        {
            program = true;
            textBox.Visibility = System.Windows.Visibility.Visible;
            dataGrid.Visibility = System.Windows.Visibility.Collapsed;
            comboBox.Visibility = System.Windows.Visibility.Collapsed;
        }
        private void showDataGrid(object sender, RoutedEventArgs e)
        {
            program = false;
            dataGrid.Visibility = System.Windows.Visibility.Visible;
            comboBox.Visibility = System.Windows.Visibility.Collapsed;
            textBox.Visibility = System.Windows.Visibility.Collapsed;
        }
        private void showComboBox(object sender, RoutedEventArgs e)
        {
            program = null;
            comboBox.Visibility = System.Windows.Visibility.Visible;
            dataGrid.Visibility = System.Windows.Visibility.Collapsed;
            textBox.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveTape())
            {
                MessageBox.Show("Tape includes invalid data. Please check your input");
                return;
            }
            if (!SaveHead())
            {
                MessageBox.Show("You didn't choose where the Head will be.");
                return;
            }
            if (!SaveProgram())
            {
                MessageBox.Show("Could not load Program. Please check input or choosen another Initialization method");
                return;
            }
            App.Current.Properties["dataGrid"] = dataGridItemsSource;
            DialogResult = true;
        }

        private bool SaveHead()
        {
            for (int loop = 0; loop < tapeStackPanel.Children.Count-1; loop++)
                if (((tapeStackPanel.Children[loop] as StackPanel).Children[0] as CheckBox).IsChecked == true)
                {
                    App.Current.Properties["HP"] = loop;
                    return true;
                }
            return false;
        }
        private bool SaveTape()
        {
            int count = tapeStackPanel.Children.Count-1;
            int?[] tape = new int?[count];
            for (int loop = 0; loop < count; loop++)
            {
                TextBox cell = (tapeStackPanel.Children[loop] as StackPanel).Children[1] as TextBox;
                if (String.IsNullOrWhiteSpace(cell.Text))
                    tape[loop] = null;
                else
                {
                    int tmp;
                    if (!Int32.TryParse(cell.Text, out tmp))
                        return false;
                    tape[loop] = tmp;
                }
            }
            App.Current.Properties["tape"] = tape;
            return true;

        }
        private bool SaveProgram()
        {
            switch (program)
            {
                case true: return TextBoxReadProgram();
                case false: return DataGridReadProgram();
                case null: return ComboBoxReadProgram();
                default: return false;
            }
        }
        private bool TextBoxReadProgram()
        {
            string[] program = textBox.Text.Split(new char[] { ',' });
            App.Current.Properties["program"] = program;
            return true;
        }
        private bool DataGridReadProgram()
        {
            int count = dataGridItemsSource.Count;
            int? curKey;
            string[] program = new string[3*count];
            for (int loop = 0; loop < count; loop++)
            {
                if (String.IsNullOrWhiteSpace(dataGridItemsSource[loop].empty))
                    curKey = null;
                else
                {
                    int temp;
                    if (!Int32.TryParse(dataGridItemsSource[loop].empty, out temp))
                        return false;
                    curKey = (int?)temp;
                }
                
                string[] q1Split = dataGridItemsSource[loop].one.Split(new char[] { ',', ';', '!', '.' });
                if (q1Split.Length != 3)
                    return false;
                program[loop] = String.Format("q1;{0};{1};{2};{3}", curKey.ToString(), q1Split[0], q1Split[1], q1Split[2]);

                string[] q2Split = dataGridItemsSource[loop].two.Split(new char[] { ',' });
                if (q2Split.Length != 3)
                    return false;
                program[loop+count] = String.Format("q2;{0};{1};{2};{3}", curKey.ToString(), q2Split[0], q2Split[1], q2Split[2]); 
                
                string[] q3Split = dataGridItemsSource[loop].three.Split(new char[] { ',' });
                if (q3Split.Length != 3)
                    return false;
                program[loop+count+count] = String.Format("q3;{0};{1};{2};{3}", curKey.ToString(), q3Split[0], q3Split[1], q3Split[2]);
            }
            App.Current.Properties["program"] = program;
            return true;
        }
        private bool ComboBoxReadProgram()
        {
            MachineInsides.MachineProgram program = null;
            try
            {
                program = MachineInsides.MachineProgram.LoadFromFile(comboBox.SelectedItem.ToString());
            }
            catch
            {
                return false;
            }
            if (program == null)
                return false;
            App.Current.Properties["programProcessed"] = program; 
            return true;
        }

        private void dataGrid_Initialize(object sender, RoutedEventArgs e)
        {
            var temp = new List<dataGridCell>(new dataGridCell[] 
            { new dataGridCell(""),
                new dataGridCell("0"),
                new dataGridCell("1"),
                new dataGridCell("2")
            });
            dataGridItemsSource = new ObservableCollection<dataGridCell>(temp);
            dataGrid.ItemsSource = dataGridItemsSource;

            dataGrid.AutoGenerateColumns = false;
            dataGrid.CanUserAddRows = true;
            dataGrid.CanUserDeleteRows = true;
            dataGrid.CanUserReorderColumns = false;
            dataGrid.CanUserResizeColumns = false;
            dataGrid.CanUserResizeRows = false;
            dataGrid.CanUserSortColumns = false;
            dataGrid.SelectionUnit = DataGridSelectionUnit.CellOrRowHeader;
            dataGrid.SelectionMode = DataGridSelectionMode.Single;
        }
        private void comboBox_Initialized(object sender, EventArgs e)
        {
            List<string> fileNames = new List<string>();
            foreach (var pathName in System.IO.Directory.GetFiles(System.IO.Directory.GetCurrentDirectory(), "*.*", System.IO.SearchOption.TopDirectoryOnly))
                fileNames.Add(pathName.Substring(pathName.LastIndexOf('\\')+1));
            comboBox.ItemsSource = fileNames;
        }
    }
}
