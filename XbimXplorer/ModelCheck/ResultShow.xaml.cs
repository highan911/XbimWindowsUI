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

namespace XbimXplorer.ModelCheck
{
    /// <summary>
    /// Interaction logic for ResultShow.xaml
    /// </summary>
    public partial class ResultShow : Window
    {
        public ResultShow(List<ResultRow> report)
        {
            InitializeComponent();
            ReportGrid.ItemsSource = report;
            
            PreCheckReportInfo reportInfo = new PreCheckReportInfo(report);
            SummaryText.Text = reportInfo.GenerateSummary();
            
        }

        private void ListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ListBox listbox = (ListBox)sender;
            string str = listbox.SelectedItem.ToString();

            XplorerMainWindow mainWindow  = Application.Current.Windows.OfType<XplorerMainWindow>().FirstOrDefault();
            mainWindow.ElementFocused(Int32.Parse(str));
            //MessageBox.Show(str, "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
