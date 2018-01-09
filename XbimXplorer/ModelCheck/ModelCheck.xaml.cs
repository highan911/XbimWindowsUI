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
using System.Xml;


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

            treedata();

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
            {
                xmlparser(openFileDialog.FileName);
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            List<RuleItem> Selected = new List<RuleItem>();

            foreach(var tree in (List<RuleItem>)selectList.ItemsSource)
            {
                foreach(var subtree in tree.Children)
                {
                    foreach(var item in subtree.Children)
                    {
                        if(item.IsChecked == true)
                        {
                            Selected.Add(item);
                        }
                    }
                }
            }

            foreach(var item in Selected)
            {
                TxtOut.AppendText(item.Name);
            }


        }

        public void xmlparser(String path)
        {
            XmlDocument spldoc = new XmlDocument();
            spldoc.PreserveWhitespace = false;
            spldoc.Load(path);

            XmlNode root = spldoc.DocumentElement;
            XmlNodeList allcategory = root.SelectNodes("CATEGORY");

            List<RuleItem> TreeViewList = new List<RuleItem>();

            foreach (XmlNode category in allcategory)
            {
                Console.WriteLine(category.Attributes["name"].Value);
                RuleItem RuleRoot = new RuleItem(category.Attributes["name"].Value);

                foreach (XmlNode subcategory in category.ChildNodes)
                {
                    Console.WriteLine(subcategory.Attributes["name"].Value);
                    RuleItem asubcategory = new RuleItem(subcategory.Attributes["name"].Value);
                    foreach (XmlNode item in subcategory.ChildNodes)
                    {
                        RuleItem arule = new RuleItem(item.Attributes["name"].Value);
                        asubcategory.Children.Add(arule);
                        Console.WriteLine(item.Attributes["name"].Value);
                    }
                    RuleRoot.Children.Add(asubcategory);
                }

                RuleRoot.Initialize();
                TreeViewList.Add(RuleRoot);

            }

            selectList.ItemsSource = TreeViewList;
        }

        private void treedata()
        {
            RuleItem root = new RuleItem("Weapons")
            {
                IsInitiallySelected = true,
                Children =
                {
                    new RuleItem("Blades")
                    {
                        Children =
                        {
                            new RuleItem("Dagger"),
                            new RuleItem("Machete"),
                            new RuleItem("Sword"),
                        }
                    },
                    new RuleItem("Vehicles")
                    {
                        Children =
                        {
                            new RuleItem("Apache Helicopter"),
                            new RuleItem("Submarine"),
                            new RuleItem("Tank"),
                        }
                    },
                    new RuleItem("Guns")
                    {
                        Children =
                        {
                            new RuleItem("AK 47"),
                            new RuleItem("Beretta"),
                            new RuleItem("Uzi"),
                        }
                    },
                }
            };

            root.Initialize();
            selectList.ItemsSource = new List<RuleItem> { root };
        }
    }
}
