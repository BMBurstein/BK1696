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

        private decimal Val { get; set; } = 0;

        public NumInputBox(NumInputType t, decimal init, decimal max)
        {
            InitializeComponent();
            switch (t)
            {
                case NumInputType.VOLT:
                    this.Text = "Voltage";
                    this.lblUnit.Text = "(V)";
                    this.numChooser.Minimum = 1;
                    this.numChooser.DecimalPlaces = 1;
                    this.numChooser.Increment = decimal.One / 10;
                    this.Icon = Properties.Resources.Disaster;
                    break;
                case NumInputType.CURR:
                    this.Text = "Current";
                    this.lblUnit.Text = "(A)";
                    this.numChooser.Minimum = decimal.One / 100;
                    this.numChooser.DecimalPlaces = 2;
                    this.numChooser.Increment = decimal.One / 100;
                    this.Icon = Properties.Resources.Lightning;
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
