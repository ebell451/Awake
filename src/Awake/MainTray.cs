using Awake.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Awake
{
    public partial class MainTray : Form
    {
        private int _idleTime = 0;
        private bool _IsAutoStart = false;
        private volatile bool _IsLock = false;
        private volatile bool _IsDarkOnLock = false;

        private RegistryKey _regPath;
        private RegistryKey _regSettings;

        private List<SleepState> _states = new List<SleepState>();

        public MainTray()
        {
            InitializeComponent();
            var version = typeof(MainTray).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (version == null)
                throw new InvalidOperationException("The assembly informational version is not defined.");
            this.versionToolStripMenuItem.Text = "Version " + version.InformationalVersion;

            try
            {
                _regPath = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
                _regSettings = Registry.CurrentUser.CreateSubKey("SOFTWARE\\" + Application.ProductName + "\\" + Application.ProductVersion);
            }
            catch
            {
            }

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            _states.Add(new SleepState(3, this.minutes15ToolStripMenuItem, Resources.CrazyEye_Sleepy));
            _states.Add(new SleepState(30, this.minutes30ToolStripMenuItem, Resources.CrazyEye_Sleepy));
            _states.Add(new SleepState(45, this.minutes45ToolStripMenuItem, Resources.CrazyEye_Sleepy));
            _states.Add(new SleepState(60, this.hour1ToolStripMenuItem, Resources.CrazyEye_Sleepy));
            _states.Add(new SleepState(120, this.hours2ToolStripMenuItem, Resources.CrazyEye_Sleepy));
            _states.Add(new SleepState(0, this.disabledToolStripMenuItem, Resources.CrazyEye_Sleep));
            _states.Add(new SleepState(-1, this.foreverToolStripMenuItem, Resources.CrazyEye_Open));

            CleanUpRunConfig();

            LoadApplicationState();
        }

        private void CleanUpRunConfig()
        {
            if (_regPath == null)
                return;

            try
            {
                //cleanup on update from insomnia.exe to awake.exe
                if (_regPath.GetValue("Insomnia") != null)
                {
                    string path = _regPath.GetValue("Insomnia", "").ToString();
                    if (!string.IsNullOrEmpty(path) && !File.Exists(path))
                    {
                        _regPath.DeleteValue("Insomnia", false);
                    }
                }
                //cleanup on executable move
                if (_regPath.GetValue("AwakeDonGeatschOS") != null)
                {
                    string path = _regPath.GetValue("AwakeDonGeatschOS", "").ToString();

                    if (path == Application.ExecutablePath.ToString())
                        _IsAutoStart = true;

                    if (!string.IsNullOrEmpty(path) && (!File.Exists(path) || path != Application.ExecutablePath.ToString()))
                    {
                        _regPath.DeleteValue("AwakeDonGeatschOS", false);
                    }
                }
            }
            catch
            {
            }
        }

        private void ClearChecked()
        {
            foreach (var item in _states)
            {
                item.MenuItem.Checked = false;
            }
        }

        private void LoadApplicationState()
        {
            try
            {
                if (_regPath == null)
                    throw new InvalidOperationException("Registry startup settings are unavailable.");

                if (_regPath.GetValue("AwakeDonGeatschOS") == null)
                {
                    startWithWindowsToolStripMenuItem.Checked = false;
                }
                else
                {
                    startWithWindowsToolStripMenuItem.Checked = true;
                }
            }
            catch
            {
                startWithWindowsToolStripMenuItem.Checked = false;
            }
            try
            {
                ClearChecked();

                if (_regSettings == null)
                    throw new InvalidOperationException("Registry application settings are unavailable.");

                int period = (int)_regSettings.GetValue("Period", -1);

                var state = _states.FirstOrDefault(s => s.Period == period);
                if (state == null)
                    throw new InvalidOperationException("The saved sleep state is invalid.");

                notico_tray.Icon = state.Icon;
                state.MenuItem.Checked = true;
            }
            catch
            {
                ClearChecked();
                notico_tray.Icon = Resources.CrazyEye_Open;
                foreverToolStripMenuItem.Checked = true;
            }

            try
            {
                disableOnLockScreenToolStripMenuItem.Checked = ((int)_regSettings.GetValue("DeactivateOnLock", 0)) == 0 ? false : true;
                monitorOffOnLockScreenToolStripMenuItem.Checked = ((int)_regSettings.GetValue("ForceBlackScreenOnLock", 0)) == 0 ? false : true;

            }
            catch
            {
            }
        }

        private void SaveApplicationState()
        {
            if (_regPath != null)
            {
            if (startWithWindowsToolStripMenuItem.Checked)
            {
                _regPath.SetValue("AwakeDonGeatschOS", Application.ExecutablePath.ToString());
            }
            else
            {
                _regPath.DeleteValue("AwakeDonGeatschOS", false);
            }
            }

            var state = _states.FirstOrDefault(s => s.MenuItem.Checked);
            if (state == null)
                return;

            notico_tray.Icon = state.Icon;

            if (_regSettings == null)
                return;

            _regSettings.SetValue("Period", state.Period, RegistryValueKind.DWord);

            try
            {
                _regSettings.SetValue("DeactivateOnLock", disableOnLockScreenToolStripMenuItem.Checked ? 1 : 0, RegistryValueKind.DWord);
                _regSettings.SetValue("ForceBlackScreenOnLock", monitorOffOnLockScreenToolStripMenuItem.Checked ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
            }
        }

        private void SetIdleTime()
        {
            Win32Wrapper.LASTINPUTINFO LastInputInfo = new Win32Wrapper.LASTINPUTINFO();
            LastInputInfo.cbSize = (uint)Marshal.SizeOf(LastInputInfo);
            LastInputInfo.dwTime = 0;

            if (Win32Wrapper.GetLastInputInfo(ref LastInputInfo))
            {
                uint systemUptime = unchecked((uint)Environment.TickCount);
                _idleTime = AwakePolicy.CalculateIdleMinutes(systemUptime, LastInputInfo.dwTime);
                return;
            }

            _idleTime = 1;
        }

        private void SetState()
        {
            SetIdleTime();

            var state = _states.FirstOrDefault(s => s.MenuItem.Checked);
            if (state == null)
                return;

            if (AwakePolicy.ShouldDeactivate(state.Period, _idleTime, _IsLock, disableOnLockScreenToolStripMenuItem.Checked))
            {
                Win32Wrapper.SetThreadExecutionState(Win32Wrapper.ES_CONTINUOUS);
            }
            else
            {
                Win32Wrapper.SetThreadExecutionState(Win32Wrapper.ES_CONTINUOUS | Win32Wrapper.ES_DISPLAY_REQUIRED | Win32Wrapper.ES_SYSTEM_REQUIRED);
            }

            if (_IsLock && monitorOffOnLockScreenToolStripMenuItem.Checked && !_IsDarkOnLock)
            {
                Win32Wrapper.SendMessage(this.Handle, Win32Wrapper.WM_SYSCOMMAND, (IntPtr)Win32Wrapper.SC_MONITORPOWER, (IntPtr)2);
                _IsDarkOnLock = true;
            }
        }

        #region Form Events

        private void continousTick_Tick(object sender, EventArgs e)
        {
            SetState();
        }

        private void httpawakecodeplexcomToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/ebell451/Awake");
        }

        private void MainTray_Load(object sender, EventArgs e)
        {
            this.Visible = false;
            this.ShowInTaskbar = false;

            if (!_IsAutoStart)
                this.notico_tray.ShowBalloonTip(3000, "Awake", "Right click to configure.", ToolTipIcon.Info);
        }

        private void mi_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void saveStateMenuItem_Click(object sender, EventArgs e)
        {
            SaveApplicationState();

        }

        private void toolStripMenuItem_Click(object sender, EventArgs e)
        {
            ClearChecked();
            ToolStripMenuItem source = sender as ToolStripMenuItem;
            source.Checked = true;

            SaveApplicationState();
        }

        void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                _IsLock = true;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                _IsLock = false;
            }
            _IsDarkOnLock = false;
        }

        #endregion Form Events
    }

}