using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace ResHCK
{
    public class ResInterceptor
    {
        public static void Write(byte[] data)
        {
            string cDir = Environment.CurrentDirectory;
            File.WriteAllBytes(cDir + "\\lgmstResUNPACKED.dll", data);
        }
    }
}
