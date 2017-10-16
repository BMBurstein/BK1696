using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BK1696
{
    class TrayApplicationContext : ApplicationContext
    {
        private static readonly Icon gray = Properties.Resources.gray;
        private static readonly Icon green = Properties.Resources.green;
        private static readonly Icon red = Properties.Resources.red;

        private NotifyIcon trayIcon = new NotifyIcon()
        {
            Icon = gray,
            Visible = true,
            Text="BK1696 control",
            ContextMenuStrip = new ContextMenuStrip()
        };

        private AboutForm about;

        public TrayApplicationContext()
        {
            Lock(null, null);
            ThreadExit += TrayApplicationContext_ThreadExit;
#if DEBUG
            trayIcon.ContextMenuStrip.Items.Add("DEBUG").Enabled = false;
#endif
            trayIcon.ContextMenuStrip.Items.Add("Set to 12V", Properties.Resources.Disaster, SetVoltage);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("About", Properties.Resources.Info, ShowAbout);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Lock", Properties.Resources.Lock, Lock);
            trayIcon.ContextMenuStrip.Items.Add("Unlock", Properties.Resources.Unlock, Unlock);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Turn on", green.ToBitmap(), TurnOn);
            trayIcon.ContextMenuStrip.Items.Add("Turn off", red.ToBitmap(), TurnOff);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Exit", Properties.Resources.Exit, Exit_Click);
            trayIcon.MouseClick += TrayIcon_MouseClick;

            if (GetState())
            {
                trayIcon.Icon = green;
            }
            else
            {
                trayIcon.Icon = red;
            }
        }

        private void TrayApplicationContext_ThreadExit(object sender, EventArgs e)
        {
            Unlock(sender, e);
            trayIcon.Icon = null;
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            if(about == null)
            {
                about = new AboutForm();
                about.FormClosed += delegate { about = null; };
                about.Show();
            }
            about.BringToFront();
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            if (about != null) { about.Close(); };
            ExitThread();
        }

        private void TrayIcon_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (trayIcon.Icon == green)
                {
                    TurnOff(sender, e);
                }
                else
                {
                    TurnOn(sender, e);
                }
            }
        }

        private string SendCommand(string command, int retry = 1)
        {
            try
            {
                using (SerialPort port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One) { NewLine = "\r", ReadTimeout = 500, WriteTimeout = 500 })
                {
                    port.Open();

                    if (port.IsOpen)
                    {
                        port.WriteLine(command);
                        string resp = port.ReadLine();
                        if (resp != "OK" && port.ReadLine() != "OK")
                        {
                            trayIcon.ShowBalloonTip(3000, "Error", "Command failed", ToolTipIcon.Error);
                        }
                        else
                        {
                            return resp;
                        }
                    }
                    else
                    {
                        trayIcon.ShowBalloonTip(3000, "Error", "Could not open COM port", ToolTipIcon.Error);
                    }
                }
            }
            catch(Exception ex) when (ex is TimeoutException || ex is UnauthorizedAccessException)
            {
                if (retry > 0)
                {
                    return SendCommand(command, retry - 1);
                }
                if (ex is TimeoutException)
                {
                    trayIcon.ShowBalloonTip(3000, "Error", "Timeout", ToolTipIcon.Error);
                }
                else
                {
                    trayIcon.ShowBalloonTip(3000, "Error", "Could not open port", ToolTipIcon.Error);
                }
            }
            trayIcon.Icon = gray;
            return null;
        }

        private bool SendSimpleCommand(string command)
        {
            return SendCommand(command) == "OK";
        }

        private void TurnOff(object sender, EventArgs e)
        {
            if (SendSimpleCommand("SOUT001"))
                trayIcon.Icon = red;
        }

        private void TurnOn(object sender, EventArgs e)
        {
            if (SendSimpleCommand("SOUT000"))
                trayIcon.Icon = green;
        }

        private void Lock(object sender, EventArgs e)
        {
            SendSimpleCommand("SESS00");
        }

        private void Unlock(object sender, EventArgs e)
        {
            SendSimpleCommand("ENDS00");
        }

        private void SetVoltage(object sender, EventArgs e)
        {
            SendSimpleCommand("VOLT00120");
        }

        private bool GetState()
        {
            string resp = SendCommand("GPAL00");
            return resp?[65] == '0';
        }
    }
}
