using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XbimXplorer.ModelCheck
{
    class Config_Global
    {
        public static string runtime_path = typeof(ModelCheck).Assembly.Location;
        public static string DIR = System.IO.Path.GetDirectoryName(runtime_path);
    }
}
