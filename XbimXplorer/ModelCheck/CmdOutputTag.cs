using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace XbimXplorer.ModelCheck
{
    public class CmdOutputTag
    {
        private string _key;


        private CmdOutputTag(string _key)
        {
            this._key = _key;
        }

        public new string ToString()
        {
            return this._key;
        }

        public new bool Equals(Object obj)
        {
            if (obj is String)
            {
                return _key.Equals((String)obj);
            }
            else if (obj is CmdOutputTag)
            {
                return _key.Equals(((CmdOutputTag)obj)._key);
            }
            return false;
        }

        public static readonly CmdOutputTag START = new CmdOutputTag("START");
        public static readonly CmdOutputTag START_PREPARE = new CmdOutputTag("START_PREPARE");
        public static readonly CmdOutputTag END_PREPARE = new CmdOutputTag("END_PREPARE");
        public static readonly CmdOutputTag START_CHECK_VALID = new CmdOutputTag("START_CHECK_VALID");
        public static readonly CmdOutputTag END_CHECK_VALID = new CmdOutputTag("END_CHECK_VALID");
        public static readonly CmdOutputTag START_CHECK_CONSIS = new CmdOutputTag("START_CHECK_CONSIS");
        public static readonly CmdOutputTag END_CHECK_CONSIS = new CmdOutputTag("END_CHECK_CONSIS");
        public static readonly CmdOutputTag START_ITEM = new CmdOutputTag("START_ITEM");
        public static readonly CmdOutputTag END_ITEM = new CmdOutputTag("END_ITEM");
        public static readonly CmdOutputTag START_SUMMARY = new CmdOutputTag("START_SUMMARY");
        public static readonly CmdOutputTag END_SUMMARY = new CmdOutputTag("END_SUMMARY");

        public static readonly CmdOutputTag RESULT = new CmdOutputTag("RESULT");
        public static readonly CmdOutputTag ERROR = new CmdOutputTag("ERROR");
        public static readonly CmdOutputTag WARN = new CmdOutputTag("WARN");

        public static readonly CmdOutputTag END = new CmdOutputTag("END");
    }


    public class StdOutCmdLine
    {
        
        public const int FIX_LENGTH = 4;
        public const string PREFIX = "[>>]";
        public const string TO_BE_CONTINUED_PREFIX = "[+>]";

        public string Tag;
        public string Data;
        public string Time;


        public StdOutCmdLine() { }

        public StdOutCmdLine(CmdOutputTag tag, string data, string Time)
        {
            this.Tag = tag.ToString();
            this.Time = Time;
            this.Data = data;
        }

        public static StdOutCmdLine FromString(string line)
        {
            //CheckLog.Logger(line);
            //if (line.Length > FIX_LENGTH) CheckLog.Logger("length more than 4");
            //if (line.Substring(0, FIX_LENGTH).Equals(PREFIX)) CheckLog.Logger("is [>>]");
            if (line.Length > FIX_LENGTH && line.Substring(0, FIX_LENGTH).Equals(PREFIX))
            {
                string jsonData = line.Substring(FIX_LENGTH);

                
                StdOutCmdLine data = JsonConvert.DeserializeObject<StdOutCmdLine>(jsonData);



                return data;
            }
            return null;
        }
    }

    public class CmdAsyncRecever
    {
        public enum CmdAsyncReceverResult
        {
            OUT,
            WAIT,
            ERROR
        }


        string nowRecord = "";

        public CmdAsyncReceverResult InputLine(string inputstr, out string receivedData)
        {
            receivedData = "";
            try
            {
                if (inputstr.Length > StdOutCmdLine.FIX_LENGTH && inputstr.Substring(0, StdOutCmdLine.FIX_LENGTH).Equals(StdOutCmdLine.TO_BE_CONTINUED_PREFIX))
                {
                    nowRecord += inputstr.Substring(StdOutCmdLine.FIX_LENGTH);
                    receivedData = null;
                    return CmdAsyncReceverResult.WAIT;
                }
                nowRecord += inputstr;
                receivedData = nowRecord;
                nowRecord = "";
                return CmdAsyncReceverResult.OUT;
            }
            catch (Exception ex)
            {
                
                receivedData = null;
                return CmdAsyncReceverResult.ERROR;
            }
        }
    }


}

