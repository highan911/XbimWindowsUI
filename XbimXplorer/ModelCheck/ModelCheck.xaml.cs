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
using System.ComponentModel;
using Newtonsoft.Json;


//https://www.codeproject.com/Articles/28306/Working-with-Checkboxes-in-the-WPF-TreeView
//https://docs.microsoft.com/en-us/dotnet/framework/wpf/data/data-binding-overview#creating-a-binding

namespace XbimXplorer.ModelCheck
{
    /// <summary>
    /// ModelCheck.xaml 的交互逻辑
    /// </summary>
    /// 
    [XplorerUiElement(PluginWindowUiContainerEnum.LayoutAnchorable, PluginWindowActivation.OnMenu,
         "Check/模型检查")]
    public partial class ModelCheck : IXbimXplorerPluginWindow
    {
        public ModelCheck()
        {
            InitializeComponent();
            InitializeBackgroundWorker();
        }

        private IXbimXplorerPluginMasterWindow _parentWindow;
        /// <summary>
        /// Component's header text in the UI
        /// </summary>
        public string WindowTitle => "模型检查";
        public string SplPath;
        public Data_ResultJson data_ResultJson = null;
        private BackgroundWorker CheckWorker;

        private void InitializeBackgroundWorker()
        {
            CheckWorker = new BackgroundWorker();
            CheckWorker.DoWork += new DoWorkEventHandler(doCheck);
        }



        /// <summary>
        /// All bindings are to be established in this call
        /// </summary>
        /// <param name="mainWindow"></param>
        public void BindUi(IXbimXplorerPluginMasterWindow mainWindow)
        {
            _parentWindow = mainWindow;
            
            // whole datacontext binding, see http://stackoverflow.com/questions/8343928/how-can-i-create-a-binding-in-code-behind-that-doesnt-specify-a-path
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
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter="spl files|*.spl"};
            if (openFileDialog.ShowDialog() == true)
            {
                SplPath = openFileDialog.FileName;
                xmlparser(openFileDialog.FileName);
            }
        }


        private void btnXlsCheck_Click(object sender, RoutedEventArgs e)
        {
            //打开xls
            string xlsPath = null;
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Rules Files|*.xls;*.xlsx" };
            if (openFileDialog.ShowDialog() == true)
            {
                xlsPath = openFileDialog.FileName;
            }

            //parse xls
            if(xlsPath == null)
            {
                MessageBox.Show("规则文件为空", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            RuleXLS XlsToSNL = new RuleXLS(xlsPath);
            try
            {
                //储存SNL文件
                string SNLPath = Config_Global.DIR + "\\docs\\Baselinelibrary\\" + System.IO.Path.GetFileNameWithoutExtension(xlsPath)+".snl";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(SNLPath));
                CheckLog.Logger(SNLPath);
                XlsToSNL.xlsread();
                XlsToSNL.GenerateSNL();
                XlsToSNL.xml(SNLPath);

                //加载config文件
                string ConfigPath = Config_Global.DIR + "\\default_config.cfg";
                string config = File.ReadAllText(ConfigPath, Encoding.GetEncoding(1252));
                CheckLog.Logger(config);

                string ConfigNewPath = Config_Global.DIR + "\\docs\\Config\\" + System.IO.Path.GetFileNameWithoutExtension(xlsPath) + ".cfg";
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(ConfigNewPath));
                System.IO.File.WriteAllText(ConfigNewPath, config, Encoding.GetEncoding(1252));

            }
            catch(Exception except)
            {
                MessageBox.Show("规则文件解析错误", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                CheckLog.Logger("[error]" + "btnXlsCheck_Click" + except.Data);
                return;
            }

            try
            {
                Process process = new Process();
                String outputdir = Config_Global.DIR + "\\" +System.IO.Path.GetFileNameWithoutExtension(xlsPath) + ".spl";
                String filename = Config_Global.DIR + "\\docs\\Baselinelibrary\\" + System.IO.Path.GetFileNameWithoutExtension(xlsPath) + ".snl";

                CheckLog.Logger(outputdir);
                CheckLog.Logger(filename);

                process.StartInfo.FileName = Config_Global.DIR + "\\baseline.exe";
                process.StartInfo.Arguments = " -cmd -outdir " + outputdir + " -filename " + filename;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardInput = true;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.CreateNoWindow = true;





                process.Start();


                //* Read the output (or the error)
                string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
                string err = process.StandardError.ReadToEnd();

                process.WaitForExit();

                SplPath = outputdir;
                xmlparser(SplPath);

            }
            catch (Exception except)
            {
                MessageBox.Show("baseline调用错误", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                CheckLog.Logger("[error]" + "btnXlsCheck_Click, baseline" + except.Data);
                return;

            }

        }


        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outline)
        {
            if(outline.Data != null)
                ResultProcessing(outline.Data);
        }


        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            CheckWorker.RunWorkerAsync();
        }


        private void doCheck(object sender, DoWorkEventArgs e)
        {
            Process process = new Process();
            String datafrom = "ifc";
            String checkType = "ConsistencyCheck";
            String checkMode = "1";
            //String normPath = "E:\\1实验室工作\\SPLdoc\\rulechecker功能基准测试.spl";
            //String normSelector = "2.2.1;2.2.3";
            String modelPath = _parentWindow.GetOpenedModelFileName();
            if (modelPath == null)
            {
                MessageBox.Show("请载入模型文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            String normPath = SplPath;
            if (normPath == null)
            {
                MessageBox.Show("请载入规则文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            String normSelector = GetSelectedNorm();
            if (normSelector.Equals(""))
            {
                MessageBox.Show("请选择规则", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            process.StartInfo.FileName = Config_Global.DIR + "\\BC.exe";
            process.StartInfo.Arguments = " -datafrom " + datafrom + " -checktype " + checkType + " -checkmode " + checkMode + " -normpath " + "\"" + normPath + "\"" + " -normsel " + "\"" + normSelector + "\""; // Note the /c command (*)
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;


            process.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
            //process.ErrorDataReceived += new DataReceivedEventHandler(OutputHandler);



            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();


            StreamWriter myinput = process.StandardInput;
            myinput.WriteLine(modelPath);
            //myinput.WriteLine("E:\\1实验室工作\\SPLdoc\\AC20-Institute-Var-2.ifc");
            myinput.Close();
            process.WaitForExit();

        }
        

        private void showResult(Data_ResultJson ResultJson)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ResultSummary.Text = ResultJson.ReportInfo.ToSummaryString();
                List<string> itemKeyList = ResultJson.ItemResults.Keys.ToList();
                itemKeyList.Sort();

                foreach (string itemName in itemKeyList)
                {
                    ItemResultJsonData item = ResultJson.ItemResults[itemName];
                    PassStatus passStatus = PassStatus.FromString(item.PassStatus);
                    if (passStatus == PassStatus.PASS)
                    {
                        ResultRow newRow = new ResultRow() { Item = itemName, PassStatus = passStatus.ToString(), ErrorCount = "", ErrorType = "" };
                        ResultGrid.Items.Add(newRow);
                    }
                    else if (passStatus == PassStatus.NOTPASS)
                    {
                        foreach (TaskResultJsonData taskResult in item.CheckResults)
                        {
                            if (!taskResult.Pass)
                            {
                                ResultRow newRow = new ResultRow() { Item = itemName, PassStatus = passStatus.ToString(), ErrorCount = taskResult.errCateCount, ErrorType = taskResult.ErrorType };
                                ResultGrid.Items.Add(newRow);
                            }

                        }
                    }
                }
            }));
        }


        private void ResultProcessing(String result)
        {
            string aLine = null;
            StringReader strReader = new StringReader(result);
            while(true)
            {
                aLine = strReader.ReadLine();
                if(aLine != null)
                {
                    //Logger(aLine);
                    StdOutCmdLine cmdLine = StdOutCmdLine.FromString(aLine);
                    if(cmdLine != null)
                    {
                        if(CmdOutputTag.RESULT.Equals(cmdLine.Tag))
                        {
                            CheckLog.Logger(cmdLine.Data);
                            Data_ResultJson result_json = JsonConvert.DeserializeObject<Data_ResultJson>(cmdLine.Data);
                            if(result_json != null)
                            {
                                showResult(result_json);
                            }

                        }
                    }
                        
                }
                else
                {
                    break;
                }

            }

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

        
    }
}
