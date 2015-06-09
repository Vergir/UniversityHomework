using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace TuringMachine
{
    public class dataGridCell
    {
        public string empty { get; set; }
        public string one { get; set; }
        public string two { get; set; }
        public string three { get; set; }

        public dataGridCell()
        {
            empty = "";
            one = "";
            two = "";
            three = "";
        }
        public dataGridCell(string empty, string one = "", string two = "", string three = "")
        {
            this.empty = empty;
            this.one = one;
            this.two = two;
            this.three = three;
        }

    }
    public class TapeToSPConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            var upperTape = (value as MachineInsides.MachineTape);
            var tape = upperTape.tape;

            List<TapeElement> result = new List<TapeElement>();
            while (tape.previous != null)
            {
                tape = tape.previous;
            }
            SolidColorBrush color;
            while (tape.next != null)
            {
                color = (tape.position == upperTape.headPosition) ? Brushes.Crimson : null;
                result.Add(new TapeElement(tape.key, color));
                tape = tape.next;
            }
            color = (tape.position == upperTape.headPosition) ? Brushes.Crimson : null;
            result.Add(new TapeElement(tape.key, color));
            return result;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public class TapeElement
    {
        public string key {get; set;}
        public SolidColorBrush color { get; set; }

        public TapeElement(int? key, SolidColorBrush color)
        {
            this.key = key.ToString();
            this.color = color;
        }
    }

    public partial class MainWindow : Window
    {
        List<dataGridCell> dataGridItemsSource;
        TurMach machine;
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            int?[] keys = (int?[])App.Current.Properties["tape"];
            int headPosition = (int)App.Current.Properties["HP"];
            if (App.Current.Properties["programProcessed"] != null)
                machine = new TurMach(keys, headPosition, (MachineInsides.MachineProgram)App.Current.Properties["programProcessed"]);
            else
                machine = new TurMach(keys, headPosition, (string[])App.Current.Properties["program"]);
            Resources["machine"] = machine;

            this.machine.program.SaveToFile(DateTime.Now.ToString("dd-MM-yyyy hh-mm-ss") + ".tm");
        }
        private void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            dataGrid.IsReadOnly = true;
            dataGrid.AutoGenerateColumns = false;
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.CanUserReorderColumns = false;
            dataGrid.CanUserResizeColumns = false;
            dataGrid.CanUserResizeRows = false;
            dataGrid.CanUserSortColumns = false;
            dataGrid.SelectionUnit = DataGridSelectionUnit.Cell;
            dataGrid.SelectionMode = DataGridSelectionMode.Single;

            dataGridItemsSource = new List<dataGridCell>();
            string[] row = new string[4];
            HashSet<int?> possibleKeys = new HashSet<int?>();
            foreach (var keyTuple in machine.program.program.Keys)
                possibleKeys.Add(keyTuple.Item2);
            foreach (var possibleKey in possibleKeys)
            {
                row[0] = possibleKey.ToString();
                for (int loop = 1; loop < 4; loop++)
                    row[loop] = String.Format("{0},{1},{2}", "q" + machine.program.program[new Tuple<int, int?>(loop, possibleKey)].Item1, machine.program.program[new Tuple<int, int?>(loop, possibleKey)].Item2.ToString(), machine.program.program[new Tuple<int, int?>(loop, possibleKey)].Item3.ToString().Substring(0, 1));
                dataGridItemsSource.Add(new dataGridCell(row[0], row[1], row[2], row[3]));
            }
            dataGrid.ItemsSource = dataGridItemsSource;
        }
        
        private bool StateIsValid()
        {
            string state = stateTB.Text;
            if (state != "q1" && state != "q2" && state != "q3")
                return false;
            machine.State = Int32.Parse(state[1].ToString());
            return true;
        }
        private void FinishProgram()
        {
            startButton.IsEnabled = false;
            stopButton.IsEnabled = false;
            stepButton.IsEnabled = false;
            sliderSP.Visibility = System.Windows.Visibility.Collapsed;
            stateTB.IsEnabled = false;
            MessageBox.Show("Program stopped");
        }

        private void Start_Machine(object sender, RoutedEventArgs e)
        {
            if (!StateIsValid())
            {
                MessageBox.Show("State has incorrect input. Please check that field");
                Stop_Machine(null, null);
            }
            speedSlider.Value = speedSlider.Minimum;
            speedSlider_ValueChanged(null, null);
            sliderSP.Visibility = System.Windows.Visibility.Visible;
            stateTB.IsReadOnly = true;
            stepButton.IsEnabled = false;
            startButton.IsEnabled = false;
            stopButton.IsEnabled = true;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += (__, _) =>
            {
                machine.StartExecution();
            };
            worker.RunWorkerCompleted += (__, _) =>
            {
                if (machine.State == 0)
                    FinishProgram();
                else
                    FinishProgram();
            };
            worker.RunWorkerAsync();
        }
        private void Step_Machine(object sender, RoutedEventArgs e)
        {
            if (!StateIsValid())
            {
                MessageBox.Show("State has incorrect input. Please check that field");
                return;
            }
            if (machine.DoStep())
                FinishProgram();
        }
        private void Stop_Machine(object sender, RoutedEventArgs e)
        {
            machine.StopExecution();
            stateTB.IsReadOnly = false;
            startButton.IsEnabled = true;
            stepButton.IsEnabled = true;
            stopButton.IsEnabled = false;
            sliderSP.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void speedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            machine.runningDelay = 50 + (int)(speedSlider.Maximum - speedSlider.Value);
        }

    }
}
