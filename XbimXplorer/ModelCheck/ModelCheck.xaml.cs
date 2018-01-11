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
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


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
        public string SplPath;



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

        private void item_dbClick(object sender, RoutedEventArgs e)
        {
            var menuItem = selectList.SelectedItem as RuleItem;

            if (menuItem != null)
            {
                RuleContent.Text = menuItem.Text;
            }
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
                SplPath = openFileDialog.FileName;
                xmlparser(openFileDialog.FileName);
            }
        }

        private void btnTest_Click(object sender, RoutedEventArgs e)
        {
            String ts = GetSelectedNorm();
            TxtOut.AppendText(ts); 
        }

        public void Logger(String lines)
        {

            // Write the string to a file.append mode is enabled so that the log
            // lines get appended to  test.txt than wiping content and writing the log

            System.IO.StreamWriter file = new System.IO.StreamWriter("E:\\test.txt", true);
            file.WriteLine(lines);

            file.Close();

        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outline)
        {
            Logger(outline.Data);
            
            //this.RuleContent.Dispatcher.Invoke(new Action(delegate
            //{
            //    this.RuleContent.Text = outline.Data;
            //}));
        }


        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            Process process = new Process();
            String datafrom = "ifc";
            String checkType = "ConsistencyCheck";
            String checkMode = "1";
            String normSelector = GetSelectedNorm();
            String normPath = SplPath;
            String modelPath = _parentWindow.GetOpenedModelFileName();
            
            process.StartInfo.FileName = "E:\\BC.exe";
            process.StartInfo.Arguments = " -datafrom " + datafrom + " -checktype " + checkType + " -checkmode " + checkMode + " -normpath " + "\"" +normPath +"\"" +" -normsel " +"\"" +normSelector + "\""; // Note the /c command (*)
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;


            //process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            //process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);

            

            process.Start();
            //process.BeginOutputReadLine();
            //process.BeginErrorReadLine();


            StreamWriter myinput = process.StandardInput;
            myinput.WriteLine(modelPath);
            myinput.Close();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            //Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            //Console.WriteLine(err);
            TxtOut.AppendText(output);
            TxtOut.AppendText(err);

            process.WaitForExit();

        }

        private String GetSelectedNorm()
        {
            List<RuleItem> Selected = new List<RuleItem>();

            foreach (var tree in (List<RuleItem>)selectList.ItemsSource)
            {
                foreach (var subtree in tree.Children)
                {
                    foreach (var item in subtree.Children)
                    {
                        if (item.IsChecked == true)
                        {
                            Selected.Add(item);
                        }
                    }
                }
            }
            String norms = "";

            for(int i = 0; i < Selected.Count; i++)
            {
                if(i == Selected.Count - 1)
                {
                    norms = norms + Selected[i].Name;
                } else
                {
                    norms = norms + Selected[i].Name + ";";
                }
            }

            return norms;
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
                //Console.WriteLine(category.Attributes["name"].Value);
                RuleItem RuleRoot = new RuleItem(category.Attributes["name"].Value);

                foreach (XmlNode subcategory in category.ChildNodes)
                {
                    //Console.WriteLine(subcategory.Attributes["name"].Value);
                    RuleItem asubcategory = new RuleItem(subcategory.Attributes["name"].Value);
                    foreach (XmlNode item in subcategory.ChildNodes)
                    {
                        RuleItem arule = new RuleItem(item.Attributes["name"].Value);
                        arule.Text = item.Attributes["text"].Value;
                        asubcategory.Children.Add(arule);
                        //Console.WriteLine(item.Attributes["name"].Value);
                    }
                    RuleRoot.Children.Add(asubcategory);
                }

                RuleRoot.Initialize();
                TreeViewList.Add(RuleRoot);

            }

            selectList.ItemsSource = TreeViewList;
        }

        public void RunCheck()
        {
            Process process = new Process();
            String datafrom = "ifc";
            String checkType = "ConsistencyCheck";
            String checkMode = "1";
            String normSelector = "2.2.1;2.2.3";
            String normPath = SplPath;
            process.StartInfo.FileName = "F:\\VS2015Projects\\ConsoleApplication1\\ConsoleApplication1\\BC.exe";
            process.StartInfo.Arguments = "/c -datafrom " + datafrom + " -checkType " + checkType + " -checkMode " + checkMode + " -normPath " + normPath + " -normSelectedStr " + normSelector; // Note the /c command (*)
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.Start();

            StreamWriter myinput = process.StandardInput;
            myinput.WriteLine("E:\\1实验室工作\\SPLdoc\\AC20-Institute-Var-2.ifc");
            myinput.Close();
            //* Read the output (or the error)
            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine(output);
            string err = process.StandardError.ReadToEnd();
            Console.WriteLine(err);
            process.WaitForExit();
        }
        
    }
}
