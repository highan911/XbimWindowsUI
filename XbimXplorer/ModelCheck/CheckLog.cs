using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.ModelCheck
{
    class CheckLog
    {

        public static void Logger(String lines)
        {

            // Write the string to a file.append mode is enabled so that the log
            // lines get appended to  test.txt than wiping content and writing the log

            System.IO.StreamWriter file = new System.IO.StreamWriter("E:\\test.txt", true);
            file.WriteLine(lines);

            file.Close();

        }
    }
}
