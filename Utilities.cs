using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KafkaProducerToFabric
{
    internal class Utilities
    {
        public static void DumpJsonOutput(string JSONresult)
        {
            /*
             *  Dump the json output on local machine for review
             */

            string path = @".\jsonDumps\" + DateTime.Now.ToString("yyyyMMdd_THHmm");
            string FilePath = path + @"\router" + DateTime.Now.ToString("yyyyMMdd_THHmmss_fffffff") + ".json";

            // If directory does not exist, create it.  
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
                CreateJsonFile(JSONresult, FilePath);
            }
            else if (!File.Exists(FilePath))
            {
                CreateJsonFile(JSONresult, FilePath);
            }
        }

        private static void CreateJsonFile(string JSONresult, string FilePath)
        {
            using (var tw = new StreamWriter(FilePath, true))
            {
                tw.WriteLine(JSONresult.ToString());
                tw.Close();
            }
        }
    }
}
