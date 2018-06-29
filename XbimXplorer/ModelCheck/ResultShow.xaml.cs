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
using ExcelDataReader;
using System.IO;
using OfficeOpenXml;

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


        private void editReport()
        {
            string export = Config_Global.DIR + "\\report.xlsx";
            var stream = File.Open(export, FileMode.Open, FileAccess.Read);
            var excelInfo = ExcelReaderFactory.CreateOpenXmlReader(stream);

            ExcelPackage a;
            

        }

        /// <summary>
        /// 双击错误构件序号即可定位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
