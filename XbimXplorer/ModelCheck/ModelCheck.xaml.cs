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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Xbim.Presentation.XplorerPluginSystem;


//https://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView
//https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/data-binding-overview#creating-a-binding

namespace XbimXplorer.ModelCheck
{
    /// <summary>
    /// ModelCheck.xaml 的交互逻辑
    /// </summary>
    /// 
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu,
         "Check/ModelCheck")]
    public partial class ModelCheck : IXbimXplorerPluginWindow
    {
        public ModelCheck()
        {
            InitializeComponent();
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;
        /// <summary>
        /// Component's header text in the UI
        /// </summary>
        public string WindowTitle => "Check";

        /// <summary>
        /// All bindings are to be established in this call
        /// </summary>
        /// <param name="mainWindow"></param>
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            
            // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            


            TxtOut.AppendText(_parentWindow.GetOpenedModelFileName());
        }

        /// <summary>
        /// open spl file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
                TxtOut.AppendText(openFileDialog.FileName);
        }
    }
}
