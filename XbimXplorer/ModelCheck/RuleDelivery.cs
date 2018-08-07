using ExcelDataReader;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace XbimXplorer.ModelCheck
{
    public enum RuleType { Property, Structure, Geometry };
    /// <summary>
    /// 条款详细信息
    /// </summary>
    public class RuleDetail
    {
        //编号
        public string No { get; set; }
        //实体名称
        public string Entity { get; set; }
        //实体IFD
        public string EntityIfd { get; set; }
        //属性、组成结构、几何
        public RuleType type { get; set; }
        //条款内容：对于属性是属性名称, 对于组成结构是ifd编码, 对于几何是要求几何
        public string content { get; set; }
        //条款描述
        public string Descript { get; set; }


        //检查一个条款的内容是否完整
        public bool isValidate()
        {
            if (string.IsNullOrWhiteSpace(Entity) || string.IsNullOrWhiteSpace(EntityIfd) || string.IsNullOrWhiteSpace(content))
                return false;
            return true;
        }

        /// <summary>
        /// 表格中的IFD通过http请求变成IFC
        /// </summary>
        public void IFDtoIFC()
        {
            try
            {
                //get请求
                string url = "http://cbims.org.cn/api/v1.0/IfdMatch/" + EntityIfd;
                WebRequest request = WebRequest.Create(url);
                WebResponse response = request.GetResponse();

                Stream dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();

                //处理数据
                dynamic res = JsonConvert.DeserializeObject(responseFromServer);
                string ifc = res.ifc;
                string regex = @"^[A-Z]+-";
                Regex rgx = new Regex(regex);
                string result = rgx.Replace(ifc, "");

                EntityIfd = result;


                //如果是structure类型的规则，更换内容的IFD到IFC
                if (type == RuleType.Structure)
                {
                    url = "http://cbims.org.cn/api/v1.0/IfdMatch/" + content;
                    request = WebRequest.Create(url);
                    response = request.GetResponse();

                    dataStream = response.GetResponseStream();
                    reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();

                    //处理数据
                    res = JsonConvert.DeserializeObject(responseFromServer);
                    ifc = res.ifc;
                    regex = @"^[A-Z]+-";
                    rgx = new Regex(regex);
                    result = rgx.Replace(ifc, "");

                    content = result;

                }



                reader.Close();
                response.Close();
            }
            catch (Exception ex)
            {
                //请求IFC失败
            }

        }

        public override string ToString()
        {
            return "编号: " + No + " " + " 实体: " + Entity + " 实体IFD: " + EntityIfd + " 类型: " + type + " 内容： " + content;
        }
    }

    /// <summary>
    /// 从excel交付表中获取规则信息
    /// </summary>
    public class RuleDelivery
    {
        IExcelDataReader excelInfo;
        string file;
        FileStream stream;

        public RuleDelivery(string filePath)
        {
            file = filePath;
            stream = File.Open(file, FileMode.Open, FileAccess.Read);
            
        }

        private void Initialize()
        {
            //var stream = File.Open(file, FileMode.Open, FileAccess.Read);
            excelInfo = ExcelReaderFactory.CreateOpenXmlReader(stream);
        }

        //返回List<RuleItem>，用作显示在检查工具中
        public List<RuleItem> parseDelivery()
        {
            Initialize();

            int firstClass = 0;
            int secondClass = 0;
            int thirdClass = 0;
            RuleItem curFirstRule = null;
            RuleItem curSecondRule = null;
            List<RuleItem> allRules = new List<RuleItem>();
            do
            {
                //对于每一行
                while (excelInfo.Read())
                {
                    //跳过第一列有值的行
                    if (excelInfo.GetValue(0) != null) continue;

                    //如果第二列有值
                    if (excelInfo.GetValue(1) != null)
                    {
                        firstClass++;
                        secondClass = 0;

                        string name = firstClass.ToString() + ':' + excelInfo.GetValue(1).ToString();
                        //Console.WriteLine(name);

                        curFirstRule = new RuleItem(name);
                        curFirstRule.Text = excelInfo.GetValue(1).ToString();
                        allRules.Add(curFirstRule);



                    }

                    //如果第三列有值
                    if (excelInfo.GetValue(2) != null)
                    {
                        secondClass++;
                        thirdClass = 0;

                        string name = firstClass.ToString() + '.' + secondClass.ToString() + ':' + excelInfo.GetValue(2).ToString();
                        curSecondRule = new RuleItem(name);
                        curSecondRule.Text = excelInfo.GetValue(2).ToString();

                        curFirstRule.Children.Add(curSecondRule);
                        //Console.WriteLine(name);

                    }

                    //如果第四列有值
                    if (excelInfo.GetValue(3) != null)
                    {
                        thirdClass++;
                        string name = firstClass.ToString() + '.' + secondClass.ToString() + '.' + thirdClass.ToString() + ':' + excelInfo.GetValue(3).ToString();
                        var thirdRule = new RuleItem(name);
                        curSecondRule.Children.Add(thirdRule);
                        //Console.WriteLine(name);
                    }


                }


            } while (excelInfo.NextResult());

            foreach (var rule in allRules)
                rule.Initialize();

            return allRules;
        }

        //返回List<RuleDetail>, 用作真正检查
        public List<RuleDetail> parseDeliveryDetail()
        {
            Initialize();

            int firstClass = 0;
            int secondClass = 0;
            int thirdClass = 0;
            RuleDetail curFirstRule = null;
            RuleDetail curSecondRule = null;
            List<RuleDetail> allRules = new List<RuleDetail>();
            do
            {
                //对于每一行
                while (excelInfo.Read())
                {
                    //跳过第一列有值的行
                    if (excelInfo.GetValue(0) != null) continue;

                    //如果第二列有值
                    if (excelInfo.GetValue(1) != null)
                    {
                        firstClass++;
                        secondClass = 0;

                        curFirstRule = new RuleDetail();
                        curFirstRule.Entity = excelInfo.GetValue(1).ToString();


                        try
                        {
                            curFirstRule.EntityIfd = excelInfo.GetValue(8).ToString();
                        }
                        catch (Exception ex)
                        {
                            //如果出现nullptr的exception，代表用户没有填写IFD

                        }

                    }

                    //如果第三列有值
                    if (excelInfo.GetValue(2) != null)
                    {
                        secondClass++;
                        thirdClass = 0;

                        string name = excelInfo.GetValue(2).ToString();
                        curSecondRule = new RuleDetail();

                        if (name == "属性")
                        {
                            curSecondRule.type = RuleType.Property;
                        }
                        else if (name == "组成结构")
                        {
                            curSecondRule.type = RuleType.Structure;
                        }
                        else if (name == "几何")
                        {
                            curSecondRule.type = RuleType.Geometry;
                        }
                        else
                        {
                            //规则类别错误
                            throw new Exception("规则类别错误");
                        }




                    }

                    //如果第四列有值
                    if (excelInfo.GetValue(3) != null)
                    {
                        thirdClass++;
                        string name = firstClass.ToString() + '.' + secondClass.ToString() + '.' + thirdClass.ToString() + ':' + excelInfo.GetValue(3).ToString();
                        var thirdRule = new RuleDetail();
                        //处理基本信息
                        thirdRule.No = firstClass.ToString() + '.' + secondClass.ToString() + '.' + thirdClass.ToString();
                        thirdRule.Entity = curFirstRule.Entity;
                        thirdRule.EntityIfd = curFirstRule.EntityIfd;
                        thirdRule.type = curSecondRule.type;
                        //根据类别处理内容
                        if (thirdRule.type == RuleType.Structure)
                        {
                            try
                            {
                                
                                thirdRule.content = excelInfo.GetValue(8).ToString();
                                thirdRule.Descript = thirdRule.Entity + "的组成结构包含" + excelInfo.GetValue(3).ToString();
                            }
                            catch
                            {
                                //用户没有写IFD编码
                            }
                        }
                        else
                        {

                            thirdRule.content = excelInfo.GetValue(3).ToString();
                            if (thirdRule.type == RuleType.Property)
                                thirdRule.Descript = thirdRule.Entity + "的属性包括" + thirdRule.content;
                            if (thirdRule.type == RuleType.Geometry)
                                thirdRule.Descript = thirdRule.Entity + "的几何表达形式为" + thirdRule.content;
                        }
                        CheckLog.Logger(thirdRule.ToString());
                        //把合理的Rule放入返回列表
                        if (thirdRule.isValidate())
                        {
                            
                            thirdRule.IFDtoIFC();
                            allRules.Add(thirdRule);
                        }

                    }


                }


            } while (excelInfo.NextResult());

            return allRules;
        }

    }
}