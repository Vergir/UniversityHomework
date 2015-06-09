using System;
using System.Collections.Generic;
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
using System.Collections.ObjectModel;
using BnC;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Windows.Markup;

namespace lab3Client
{
    public class CatalogueWrapper : INotifyPropertyChanged
    {
        private BookCatalogue catalogue;
        public BookCatalogue Catalogue
        {
            get { return catalogue; }
            set { SetField(ref catalogue, value);}
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        public CatalogueWrapper() { }
        public CatalogueWrapper(BookCatalogue catalogue)
        {
            this.Catalogue = catalogue;
        }
    }
    public class BooksToDGEntrysConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return null;
            var catalogue = value as BookCatalogue;
            var resultList = new ObservableCollection<Dictionary<string, string>>();
            foreach (var book in catalogue)
            {
                var tmp = new Dictionary<string,string>();
                foreach(Book.fields fieldName in Enum.GetValues(typeof(Book.fields)))
                    tmp[fieldName.ToString().ToUpper()] = book.GetInfoAbout(fieldName); 
                resultList.Add(tmp);
            }
            return resultList;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return DependencyProperty.UnsetValue;
        }
    }

    public partial class MainWindow : Window
    {
        private Client client;
        private CatalogueWrapper catalogue;
        
        private void GetCatalogue() 
        {
            var rawCatalogue = new byte[]{};
            if (client.TryByteRequest(ref rawCatalogue, Client.Request.GetCatalogue))
              catalogue.Catalogue = BookSerialization.GetCatalogueFromByteArray(rawCatalogue);
            else
                MessageBox.Show(Properties.Resources.NoConnection);
            ClearSorts();
            ClearFilter();
            ClearSearch();
        }

        private void ClearSorts()
        {
            foreach (var col in catalogueGrid.Columns)
                if (col.SortDirection != null)
                    col.SortDirection = null;
            CollectionViewSource.GetDefaultView(catalogueGrid.ItemsSource).SortDescriptions.Clear();
            sortingLabel.Content = Properties.Resources.SortLabel + "-";
        }
        
        private void ClearFilter()
        {
            CollectionViewSource.GetDefaultView(catalogueGrid.ItemsSource).Filter = new Predicate<object>((obj) => { return true; });
            filterLabel.Content = Properties.Resources.FilterLabel + "-";
        }
        private void ClearSearch()
        {
            searchLabel.Content = Properties.Resources.SearchLabel + "-";
            searchLabel.ToolTip = null;
        }
        
        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            catalogue = new CatalogueWrapper();
            catalogue = (CatalogueWrapper)Resources["catalogue"];
            this.client = (Client)App.Current.Properties["client"];
            
            GetCatalogue();

        }

        private void catalogueGrid_Loaded(object sender, RoutedEventArgs e)
        {
            catalogueGrid.AutoGenerateColumns = false;
            catalogueGrid.IsReadOnly = true;
            catalogueGrid.CanUserAddRows = false;
            catalogueGrid.CanUserDeleteRows = false;
            catalogueGrid.CanUserReorderColumns = true;
            catalogueGrid.CanUserResizeColumns = false;
            catalogueGrid.CanUserResizeRows = false;
            catalogueGrid.CanUserSortColumns = true;
            catalogueGrid.SelectionMode = DataGridSelectionMode.Single;
            catalogueGrid.SelectionUnit = DataGridSelectionUnit.FullRow;
            catalogueGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
            catalogueGrid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;
            catalogueGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
        }
        private void catalogueGrid_Sorting(object sender, DataGridSortingEventArgs e)
        {
            string sortDirection = null;
            switch (e.Column.SortDirection)
            {
                case ListSortDirection.Ascending: sortDirection = Properties.Resources.Sort_Down; break;
                case ListSortDirection.Descending: sortDirection = Properties.Resources.Sort_Up; break;
                case null: sortDirection = Properties.Resources.Sort_Up; break;
            }
            sortingLabel.Content = String.Format("{0} {1} - {2}",Properties.Resources.SortLabel, e.Column.Header.ToString(), sortDirection);
        }
        private void synopsisText_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var text = sender as TextBlock;
            if (text.TextWrapping == TextWrapping.NoWrap)
                text.TextWrapping = TextWrapping.WrapWithOverflow;
            else
                text.TextWrapping = TextWrapping.NoWrap;
        }

        private void MainMenu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TabControl menu = sender as TabControl;
            foreach (TabItem tab in menu.Items)
                if (tab.IsSelected)
                    tab.BorderBrush = Brushes.Gray;
                else
                    tab.BorderBrush = Brushes.White;
        }
        
        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            if (new dialogs.addBook().ShowDialog() == true)
            {
                Book book = App.Current.Properties["book"] as Book;
                byte[] rawBook = book.ToByteArray();
                if (client.TryByteRequest(ref rawBook, Client.Request.AddBook))
                    GetCatalogue();
                else
                    MessageBox.Show(Properties.Resources.NoConnection);
            }
        }
        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            int index = catalogueGrid.SelectedIndex;
            if (index == -1)
            {
                MessageBox.Show(Properties.Resources.RemoveNoSelect);
                return;
            }
            Book book = catalogue.Catalogue.GetBookByIndex(index);
            string bookName = book.GetInfoAbout(Book.fields.name);
            if (MessageBox.Show(Properties.Resources.RemoveConfirm + bookName + "\"?", Properties.Resources.RemoveConfirmTitle, MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                byte[] transferingPacket = BitConverter.GetBytes(index);
                if (client.TryByteRequest(ref transferingPacket, Client.Request.RemoveBook))
                    catalogue.Catalogue = BookSerialization.GetCatalogueFromByteArray(transferingPacket);
                else
                    MessageBox.Show(Properties.Resources.NoConnection);
            }

        }        
        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            GetCatalogue();
        }
        
        private void fieldsSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (new dialogs.FieldsSearch().ShowDialog() == true)
            {
                string searchRequest = App.Current.Properties["fieldsSearchRequest"] as String;
                string[] searchInputs = searchRequest.Split(new char[]{';'}, 2);
                string field;
                switch (searchInputs[0])
                {
                    case "name": field = Properties.Resources.FieldName; break;
                    case "author": field = Properties.Resources.FieldAuthor; break;
                    case "genre": field = Properties.Resources.FieldGenre; break;
                    case "writetime": field = Properties.Resources.FieldWriteTime; break;
                    case "isbn": field = Properties.Resources.FieldISBN; break;
                    default: field = "???"; break;
                }
                byte[] transferMessage = System.Text.Encoding.UTF8.GetBytes(searchRequest);
                if (client.TryByteRequest(ref transferMessage, Client.Request.SearchField))
                {
                    catalogue.Catalogue = BookSerialization.GetCatalogueFromByteArray(transferMessage);
                    searchLabel.Content = String.Format("{0}{1} {2} \"{3}\"", Properties.Resources.SearchLabel, field, Properties.Resources.SearchLabelInc, searchInputs[1]);
                    searchLabel.ToolTip = null;
                }
                else
                    MessageBox.Show(Properties.Resources.NoConnection);
            }
        }
        private void keywordsSearchButton_Click(object sender, RoutedEventArgs e)
        {
            if (new dialogs.keywordsSearch().ShowDialog() == true)
            {
                string searchRequest = App.Current.Properties["keywordsSearchRequest"] as String;
                string labelTooltip = searchRequest.Replace(";", Environment.NewLine);
                byte[] transferMessage = System.Text.Encoding.UTF8.GetBytes(searchRequest);
                if (client.TryByteRequest(ref transferMessage, Client.Request.SearchKeywords))
                {
                    catalogue.Catalogue = BookSerialization.GetCatalogueFromByteArray(transferMessage);
                    searchLabel.Content = Properties.Resources.SearchLabel + Properties.Resources.SearchKW;
                    searchLabel.ToolTip = Properties.Resources.KeywordsHover + Environment.NewLine + labelTooltip;
                }
                else
                    MessageBox.Show(Properties.Resources.NoConnection);
            }
        }
        private void searchStop_Click(object sender, RoutedEventArgs e)
        {
            GetCatalogue();
        }
        private void filterButton_Click(object sender, RoutedEventArgs e)
        {
            if (new dialogs.filterDialog().ShowDialog() == true)
            {
                var viewSource = CollectionViewSource.GetDefaultView(catalogueGrid.ItemsSource);
                int allBooks = 0;
                foreach (var book in catalogueGrid.ItemsSource)
                    allBooks++;

                viewSource.Filter = App.Current.Properties["filter"] as Predicate<object>;
                foreach(var showingBook in viewSource)
                    if (viewSource.Contains(showingBook))
                        allBooks--;
                filterLabel.Content = App.Current.Properties["filterDescription"].ToString();
            }
        }
        private void filterStop_Click(object sender, RoutedEventArgs e)
        {
            ClearFilter();
        }
        
        private void serverButton_Click(object sender, RoutedEventArgs e)
        {
            var serverChoser = new dialogs.chooseServer();
            if (serverChoser.ShowDialog() == true)
            {
                client = new Client(serverChoser.host);
                GetCatalogue();
            }
        }
        private void mailDeveloperButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("mailto:Phlash9[at]mail.ru?subject=DeWitt");
        }
        private void skinsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SkinsExpander.IsMouseOver)
                SkinsExpander.IsExpanded = !SkinsExpander.IsExpanded;
        }
        private void defaultSkin_Click(object sender, RoutedEventArgs e)
        {
            var newSkin = new DefaultSkin();
            newSkin.InitializeComponent();
            App.Current.Resources = newSkin;
        }
        private void magicSkin_Click(object sender, RoutedEventArgs e)
        {
            var newSkin = new MagicSkin();
            newSkin.InitializeComponent();
            App.Current.Resources = newSkin;
        }
        

        private void sortingLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ClearSorts();
        }
        private void filterLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ClearFilter();
        }
        private void searchLabel_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            GetCatalogue();
        }

        private void aboutProgram_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(String.Format("{1}{0}{2}{0}{3}{0}{4}{0}{5}", Environment.NewLine, Properties.Resources.About0, Properties.Resources.About1, Properties.Resources.About2, Properties.Resources.About3, Properties.Resources.About4), Properties.Resources.AboutTitle);
        }

        private void UpdateStrings()
        {
            MenuTabCatalogue.Header = Properties.Resources.MenuTabCatalogue;
            MenuTabSF.Header = Properties.Resources.MenuTabSearch;
            MenuTabMisc.Header = Properties.Resources.MenuTabMisc;
            AddButton.Content = Properties.Resources.MenuButtonAdd;
            RemoveButton.Content = Properties.Resources.MenuButtonRemove;
            RefreshButton.Content = Properties.Resources.MenuButtonRefresh;
            FSButton.Content = Properties.Resources.MenuButtonFieldsSearch;
            KSButton.Content = Properties.Resources.MenuButtonKeywordsSearch;
            SearchStopButton.Content = Properties.Resources.MenuButtonSearchStop;
            FilterButton.Content = Properties.Resources.MenuButtonFilter;
            FilterStopButton.Content = Properties.Resources.MenuButtonFilterStop;
            ServerButton.Content = Properties.Resources.MenuButtonServer;
            SkinsExpander.Header = Properties.Resources.MenuButtonSkins;
            DefaultButton.Content = Properties.Resources.SkinDefault;
            MagicButton.Content = Properties.Resources.SkinMagic;
            AboutButton.Content = Properties.Resources.MenuButtonAbout;
            MailButton.Content = Properties.Resources.MenuButtonMail;
            catalogueGrid.Columns[0].Header = Properties.Resources.FieldName;
            catalogueGrid.Columns[1].Header = Properties.Resources.FieldAuthor;
            catalogueGrid.Columns[2].Header = Properties.Resources.FieldGenre;
            catalogueGrid.Columns[3].Header = Properties.Resources.FieldWriteTime;
            catalogueGrid.Columns[4].Header = Properties.Resources.FieldISBN;
            BooksLabel.Content = Properties.Resources.CountLabel;
            
        }
        private void US_Click(object sender, MouseButtonEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            UpdateStrings();
            GetCatalogue();
        }
        private void RU_Click(object sender, MouseButtonEventArgs e)
        {
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("ru-RU");
            UpdateStrings();
            GetCatalogue();
        }
    }
}
