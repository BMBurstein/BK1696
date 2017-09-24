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

        private bool SendCommand(string command, bool retry = true)
        {
            try
            {
                using (SerialPort port = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One) { NewLine = "\r", ReadTimeout = 100, WriteTimeout = 100 })
                {
                    port.Open();

                    if (port.IsOpen)
                    {
                        port.WriteLine(command);
                        if (port.ReadLine() == "OK")
                        {
                            return true;
                        }
                        else
                        {
                            trayIcon.Icon = gray;
                            trayIcon.ShowBalloonTip(3000, "Error", "Command failed", ToolTipIcon.Error);
                            return false;
                        }
                    }
                    else
                    {
                        trayIcon.Icon = gray;
                        trayIcon.ShowBalloonTip(3000, "Error", "Could not open COM port", ToolTipIcon.Error);
                        return false;
                    }
                }
            }
            catch(TimeoutException)
            {
                if(retry)
                {
                    return SendCommand(command, false);
                }
                trayIcon.Icon = gray;
                trayIcon.ShowBalloonTip(3000, "Error", "Timeout", ToolTipIcon.Error);
                return false;
            }
        }

        private void TurnOff(object sender, EventArgs e)
        {
            if (SendCommand("SOUT001"))
                trayIcon.Icon = red;
        }

        private void TurnOn(object sender, EventArgs e)
        {
            if (SendCommand("SOUT000"))
                trayIcon.Icon = green;
        }

        private void Lock(object sender, EventArgs e)
        {
            SendCommand("SESS00");
        }

        private void Unlock(object sender, EventArgs e)
        {
            SendCommand("ENDS00");
        }

        private void SetVoltage(object sender, EventArgs e)
        {
            SendCommand("VOLT00120");
        }
    }
}
