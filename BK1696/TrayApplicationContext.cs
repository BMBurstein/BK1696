using Microsoft.Win32;
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
        private NotifyIcon trayIcon = new NotifyIcon()
        {
            Icon = Properties.Resources.gray,
            Visible = true,
            Text = "BK1696 control",
            ContextMenuStrip = new ContextMenuStrip()
        };

        private AboutForm about;
        private string portName;
        private bool isOn = false;
        private ToolStripMenuItem runAtStartupMenuItem;
        private ToolStripItem setVoltageMenuItem;
        private ToolStripItem setCurrentMenuItem;

        public TrayApplicationContext()
        {
            var portMenu = new ToolStripMenuItem("COM port", Properties.Resources.Network_connection);
            ToolStripItem activePort = null;
            foreach (var port in SerialPort.GetPortNames())
            {
                var newPort = portMenu.DropDownItems.Add(port, null, SetPort);
                portName = port;
                if (SendCommand("SESS00", -1) != null) // lock command
                {
                    activePort = newPort;
                }
            }
            if (activePort != null)
            {
                SetPort(activePort, null);
            }

            ThreadExit += TrayApplicationContext_ThreadExit;
#if DEBUG
            trayIcon.ContextMenuStrip.Items.Add("DEBUG").Enabled = false;
#endif

            trayIcon.ContextMenuStrip.Items.Add(portMenu);
            trayIcon.ContextMenuStrip.Items.Add("-");
            runAtStartupMenuItem = (ToolStripMenuItem)trayIcon.ContextMenuStrip.Items.Add("Run at startup", null, SetStartup);
            trayIcon.ContextMenuStrip.Items.Add("-");
            setVoltageMenuItem = trayIcon.ContextMenuStrip.Items.Add("Set voltage", Properties.Resources.Disaster.ToBitmap(), SetVoltage);
            setCurrentMenuItem = trayIcon.ContextMenuStrip.Items.Add("Set current", Properties.Resources.Lightning.ToBitmap(), SetCurrent);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Lock", Properties.Resources.Lock, Lock);
            trayIcon.ContextMenuStrip.Items.Add("Unlock", Properties.Resources.Unlock, Unlock);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Refresh status", Properties.Resources.Refresh, UpdateState);
            trayIcon.ContextMenuStrip.Items.Add("Turn on", Properties.Resources.green.ToBitmap(), TurnOn);
            trayIcon.ContextMenuStrip.Items.Add("Turn off", Properties.Resources.red.ToBitmap(), TurnOff);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("About", Properties.Resources.Info, ShowAbout);
            trayIcon.ContextMenuStrip.Items.Add("-");
            trayIcon.ContextMenuStrip.Items.Add("Exit", Properties.Resources.Exit, Exit_Click);
            trayIcon.MouseClick += TrayIcon_MouseClick;

            UpdateState(null, null);
        }

        private void SetStartup(object sender, EventArgs e)
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true)) {
                if (runAtStartupMenuItem.Checked)
                {
                    key.DeleteValue(Application.ProductName, false);
                    runAtStartupMenuItem.Checked = false;
                }
                else
                {
                    key.SetValue(Application.ProductName, "\"" + Application.ExecutablePath + "\"");
                    runAtStartupMenuItem.Checked = true;
                }
            }
        }

        private void UpdateState(object sender, EventArgs e)
        {
            isOn = GetState();
            trayIcon.Icon = isOn ? Properties.Resources.green : Properties.Resources.red;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true))
            {
                runAtStartupMenuItem.Checked = key.GetValue(Application.ProductName) != null;
            }

            string resp = SendCommand("GETS00");
            if (resp != null)
            {
                var v = ExtractV(resp);
                var c = ExtractC(resp);
                SetVoltageText(v);
                SetCurrentText(c);
            }
        }

        private void TrayApplicationContext_ThreadExit(object sender, EventArgs e)
        {
            Unlock(sender, e);
            trayIcon.Icon = null;
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            if (about == null)
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
                if (isOn)
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
                if (portName != null)
                {
                    using (SerialPort port = new SerialPort(portName, 9600, Parity.None, 8, StopBits.One) { NewLine = "\r", ReadTimeout = 500, WriteTimeout = 500 })
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
            }
            catch (Exception ex) when (ex is TimeoutException || ex is UnauthorizedAccessException)
            {
                if (retry > 0)
                {
                    return SendCommand(command, retry - 1);
                }
                if (retry == 0)
                {
                    if (ex is TimeoutException)
                    {
                        trayIcon.ShowBalloonTip(3000, "Error", "Timeout", ToolTipIcon.Error);
                    }
                    else
                    {
                        trayIcon.ShowBalloonTip(3000, "Error", "Could not open port", ToolTipIcon.Error);
                    }
                }
            }
            trayIcon.Icon = Properties.Resources.gray;
            isOn = false;
            return null;
        }

        private bool SendSimpleCommand(string command)
        {
            return SendCommand(command) == "OK";
        }

        private void TurnOff(object sender, EventArgs e)
        {
            if (SendSimpleCommand("SOUT001")) {
                trayIcon.Icon = Properties.Resources.red;
                isOn = false;
            }
        }

        private void TurnOn(object sender, EventArgs e)
        {
            if (SendSimpleCommand("SOUT000"))
            {
                trayIcon.Icon = Properties.Resources.green;
                isOn = true;
            }
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
            string resp = SendCommand("GETS00");
            var v = ExtractV(resp);
            resp = SendCommand("GMAX00");
            var m = ExtractV(resp);
            v = NumInputBox.GetVal(NumInputBox.NumInputType.VOLT, v, m);
            if (v == 0) return;
            SetVoltageText(v);
            v *= 10;
            SendSimpleCommand("VOLT00" + v.ToString("000"));
        }

        private void SetVoltageText(decimal v)
        {
            setVoltageMenuItem.Text = $"Set voltage ({v:F1})";
        }

        private void SetCurrent(object sender, EventArgs e)
        {
            string resp = SendCommand("GETS00");
            var c = ExtractC(resp);
            resp = SendCommand("GMAX00");
            var m = ExtractC(resp);
            c = NumInputBox.GetVal(NumInputBox.NumInputType.CURR, c, m);
            if (c == 0) return;
            SetCurrentText(c);
            c *= 100;
            SendSimpleCommand("CURR00" + c.ToString("000"));
        }

        private void SetCurrentText(decimal c)
        {
            setCurrentMenuItem.Text = $"Set current ({c:F2})";
        }

        private void SetPort(object sender, EventArgs e)
        {
            var item = (ToolStripMenuItem)sender;
            foreach (ToolStripMenuItem it in ((ToolStripMenuItem)item.OwnerItem).DropDownItems)
            {
                if (it == item)
                    it.Checked = true;
                else
                    it.Checked = false;
            }
            portName = item.Text;
        }

        private bool GetState()
        {
            string resp = SendCommand("GPAL00");
            return resp?[65] == '0';
        }

        private decimal ExtractV(string vc)
        {
            return decimal.Parse(vc.Substring(0, 3)) / 10;
        }

        private decimal ExtractC(string vc)
        {
            return decimal.Parse(vc.Substring(3, 3)) / 100;
        }
    }
}
