using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Cursed_Market
{
    public partial class Form_Crosshair : Form
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);


        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);


        // Delegate for the hook callback function
        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);




        protected override CreateParams CreateParams
        {
            get
            {
                // Retrieve the base window creation parameters.
                CreateParams createParams = base.CreateParams;

                // The ExStyle property sets the extended window styles.
                // Here we enable several flags using the bitwise OR operator:
                //
                // 0x20  - WS_EX_TRANSPARENT:
                //         Makes the window transparent to mouse events, meaning that clicks pass through the window.
                //
                // 0x80  - WS_EX_TOOLWINDOW:
                //         Designates the window as a tool window, which prevents it from appearing on the taskbar.
                //
                // 0x80000  - WS_EX_LAYERED:
                //         Allows the use of transparency effects and alpha blending for the window.
                //
                // 0x08000000  - WS_EX_NOACTIVATE:
                //         Prevents the window from being activated when clicked, i.e., it does not gain focus.
                createParams.ExStyle |= 0x20 | 0x80 | 0x80000 | 0x08000000;


                return createParams;
            }
        }




        // Global hook variables
        private IntPtr _hookID = IntPtr.Zero;
        private LowLevelMouseProc _proc;

        // Constants for hook and mouse event
        private const int WH_MOUSE_LL = 14;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_RBUTTONUP = 0x205;

        // Background workers for mouse hook and timer
        private BackgroundWorker bgwHook;




        // Set the global mouse hook
        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }
        // Global hook callback function
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                if (this.IsHandleCreated && this.Handle != IntPtr.Zero)
                {
                    if (wParam == (IntPtr)WM_RBUTTONDOWN)
                    {
                        this.BeginInvoke((Action)(() => this.Opacity = (float)Globals.Crosshair.opacity / 100));
                    }
                    else if (wParam == (IntPtr)WM_RBUTTONUP)
                    {
                        this.BeginInvoke((Action)(() => this.Opacity = 0));
                    }
                }
            }


            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }




        // Background worker method for global mouse hook
        private void bgwHook_DoWork(object sender, DoWorkEventArgs e)
        {
            // Set the global hook
            _hookID = SetHook(_proc);
            // Start a message loop to keep the hook active
            Application.Run();
        }




        private void InitializeBackgroundWorkers()
        {
            // Initialize background worker for global mouse hook
            bgwHook = new BackgroundWorker();
            bgwHook.WorkerSupportsCancellation = true;
            bgwHook.DoWork += bgwHook_DoWork;
        }




        public Form_Crosshair()
        {
            InitializeComponent();
            InitializeSettings();
            if (Globals.Application.startupArguments.Contains(Globals.Application.SE_CommonStartupArguments.crosshairToggleFeature))
            {
                InitializeBackgroundWorkers();
                bgwHook.RunWorkerAsync();
            }
                

            // Initialize mouse hook delegate
            _proc = HookCallback;


            this.Width = Screen.PrimaryScreen.Bounds.Width;   // Get user screen size and apply it to Crosshair Form width.
            this.Height = Screen.PrimaryScreen.Bounds.Height; // Get user screen size and apply it to Crosshair Form height.
        }
        private void Form_Crosshair_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false; // We need to disable Safety Measures in order to call user32.dll.
            SetStyle(ControlStyles.SupportsTransparentBackColor, true); // Enables transparent background support for our Form.
        }
        private void Form_Crosshair_Shown(object sender, EventArgs e) // Update crosshair settings each time it's initially displayed on screen.
        {
            ForceInitializeSettings();
        }
        private void Crosshair_FormClosing(object sender, FormClosingEventArgs e) // Prevent Crosshair Form from being closed by any means.
        {
            e.Cancel = true;
        }




        private void SetCrosshairFromFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    Image crosshairImage = Image.FromFile(filePath);
                    pictureBox_Crosshair.Image = crosshairImage;
                    return; // Code is complete, we force return to never reach ICON_MISSING scenario.
                }
                catch { }
            }

            pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_MISSING; // Use missing icon if either file doesn't exist or there wasn't any success reading it as an image.
        }




        public void ForceInitializeSettings() => InitializeSettings(); // Public proxy function for initializing settings at any time given, from any class.
        private void InitializeSettings()
        {
            switch (Globals.Crosshair.selectedCrosshair)
            {
                default:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_MISSING; // Default crosshair to use if nothing is set.
                    break;

                case Globals.Crosshair.E_Crosshairs.none:
                    break;

                case Globals.Crosshair.E_Crosshairs.cs_nafany:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_CS_nafany;
                    break;

                case Globals.Crosshair.E_Crosshairs.cs_donk:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_CS_donk;
                    break;

                case Globals.Crosshair.E_Crosshairs.cs_felps:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_CS_Felps;
                    break;

                case Globals.Crosshair.E_Crosshairs.circleAqua:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_CIRCLE_AQUA;
                    break;

                case Globals.Crosshair.E_Crosshairs.circleWhite:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_CIRCLE_WHITE;
                    break;

                case Globals.Crosshair.E_Crosshairs.dotAqua:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_DOT_AQUA;
                    break;

                case Globals.Crosshair.E_Crosshairs.dotGreen:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_DOT_GREEN;
                    break;

                case Globals.Crosshair.E_Crosshairs.dotRed:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_DOT_RED;
                    break;

                case Globals.Crosshair.E_Crosshairs.dotYellow:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_DEFAULT_DOT_YELLOW;
                    break;

                case Globals.Crosshair.E_Crosshairs.tacticAqua:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_TACTIC_AQUA;
                    break;

                case Globals.Crosshair.E_Crosshairs.tacticWhite:
                    pictureBox_Crosshair.Image = Properties.Resources.IMG_CROSSHAIR_TACTIC_WHITE;
                    break;

                case Globals.Crosshair.E_Crosshairs.custom01:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair01FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom02:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair02FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom03:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair03FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom04:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair04FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom05:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair05FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom06:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair06FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom07:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair07FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom08:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair08FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom09:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair09FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom10:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair10FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom11:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair11FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom12:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair12FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom13:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair13FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom14:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair14FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom15:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair15FilePath);
                    break;

                case Globals.Crosshair.E_Crosshairs.custom16:
                    SetCrosshairFromFile(Globals.Crosshair.customCrosshair16FilePath);
                    break;
            }


            this.BackColor = this.pictureBox_Crosshair.BackColor; // Applying crosshair back color to the Form itself.
            this.TransparencyKey = this.pictureBox_Crosshair.BackColor; // Announcing Form back color as Form transparency key. This way we're getting rid of background color, making crosshair display as a proper PNG image.


            pictureBox_Crosshair.Location = new Point(this.Width / 2 - (pictureBox_Crosshair.Width / 2), this.Height / 2 - (pictureBox_Crosshair.Height / 2)); // Calculating X & Y coordinates of the user screen center and moving crosshair to them.
            this.Opacity = (float)Globals.Crosshair.opacity / 100; // Crosshair opacity is stored as percent number (0-100), bu WinForms opacity settings are designed for a float in range 0.0-1.0 | 100 / 100 = 1.0
        }
    }
}
