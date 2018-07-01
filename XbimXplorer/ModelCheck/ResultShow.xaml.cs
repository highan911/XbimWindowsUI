using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.IO;
using OfficeOpenXml;
using Microsoft.Win32;
using System.Globalization;

namespace XbimXplorer.ModelCheck
{
    /// <summary>
    /// Interaction logic for ResultShow.xaml
    /// </summary>
    public partial class ResultShow : Window
    {
        List<ResultRow> report;
        PreCheckReportInfo reportInfo;

        public ResultShow(List<ResultRow> report)
        {
            InitializeComponent();
            this.report = report;
            ReportGrid.ItemsSource = report;
            
            reportInfo = new PreCheckReportInfo(report);
            SummaryText.Text = reportInfo.GenerateSummary();
            
        }


        private void ExportReport(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog()
            {
                Filter = "Text files (*.xlsx)|*.xlsx"
            };
            string saveFileTo = null;
            if (dialog.ShowDialog() == true)
            {
                saveFileTo = dialog.FileName;
            }

            string export = Config_Global.DIR + "\\report.xlsx";
            var stream = new FileInfo(export);

            
            using (var p = new ExcelPackage(stream))
            {
                //Get the Worksheet created in the previous codesample. 
                var ws = p.Workbook.Worksheets["Sheet1"];
                //Set the cell value using row and column.

                int SUMOFFSET = 7;
                ws.Cells[SUMOFFSET, 2].Value = reportInfo.propertyPass;
                ws.Cells[SUMOFFSET, 3].Value = reportInfo.propertyNotPass;
                ws.Cells[SUMOFFSET, 5].Value = (reportInfo.propertyPass / (reportInfo.propertyTotal + 0.001)).ToString("P", CultureInfo.InvariantCulture);

                ws.Cells[SUMOFFSET + 1, 2].Value = reportInfo.structurePass;
                ws.Cells[SUMOFFSET + 1, 3].Value = reportInfo.structureNotPass;
                ws.Cells[SUMOFFSET + 1, 5].Value = (reportInfo.structurePass / (reportInfo.structureTotal + 0.001)).ToString("P", CultureInfo.InvariantCulture);

                ws.Cells[SUMOFFSET+2, 2].Value = reportInfo.geometryPass;
                ws.Cells[SUMOFFSET+2, 3].Value = reportInfo.geometryNotPass;
                ws.Cells[SUMOFFSET+2, 5].Value = (reportInfo.geometryPass / (reportInfo.geometryTotal + 0.001)).ToString("P", CultureInfo.InvariantCulture);

                ws.Cells[SUMOFFSET + 3, 2].Value = reportInfo.PassTotal();
                ws.Cells[SUMOFFSET + 3, 3].Value = reportInfo.RulesTotal()-reportInfo.PassTotal();
                ws.Cells[SUMOFFSET + 3, 5].Value = reportInfo.PassRate().ToString("P", CultureInfo.InvariantCulture); 



                int OFFSET = 13;
                for(int i = 0; i < report.Count(); i++)
                {
                    ws.Cells[OFFSET + i, 1].Value = i + 1;
                    ws.Cells[OFFSET + i, 2].Value = report[i].ItemContent;
                    ws.Cells[OFFSET + i, 3].Value = report[i].ErrorType;
                    ws.Cells[OFFSET + i, 4].Value = report[i].PassStatus;
                    ws.Cells[OFFSET + i, 5].Value = string.Join(", ", report[i].ErrorEntityLabels); 
                }
                p.SaveAs(new FileInfo(saveFileTo));                
            }
            MessageBox.Show("导出成功");

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
