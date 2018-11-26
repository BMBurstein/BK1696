using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BK1696
{
    public partial class NumInputBox : Form
    {
        public enum NumInputType
        {
            VOLT,
            CURR
        }

        public static decimal GetVal(NumInputType t, decimal init, decimal max)
        {
            NumInputBox box = new NumInputBox(t, init, max);
            box.ShowDialog();

            return box.Val;
        }

        private decimal Val { get; set; }

        public NumInputBox(NumInputType t, decimal init, decimal max)
        {
            InitializeComponent();
            switch (t)
            {
                case NumInputType.VOLT:
                    Text = "Voltage";
                    lblUnit.Text = "(V)";
                    numChooser.Minimum = 1;
                    numChooser.DecimalPlaces = 1;
                    numChooser.Increment = decimal.One / 10;
                    break;
                case NumInputType.CURR:
                    Text = "Current";
                    lblUnit.Text = "(A)";
                    numChooser.Minimum = decimal.One / 100;
                    numChooser.DecimalPlaces = 2;
                    numChooser.Increment = decimal.One / 100;
                    break;
            }
            numChooser.Value = init;
            numChooser.Maximum = max;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            Val = numChooser.Value;
            Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Val = 0;
            Close();
        }
    }
}
