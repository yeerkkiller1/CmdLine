using CmdLine;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CmdLineTest2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            for (int ix = 0; ix < 10; ix++)
            {
                CommandLine cmd = new CommandLine();
                cmd.Run("ls");
            }
        }
    }
}
