using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.ModelCheck
{

    public class ResultRow
    {
        public string Item { get; set; }
        public string PassStatus { get; set; }
        public string ErrorType { get; set; }
        public string ErrorCount { get; set; }
    }

    public class ItemResultJsonData
    {

        public string Item;
        public string PassStatus;
        public string NaturalLanguage;//记录自然语言描述
        public string Undotodo;
        public List<TaskResultJsonData> CheckResults = new List<TaskResultJsonData>();

    }


    public class TaskResultJsonData
    {
        public string TaskType;
        public bool Pass;
        public string ErrorType;
        public string Reason;
        public List<string> IdSet;
        public string errCateCount;

        public string DislayString()
        {
            return ErrorType + " " + Reason + " " + errCateCount;
        }

    }

    public class ReportInfo
    {
        public int totalNum = 0;//记录检查的所有规范的总条数
        public int passNum = 0;//通过的条款的数目
        public int notPassNum = 0;//未通过的条款的数目
        public int notPassCompNum = 0;//未通过的“构件缺失”或“构件错误”的条款数目
        public int notPassPropNum = 0;//未通过的“属性缺失”或“硬性错误”的条款数目
        public int notReadyNum = 0;//未编写的条款的数目
        public int notPassElementNum = 0;//未通过检查的构件的数目

        //zhh 20160720 添加完整性和一致性检查的分类统计信息
        public bool isTotalCheck = false;   //记录是否为综合检查，以决定是否显示分类统计信息
        public bool isConsisCheck = false;
        public bool isValidCheck = false;

        public int valid_totalNum = 0; //记录完整性检查的所有规范的总条数
        public int valid_passNum = 0;  //通过完整性检查的条款的数目
        public int valid_notPassNum = 0;//未通过完整性检查的条款的数目
        public int valid_notPassCompNum = 0;
        public int valid_notPassPropNum = 0;
        public int valid_notReadyNum = 0;//未编写的条款的数目
        public int valid_notPassElementNum = 0;//未通过完整性检查检查的构件的数目
        public int valid_compMissNum = 0;

        public int consistency_totalNum = 0; //记录完整性检查的所有规范的总条数
        public int consistency_passNum = 0;  //通过完整性检查的条款的数目
        public int consistency_notPassNum = 0;//未通过完整性检查的条款的数目
        public int consistency_notPassCompNum = 0;
        public int consistency_notPassPropNum = 0;
        public int consistency_notReadyNum = 0;//未编写的条款的数目
        public int consistency_notPassElementNum = 0;//未通过完整性检查检查的构件的数目

        public int compMissNum = 0;//zhangrui 20161110 错误类型只有构件缺失的条款数

        public string ToSummaryString()
        {
            //zhh 20160718 完善检查结果统计


            String returnStr = "";
            if (!isTotalCheck)
            {
                int total = totalNum - notReadyNum;
                Double passRatio = (double)passNum / (double)(total);
                Double notpassRatio = (double)(notPassNum + compMissNum) / (double)(total);

                returnStr = "规范共包含条款" + totalNum + "条；自动检查条款" + total + "条；其中模型适用的条款" + total + "条。\r\n";
                returnStr += "通过率 " + string.Format("{0:0%}", passRatio) + " (" + passNum + "条/" + total + "条 )；";
                returnStr += "不通过率 " + string.Format("{0:0%}", notpassRatio) + " (" + (notPassNum + compMissNum) + "条/" + total + "条 )；";
                // if (compMissNum > 0) {
                // returnStr += "不适用条款 " + string.Format("{0:0%}", notSuitRatio) + " (" +
                // compMissNum + "条/" + total +"条 )；";
                // }
                returnStr += "不符合要求的构件总数目为" + notPassElementNum + "个。\r\n";

                if (isConsisCheck)
                {
                    returnStr += "\r\n-----------------检查结果分类统计-----------------\r\n";
                    returnStr += "4硬性错误：" + resultSummary(notPassPropNum, total, -1);
                    returnStr += "3构件错误：" + resultSummary(notPassCompNum, total, -1);
                }
                else if (isValidCheck)
                {
                    returnStr += "\r\n-----------------检查结果分类统计-----------------\r\n";
                    returnStr += "2属性缺失：" + resultSummary(notPassPropNum, total, -1);
                    returnStr += "1构件缺失：" + resultSummary(notPassCompNum + compMissNum, total, -1);
                }
            }
            //zhh 20160830 完整性和一致性的含义不明确，一些实体的缺失是在一致性检查出来的。
            //因此将这个细分暂时注掉。
            //zhh 20160720 添加：如果是综合检查，则添加分类统计信息。

            else if (isTotalCheck == true)
            {

                int total = totalNum - notReadyNum;
                int suit = total - compMissNum;
                Double passRatio = (double)passNum / (double)(suit);
                Double notpassRatio = (double)notPassNum / (double)(suit);
                //			Double notSuitRatio = (double) compMissNum / (double) (total);

                returnStr = "规范共包含条款" + totalNum + "条；自动检查条款" + total + "条；其中模型适用的条款" + suit + "条。\r\n";
                returnStr += "通过率 " + string.Format("{0:0%}", passRatio) + " (" + passNum + "条/" + suit + "条 )；";
                returnStr += "不通过率 " + string.Format("{0:0%}", notpassRatio) + " (" + notPassNum + "条/" + suit + "条 )；";
                returnStr += "不符合要求的构件总数目为" + notPassElementNum + "个。\r\n";

                int validNum = valid_totalNum - valid_notReadyNum - compMissNum;
                int consisNum = consistency_totalNum - consistency_notReadyNum - compMissNum;

                Double valid_passRatio = (double)valid_passNum / (double)validNum;
                Double valid_notpassRatio = (double)(valid_notPassNum + valid_compMissNum - compMissNum) / (double)validNum;

                returnStr += "---BIM模型信息完整性 \r\n";
                returnStr += "通过率 " + string.Format("{0:0%}", valid_passRatio) + " (" + valid_passNum + "条/" + validNum + "条 )；";
                returnStr += "不通过率 " + string.Format("{0:0%}", valid_notpassRatio) + " (" + (valid_notPassNum + valid_compMissNum - compMissNum) + "条/" + validNum + "条 )；";
                returnStr += "不符合要求的构件总数目为" + valid_notPassElementNum + "个。\r\n";

                Double consistency_passRatio = (double)(consistency_passNum - compMissNum) / (double)consisNum;
                Double consistency_notpassRatio = (double)consistency_notPassNum / (double)consisNum;
                returnStr += "---BIM模型规范符合性\r\n";
                returnStr += "通过率 " + string.Format("{0:0%}", consistency_passRatio) + " (" + (consistency_passNum - compMissNum) + "条/" + consisNum + "条 )；";
                returnStr += "不通过率 " + string.Format("{0:0%}", consistency_notpassRatio) + " (" + consistency_notPassNum + "条/" + consisNum + "条 )；";
                returnStr += "不符合要求的构件总数目为" + consistency_notPassElementNum + "个。\r\n";

                returnStr += "\r\n-----------------检查结果分类统计-----------------\r\n";
                returnStr += "4硬性错误：通过率" + resultSummary(consistency_notPassPropNum, consisNum, -1);
                returnStr += "3构件错误：通过率" + resultSummary(consistency_notPassCompNum, consisNum, -1);
                returnStr += "2属性缺失：通过率" + resultSummary(valid_notPassPropNum, validNum, -1);
                returnStr += "1构件缺失：通过率" + resultSummary(valid_notPassCompNum + valid_compMissNum - compMissNum, validNum, -1);

            }


            return returnStr;
            //end zhh
            //			return "total number of rules:"+ totalNum + ".  pass: " + passNum + "  failed: " + notPassNum + 
            //					"  undo: " + notReadyNum + "  the number of entities that failed to pass the rule is: " + notPassElementNum + ". ";
        }

        //zhangrui 20161025 把结果统计输出封装成函数，totalNum表示去掉notready条款之后的条款总数；
        public string resultSummary(int notPassNum, int totalNum, int eleNum)
        {
            String retStr = "";

            int passNum = totalNum - notPassNum;
            Double passRatio = (double)passNum / (double)totalNum;
            Double notPassRatio = (double)notPassNum / (double)totalNum;

            retStr += "通过率 " + string.Format("{0:0%}", passRatio) + " (" + passNum + "条/" + totalNum + "条 )；";
            retStr += "不通过率 " + string.Format("{0:0%}", notPassRatio) + " (" + notPassNum + "条/" + totalNum + "条 )";

            //分类信息统计暂时无法统计构件个数，所以传入-1
            if (eleNum >= 0)
                retStr += "；不符合要求的构件总数目为" + eleNum + "个。\r\n";
            else
                retStr += "。\r\n";

            return retStr;
        }


    }



    public class PassStatus
    {
        private string _key;
        private PassStatus(string _key)
        {
            this._key = _key;
        }

        public new string ToString()
        {
            return this._key;
        }

        public new bool Equals(Object obj)
        {
            if (obj is string)
            {
                return _key.Equals((string)obj);
            }
            else if (obj is PassStatus)
            {
                return _key.Equals(((PassStatus)obj)._key);
            }
            return false;
        }

        public static readonly PassStatus PASS = new PassStatus("pass");
        public static readonly PassStatus NOTPASS = new PassStatus("not pass");
        public static readonly PassStatus NOTREADY = new PassStatus("not ready");
        public static readonly PassStatus RUNERROR = new PassStatus("run error");

        public static PassStatus FromString(string passStatus)
        {
            switch (passStatus)
            {
                case "pass":
                    return PASS;
                case "not pass":
                    return NOTPASS;
                case "not ready":
                    return NOTREADY;
                case "run error":
                    return RUNERROR;
            }

            
            return null;
        }

    }

    public class ErrorType
    {
        private string _key;
        private ErrorType(string _key)
        {
            this._key = _key;
        }

        public new string ToString()
        {
            return this._key;
        }

        public new bool Equals(Object obj)
        {
            if (obj is string)
            {
                return _key.Equals((string)obj);
            }
            else if (obj is ErrorType)
            {
                return _key.Equals(((ErrorType)obj)._key);
            }
            return false;
        }
        public static readonly ErrorType PropErr_4 = new ErrorType("4硬性错误");
        public static readonly ErrorType ElemErr_3 = new ErrorType("3构件错误");
        public static readonly ErrorType PropMissing_2 = new ErrorType("2属性缺失");
        public static readonly ErrorType ElemMissing_1 = new ErrorType("1构件缺失");

        public static ErrorType FromString(string error)
        {
            switch (error)
            {
                case "1构件缺失":
                    return ElemMissing_1;
                case "2属性缺失":
                    return PropMissing_2;
                case "3构件错误":
                    return ElemErr_3;
                case "4硬性错误":
                    return PropErr_4;
            }

            
            return null;
        }

    }



    public class Data_ResultJson
    {
        public ReportInfo ReportInfo;
        public Dictionary<String, ItemResultJsonData> ItemResults = new Dictionary<String, ItemResultJsonData>();
    }
}
