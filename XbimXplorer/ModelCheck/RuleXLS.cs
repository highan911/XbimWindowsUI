using System;
using System.Collections.Generic;
using System.Xml;
using ExcelDataReader;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.ModelCheck
{
    public class Klass
    {

        public int Id;
        public string text;

        public Klass(string text, int id)
        {
            Id = id;
            this.text = text;
        }

        public override string ToString()
        {
            return Id + " " + text;
        }
    }

    public class Rule
    {
        public Klass classA;
        public Klass classB;
        public Klass classC;
        public bool IsMandatory;
        public string SNL;
        public Rule(Klass A, Klass B, Klass C, string SNL, bool IsMandatory)
        {
            classA = A;
            classB = B;
            classC = C;
            this.SNL = SNL;
            this.IsMandatory = IsMandatory;
        }
        public string toString()
        {
            return classA + " " + classB + " " + classC + " " + SNL + " " + IsMandatory;
        }
        public string getNum()
        {
            return classA.Id + "." + classB.Id + "." + classC.Id;
        }
        public string getSec()
        {
            return classA.Id + "," + classB.Id;
        }
    }

    public class RuleXLS
    {
        /// <summary>
        /// 三级分类在xls表格中的列数，SNL信息在第7列
        /// </summary>
        static int classA = 1;
        static int classB = 2;
        static int classC = 3;
        static int Mandatory = 4;
        static int SNL = 7;
        static int headerRowCount = 6;

        public string fileName = null;

        IExcelDataReader excelInfo;
        List<Rule> ruleSet = new List<Rule>();


        public RuleXLS(string filePath)
        {
            fileName = Path.GetFileNameWithoutExtension(filePath);
            var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            excelInfo = ExcelReaderFactory.CreateReader(stream);
            int row = 0;
            while (excelInfo.Read())
            {
                row++;
                if (row == headerRowCount) break;
            }

        }

        public void GenerateSNL()
        {
            foreach (Rule rule in ruleSet)
            {
                //需要生成SNL的条件是：1.isMandatory = true 2.ClassB 是 “组成结构” 或者 “具有属性”
                if (rule.IsMandatory && (rule.classB.text.Contains("组成结构") || rule.classB.text.Contains("具有属性") || rule.classB.text.Contains("几何表达")))
                {
                    //生成组成结构的SNL
                    if (rule.classB.text.Contains("组成结构"))
                    {
                        string sub = rule.classA.text;
                        string obj = rule.classC.text;
                        string SNL = "所有 " + sub + " 有 " + obj;
                        rule.SNL = SNL;
                    }

                    if (rule.classB.text.Contains("具有属性"))
                    {
                        string sub = rule.classA.text;
                        string obj = rule.classC.text;
                        string SNL = "所有 " + sub + " 有属性 " + obj;
                        rule.SNL = SNL;
                    }

                    if (rule.classB.text.Contains("几何表达"))
                    {
                        string sub = rule.classA.text;
                        rule.SNL = "所有 " + sub + " 有属性 Representation";
                    }


                    //Console.WriteLine(rule.toString());
                }
                //Console.WriteLine(rule.toString());
            }
        }

        public void show()
        {
            foreach (var rule in ruleSet)
            {
                Console.WriteLine(rule.toString());
            }
        }

        public void xml(string SplPath)
        {
            HashSet<string> Aset = new HashSet<string>();
            HashSet<string> Bset = new HashSet<string>();




            //根节点rule_lib
            XmlDocument doc = new XmlDocument();
            XmlElement rulelib = doc.CreateElement("rule_lib");
            doc.AppendChild(rulelib);

            XmlDeclaration xmldec = doc.CreateXmlDeclaration("1.0", "utf-8", "yes");
            doc.InsertBefore(xmldec, rulelib);

            //rule_lib下的lib_name
            XmlElement libname = doc.CreateElement("lib_name");
            libname.InnerText = fileName ?? "Sample";
            rulelib.AppendChild(libname);



            //rule_lib下的lib_info, ontology
            XmlElement libinfo = doc.CreateElement("lib_info");
            rulelib.AppendChild(libinfo);
            XmlElement ontology = doc.CreateElement("ontology");
            rulelib.AppendChild(ontology);

            //rule_lib下的class_set
            XmlElement classset = doc.CreateElement("class_set");
            rulelib.AppendChild(classset);


            //class_set下面必须有0；0，0
            XmlElement rootclass = doc.CreateElement("class");
            XmlAttribute rootclassattr = doc.CreateAttribute("id");
            rootclassattr.Value = "0";
            rootclass.Attributes.Append(rootclassattr);

            XmlElement rootclassname = doc.CreateElement("class_name");
            rootclassname.InnerText = "所有规则";
            XmlElement rootclassnum = doc.CreateElement("class_number");
            rootclass.AppendChild(rootclassname);
            rootclass.AppendChild(rootclassnum);
            classset.AppendChild(rootclass);

            XmlElement rootclass2 = doc.CreateElement("class");
            XmlAttribute rootclassattr2 = doc.CreateAttribute("id");
            rootclassattr2.Value = "0,0";
            rootclass2.Attributes.Append(rootclassattr2);

            XmlElement rootclassname2 = doc.CreateElement("class_name");
            rootclassname2.InnerText = "未分类";
            XmlElement rootclassnum2 = doc.CreateElement("class_number");
            rootclass2.AppendChild(rootclassname2);
            rootclass2.AppendChild(rootclassnum2);
            classset.AppendChild(rootclass2);



            //rule_lib下的resource_set
            XmlElement resource = doc.CreateElement("resource_set");
            rulelib.AppendChild(resource);


            XmlElement ruleset = doc.CreateElement("rule_set");
            rulelib.AppendChild(ruleset);
            XmlElement ruleintro = doc.CreateElement("rule_introduction");
            ruleset.AppendChild(ruleintro);

            XmlElement fmlset = doc.CreateElement("fml_descpt_set");
            rulelib.AppendChild(fmlset);

            XmlElement descptset = doc.CreateElement("temporal_descpt_set");
            rulelib.AppendChild(descptset);

            XmlElement noteset = doc.CreateElement("note_set");
            rulelib.AppendChild(noteset);

            XmlElement selected_rules = doc.CreateElement("selected_rules");
            rulelib.AppendChild(selected_rules);

            int id = 1;
            foreach (var arule in ruleSet)
            {
                if (!Aset.Contains(arule.classA.Id.ToString()))
                {
                    Aset.Add(arule.classA.Id.ToString());

                    XmlElement klass = doc.CreateElement("class");

                    XmlElement klassname = doc.CreateElement("class_name");
                    klassname.InnerText = arule.classA.text;
                    klass.AppendChild(klassname);

                    XmlElement klassnumber = doc.CreateElement("class_number");
                    klassnumber.InnerText = arule.classA.Id.ToString();
                    klass.AppendChild(klassnumber);

                    XmlAttribute klassid = doc.CreateAttribute("id");
                    klassid.Value = "0," + arule.classA.Id;
                    klass.Attributes.Append(klassid);

                    classset.AppendChild(klass);
                }

                if (!Bset.Contains(arule.getSec()))
                {
                    Bset.Add(arule.getSec());

                    XmlElement klass = doc.CreateElement("class");

                    XmlElement klassname = doc.CreateElement("class_name");
                    klassname.InnerText = arule.classB.text;
                    klass.AppendChild(klassname);

                    XmlElement klassnumber = doc.CreateElement("class_number");
                    klassnumber.InnerText = arule.classA.Id + "." + arule.classB.Id;
                    klass.AppendChild(klassnumber);

                    XmlAttribute klassid = doc.CreateAttribute("id");
                    klassid.Value = "0," + arule.getSec();
                    klass.Attributes.Append(klassid);

                    classset.AppendChild(klass);
                }


                XmlNode rule = doc.CreateElement("rule");
                XmlAttribute attr_id = doc.CreateAttribute("id");
                attr_id.Value = id.ToString();
                rule.Attributes.Append(attr_id);

                XmlNode whichclass = doc.CreateElement("whichclass");
                whichclass.InnerText = "0," + arule.classA.Id + "," + arule.classB.Id;
                rule.AppendChild(whichclass);

                XmlNode rulenumber = doc.CreateElement("rule_number");
                rulenumber.InnerText = arule.getNum();
                rule.AppendChild(rulenumber);

                XmlNode res = doc.CreateElement("resourceName");
                rule.AppendChild(res);

                XmlNode rulegroup = doc.CreateElement("rule_group");
                rule.AppendChild(rulegroup);


                XmlNode snl = doc.CreateElement("rule_nat_descpt");
                snl.InnerText = arule.SNL;
                rule.AppendChild(snl);

                XmlNode anot = doc.CreateElement("rule_anot_descpt");
                rule.AppendChild(anot);

                ruleset.AppendChild(rule);

                XmlElement fmlnode = doc.CreateElement("fml_descpt");
                XmlAttribute attr_id2 = doc.CreateAttribute("id");
                attr_id2.Value = id.ToString();
                fmlnode.Attributes.Append(attr_id2);

                XmlElement ruleid = doc.CreateElement("ruleId");
                ruleid.InnerText = id.ToString();
                fmlnode.AppendChild(ruleid);

                XmlElement dessnl = doc.CreateElement("descpt_snl");
                dessnl.InnerText = arule.SNL;
                fmlnode.AppendChild(dessnl);

                fmlset.AppendChild(fmlnode);




                id++;
            }

            doc.Save(SplPath);

        }



        public void xlsread()
        {
            Klass curClassA = null;
            Klass curClassB = null;
            Klass curClassC = null;
            int A = 1;
            int B = 1;
            int C = 1;
            string curSNL = "";
            bool curMandatory = false;
            do
            {
                while (excelInfo.Read())
                {

                    //如果第0列有数据，什么都不做
                    if (excelInfo.GetValue(0) != null)
                        continue;

                    //如果第1列有数据，当前第一级别设为这个数据
                    if (excelInfo.GetValue(classA) != null)
                    {
                        curClassA = new Klass(excelInfo.GetValue(classA).ToString(), A++);
                        B = 1;
                        continue;
                    }

                    if (excelInfo.GetValue(classB) != null)
                    {
                        curClassB = new Klass(excelInfo.GetValue(classB).ToString(), B++);
                        C = 1;
                    }




                    curClassC = new Klass(excelInfo.GetValue(classC).ToString(), C++);


                    curSNL = (excelInfo.GetValue(SNL) == null) ? null : excelInfo.GetValue(SNL).ToString();

                    if (excelInfo.GetValue(Mandatory) != null && excelInfo.GetValue(Mandatory).ToString().Contains("必须"))
                    {
                        //Console.WriteLine(excelInfo.GetValue(Mandatory).ToString());
                        curMandatory = true;
                    }
                    else
                    {
                        curMandatory = false;
                    }

                    //只有classB是“具有属性”或者“组成结构”的才放入规则
                    if (curClassB.text.Contains("组成结构") || curClassB.text.Contains("具有属性") || curClassB.text.Contains("几何表达"))
                    {
                        Rule newRule = new Rule(curClassA, curClassB, curClassC, curSNL, curMandatory);
                        ruleSet.Add(newRule);
                    }


                }
            } while (excelInfo.NextResult());
        }
    }
}
