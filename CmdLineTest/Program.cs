using CmdLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CmdLineTest
{
    class Program
    {
        static void Main(string[] args)
        {
            for (int ix = 0; ix < 10; ix++)
            {
                CommandLine cmd = new CommandLine(false);
                cmd.Run("ls");
            }
        }
    }
}
