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
using Xbim.Common;
using Xbim.Ifc;
using Xbim.Ifc4.Interfaces;
using Xbim.ModelGeometry.Scene;


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

        //预检查所需数据
        private BackgroundWorker preCheckWorker;
        private List<RuleDetail> allRules;
        private List<RuleDetail> preRules;


        private void InitializeBackgroundWorker()
        {
            CheckWorker = new BackgroundWorker();
            CheckWorker.DoWork += new DoWorkEventHandler(doCheck);

            preCheckWorker = new BackgroundWorker();
            preCheckWorker.DoWork += new DoWorkEventHandler(doPreCheck);
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

        //旧版xls打开
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
                
                XlsToSNL.xlsread();
                XlsToSNL.GenerateSNL();
                XlsToSNL.xml(SNLPath);

                //加载config文件
                string ConfigPath = Config_Global.DIR + "\\default_config.cfg";
                string config = File.ReadAllText(ConfigPath, Encoding.GetEncoding(1252));
                

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

        /// <summary>
        /// 新版交付标准打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnXlsDelivery_Click(object sender, RoutedEventArgs e)
        {
            //打开xls
            string xlsPath = null;
            OpenFileDialog openFileDialog = new OpenFileDialog { Filter = "Rules Files|*.xls;*.xlsx" };
            if (openFileDialog.ShowDialog() == true)
            {
                xlsPath = openFileDialog.FileName;
            }

            //parse xls
            if (xlsPath == null)
            {
                //MessageBox.Show("规则文件为空", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RuleDelivery deliveryFile = new RuleDelivery(xlsPath);
            selectList.ItemsSource = deliveryFile.parseDelivery();
            allRules = deliveryFile.parseDeliveryDetail();

            



        }



        /// <summary>
        /// 通过详细规则生成需要过滤的IFC集合
        /// </summary>
        /// <returns></returns>
        private HashSet<string> GenerateIfcFromRules()
        {
            HashSet<string> targetSet = new HashSet<string>();
            foreach(var rule in preRules)
            {
                if(!targetSet.Contains(rule.EntityIfd))
                {
                    targetSet.Add(rule.EntityIfd);
                }
            }
            return targetSet;
        }



        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outline)
        {
            if(outline.Data != null)
                ResultProcessing(outline.Data);
        }


        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            preCheckWorker.RunWorkerAsync();
        }


        /// <summary>
        /// 根据选择的条款过滤preRules检查项
        /// </summary>
        /// <param name="normSelector">选中的条款</param>
        private void normFilter(string normSelector)
        {
            //normSelector = 1.1.1:客户姓名;1.1.2:项目状态;1.1.3:我不存在;1.1.4:NumberOfStoreys;1.1.5:CompositionType
            var normSelected = normSelector.Split(';');
            HashSet<string> normNums = new HashSet<string>();
            foreach(var norm in normSelected)
            {
                //过滤冒号以后的
                int indexOfComma = norm.IndexOf(':');
                normNums.Add(norm.Substring(0, indexOfComma));
            }
            preRules = allRules.Where<RuleDetail>(rule =>  normNums.Contains(rule.No) ).ToList();
        }

        /// <summary>
        /// 模型预检查
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void doPreCheck(object sender, DoWorkEventArgs e)
        {
            DateTime before = System.DateTime.Now;
            
            
            
           

            String modelPath = _parentWindow.GetOpenedModelFileName();
            if (modelPath == null)
            {
                MessageBox.Show("请载入模型文件", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            String normSelector = GetSelectedNorm();
            if (normSelector.Equals(""))
            {
                MessageBox.Show("请选择规则", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            normFilter(normSelector);






            Dispatcher.BeginInvoke(new Action(delegate
            {
                HashSet<string> TargetIfcSet = GenerateIfcFromRules();
                PropertyExtract extractHelper = new PropertyExtract(_parentWindow.Model, _parentWindow.GetContext());


                //通过规则生成中间文件
                IFCFile file = extractHelper.getFilterIfcProperty(TargetIfcSet);
                List<ResultRow> resultRows = new List<ResultRow>();
                foreach (var rule in preRules)
                {
                    resultRows.Add(CheckSingleRule(file, rule));
                }
                showResultForPreCheck(resultRows);

                //生成总体描述
                PreCheckReportInfo report = new PreCheckReportInfo(resultRows);
                ResultSummary.Text = report.GenerateSummary();


            }));

            DateTime after = System.DateTime.Now;
            TimeSpan ts = after.Subtract(before);

            CheckLog.Logger("[precheck时间]" + ts);




        }



        /// <summary>
        /// 判断某个构件是否包含（包括structurecontain+aggregate两种关系）某种IFC
        /// </summary>
        /// <param name="entityLabel">被判断的构件的entitylabel</param>
        /// <param name="ifcContained">被包含的IFC类型</param>
        /// <param name="relContain">包含关系</param>
        /// <returns></returns>
        public bool judgeRelContainsHelper(int entityLabel, string ifcContained, SortedDictionary<int, List<int>> relContain, IfcStore model)
        {
            //所选实体不在relContain中，返回false
            if (!relContain.ContainsKey(entityLabel)) return false;
            //如果在relContain中，递归查找
            var list = relContain[entityLabel];
            foreach (var entity in list)
            {
                if (model.Instances[entity].ExpressType.ExpressNameUpper == ifcContained.ToUpper())
                    return true;
                if (judgeRelContainsHelper(entity, ifcContained, relContain, model))
                    return true;
            }
            return false;
        }


        /// <summary>
        /// 检查一个规则
        /// </summary>
        /// <param name="file">过滤后的IFC文件信息</param>
        /// <param name="rule">某个规则</param>
        private ResultRow CheckSingleRule(IFCFile file, RuleDetail rule)
        {
            IfcStore model = _parentWindow.Model;

            //获取检查的实体是哪一个
            var targetEntities = file.Elements.Where(i => i.TYPE.ToUpper() == rule.EntityIfd.ToUpper());

            //bool result = true;
            int ErrorCount = 0;

            ResultRow Result = new ResultRow();
            Result.Item = rule.No;
            Result.PassStatus = "通过";
            Result.ErrorEntityLabels = new List<int>();
            Result.ItemContent = rule.Descript;
            Result.EntityCheckCount = targetEntities.Count().ToString();


            //如果是属性检查
            if (rule.type == RuleType.Property)
            {
                Result.ErrorType = "属性检查";
                foreach(var Entity in targetEntities)
                {
                    //存在实体并不含有这个属性或值为空
                    if(!Entity.properties.ContainsKey(rule.content) || string.IsNullOrWhiteSpace(Entity.properties[rule.content]))
                    {
                        Result.PassStatus = "不通过";
                        ErrorCount++;
                        Result.ErrorEntityLabels.Add(Entity.LABEL);
                    }
                }

            }
            else if(rule.type == RuleType.Geometry)
            {
                Result.ErrorType = "几何检查";
                foreach (var Entity in targetEntities)
                {
                    //几何不对
                    if(!Entity.properties["Representation"].ToUpper().Contains(rule.content.ToUpper()))
                    {
                        Result.PassStatus = "不通过";
                        ErrorCount++;
                        Result.ErrorEntityLabels.Add(Entity.LABEL);
                    }
                }
            }
            else if(rule.type == RuleType.Structure)
            {
                Result.ErrorType = "空间检查";
                SortedDictionary<int, List<int>> relContain = file.Rels["isContaining"];
                //空间检查需要注意Ifcwall的多种形式
                if(rule.content.ToUpper() == "IFCWALL" || rule.content.ToUpper() == "IFCWALLSTANDARDCASE" || rule.content.ToUpper() == "IFCWALLELMENTEDCASE")
                {
                    foreach (var Entity in targetEntities)
                    {
                        if (!judgeRelContainsHelper(Entity.LABEL, "IFCWALL", relContain, model) && !judgeRelContainsHelper(Entity.LABEL, "IFCWALLSTANDARDCASE", relContain, model) && !judgeRelContainsHelper(Entity.LABEL, "IFCWALLELMENTEDCASE", relContain, model))
                        {
                            Result.PassStatus = "不通过";
                            ErrorCount++;
                            Result.ErrorEntityLabels.Add(Entity.LABEL);
                        }

                    }
                } else
                {
                    foreach (var Entity in targetEntities)
                    {
                        if (!judgeRelContainsHelper(Entity.LABEL, rule.content, relContain, model))
                        {
                            Result.PassStatus = "不通过";
                            ErrorCount++;
                            Result.ErrorEntityLabels.Add(Entity.LABEL);
                        }

                    }
                }

            }
            Result.ErrorCount = ErrorCount.ToString();
            return Result;
        }


        private void doCheck(object sender, DoWorkEventArgs e)
        {
            DateTime before = System.DateTime.Now;

            StopProgressBarAnimation(true);

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

            DateTime after = System.DateTime.Now;

            TimeSpan ts = after.Subtract(before);

            CheckLog.Logger("[check时间]" + ts);

        }
        
        /// <summary>
        /// 显示预检查的结果：总体+表格
        /// </summary>
        /// <param name="results">对于所有条款的检查结果</param>
        private void showResultForPreCheck(List<ResultRow> results)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                ResultGrid.Items.Clear();
                foreach(var Row in results)
                {
                    ResultGrid.Items.Add(Row);
                }              
            }));
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
                        ReportProgress(cmdLine.Tag);
                        if(CmdOutputTag.RESULT.Equals(cmdLine.Tag))
                        {
                            StopProgressBarAnimation(false);
                            ReportProgress(cmdLine.Tag);
                            
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

        private void ReportProgress(string text)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _parentWindow.ReportCheckProgress(text);
            }));

        }

        private void StopProgressBarAnimation(bool Stop)
        {
            Dispatcher.BeginInvoke(new Action(delegate
            {
                _parentWindow.SetProgressBar(Stop);
            }));
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

        private void btnExtract_Click(object sender, RoutedEventArgs e)
        {
            DateTime before = System.DateTime.Now;
            getProperties();
            DateTime after = System.DateTime.Now;
            TimeSpan ts = after.Subtract(before);
            CheckLog.Logger("[info]" + "时间统计" + ts);
        }

        //导出模型数据
        private void getProperties()
        {
            
            IfcStore curModel = _parentWindow.Model;
            Xbim3DModelContext context = _parentWindow.GetContext();
            PropertyExtract extractTool = new PropertyExtract(curModel, context);

            //CheckLog.Logger(curModel.Instances.CountOf<IIfcProduct>().ToString());

            IFCFile fileProperties = extractTool.getAllIfcProperty();
            string str = JsonConvert.SerializeObject(fileProperties);
            CheckLog.Logger("[info]" + str);

        }

    }
}
