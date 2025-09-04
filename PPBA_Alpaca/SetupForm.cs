using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PPBA_Alpaca
{
    public partial class SetupForm : Form
    {
        public string SelectedPort { get; private set; }

        public SetupForm()
        {
            // Populate dropdown with available COM ports
            // Save selection to Settings or ASCOM Profile
        }


    }
}
