using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.InteropServices;

namespace sideapp
{
    public partial class Form1 : Form
    {
        int iEventCount = 0;

        Thread threadShowDateTime;
        bool Form1Closed = false;

        Thread threadTimer1;
        bool Timer1Running = false;
        bool Timer1Clear = true;
        DateTime Timer1StartedAt;

        Thread threadTimer2;
        bool Timer2Running = false;
        bool Timer2Clear = true;
        DateTime Timer2StartedAt;
        UInt64 Timer2AddMs = 0;

        Thread threadSetControlsInvisible;

        string subst_s = "";
        string subst_p = "";
        string subst_z = "";

        [DllImport("kernel32.dll")]
        static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

        /// <summary>
        /// Constructor
        /// </summary>
        public Form1()
        {
            InitializeComponent();

            //	Set events
            this.MouseEnter += new System.EventHandler(this.Form1MouseEnter);
            this.FormClosed += new FormClosedEventHandler((o, a) => { Form1Closed = true; });

            this.labelTimer1.DoubleClick += new System.EventHandler(this.labelTimer1_DoubleClick);
            this.labelTimer2.DoubleClick += new System.EventHandler(this.labelTimer2_DoubleClick);
            this.label1EventCount.DoubleClick += new System.EventHandler(this.label1EventCount_DoubleClick);
        }

        /// <summary>   
        /// Handles the MouseEnter event for the form by making all previously hidden controls visible and initiating a
        /// background process to revert their visibility. Also brings the form to the foreground.
        /// </summary>
        private void Form1MouseEnter(object sender, EventArgs e)
        {
            //	When the mouse enters make all invisible controls visible and kick a thread to turn
            //	them back to invisible. Also bring the form to the foreground.

            labelTimer1.Visible = true;
            labelTimer1Status.Visible = true;

            labelTimer2.Visible = true;
            labelTimer2Status.Visible = true;

            label1EventCount.Visible = true;
            labelEventCountStatus.Visible = true;

            buttonQuit.Visible = true;

            threadSetControlsInvisible = new Thread(SetControlsInvisible);
            threadSetControlsInvisible.IsBackground = false;
            threadSetControlsInvisible.Start();
        }

        /// <summary>
        /// Make all unused controls invisible after a short delay. Removes screen clutter.
        /// </summary>
        private void SetControlsInvisible()
        {
            //	First wait a tad
            Thread.Sleep(3 * 1000);

            //	Hide all controls which are not active
            if (Timer1Clear)
            {
                labelTimer1.Visible = false;
                labelTimer1Status.Visible = false;
            }

            if (Timer2Clear)
            {
                labelTimer2.Visible = false;
                labelTimer2Status.Visible = false;
            }

            if (iEventCount == 0)
            {
                label1EventCount.Visible = false;
                labelEventCountStatus.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(subst_s))
            {
                labelS.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(subst_z))
            {
                labelS.Visible = false;
            }

            if (string.IsNullOrWhiteSpace(subst_p))
            {
                labelP.Visible = false;
            }

            buttonQuit.Visible = false;
        }

        /// <summary>
        /// On Form Load: Set up the form position, start the date/time thread, and initialize timers and event count.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            CheckForIllegalCrossThreadCalls = false;

            //	Put the form top right
            Left = SystemInformation.PrimaryMonitorSize.Width - Width;
            Top = 0;

            //	Start the clock
            threadShowDateTime = new Thread(ShowDateTime);
            threadShowDateTime.IsBackground = false;
            threadShowDateTime.Start();

            //	Init the timers
            labelTimer1.Text = "00:00:00";
            labelTimer1Status.Text = "";

            labelTimer2.Text = "00:00:00";
            labelTimer2Status.Text = "";

            //	Init the event count
            labelEventCountStatus.Text = "Event count";
            label1EventCount.Text = $"{iEventCount,5}";
        }

        /// <summary>
        /// Display the current date and time, and update subst drive mappings every interval.
        /// </summary>
        private void ShowDateTime()
        {
            while (!Form1Closed)
            {
                DateTime dt = DateTime.Now;

                //  Set the time values
                labelDay.Text = dt.DayOfWeek.ToString();
                labelDate.Text = dt.ToString("d-MMM-yyyy").ToUpper();
                labelTime.Text = dt.ToString("h:mm") + " " + dt.ToString("tt");

                //  Set the subst values
                var subst_target = new StringBuilder(1000);
                if (QueryDosDevice("S:", subst_target, 300) > 0)
                {
                    subst_s = subst_target.ToString();
                    subst_s = subst_s.Substring(Math.Max((subst_s.Length - 30), 1));
                    labelS.Text = $"S: = {subst_s}";
                    labelS.Visible = true;
                }
                else
                {
                    subst_s = "";
                    labelS.Text = "";
                    labelS.Visible = false;
                }

                //  Display an onscreen indication if certain drive letters have been mapped using SUBST.
                //  (Avoids an obscure class of bug!)
                if (QueryDosDevice("Z:", subst_target, 300) > 0)
                {
                    subst_z = subst_target.ToString();
                    subst_z = subst_z.Substring(Math.Max((subst_z.Length - 30), 1));
                    labelZ.Text = $"Z: = {subst_z}";
                    labelZ.Visible = true;
                }
                else
                {
                    subst_z = "";
                    labelZ.Text = "";
                    labelZ.Visible = false;
                }

                if (QueryDosDevice("P:", subst_target, 300) > 0)
                {
                    subst_p = subst_target.ToString();
                    subst_p = subst_p.Substring(Math.Max((subst_p.Length - 30), 1));
                    labelP.Text = $"P: = {subst_p}";
                    labelP.Visible = true;
                }
                else
                {
                    subst_p = "";
                    labelP.Text = "";
                    labelP.Visible = false;
                }

                Thread.Sleep(7 * 1000);
            }
        }

        /// <summary>
        /// Handles the click event for the timer label, starting or stopping the timer depending on its current state.
        /// </summary>
        private void labelTimer1_Click(object sender, EventArgs e)
        {
            //	Note the start time and kick off a thread to count seconds
            //	unless we are already running in which case stop the thread
            if (!Timer1Running)
            {
                //  Set a base time if none set
                if (Timer1Clear)
                {
                    Timer1StartedAt = DateTime.Now;
                    Timer1Clear = false;
                }

                //  Start the counter
                threadTimer1 = new Thread(ShowTimer1);
                threadTimer1.IsBackground = false;
                threadTimer1.Start();

                //  Note it
                Timer1Running = true;
                labelTimer1Status.Text = "Running";

            }
            else
            {
                //  Kill the timer thread
                threadTimer1.Abort();
                Timer1Running = false;
                labelTimer1Status.Text = "Split";
            }
        }

        /// <summary>
        /// Update the timer display for Timer 1.
        /// </summary>
        private void ShowTimer1()
        {
            while (true)
            {
                TimeSpan ts = new TimeSpan();
                ts = DateTime.Now - Timer1StartedAt;

                labelTimer1.Text = $"{ts.Hours,2:00}:{ts.Minutes,2:00}:{ts.Seconds,2:00}";

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Handles the double-click event for the timer label, resetting the timer to its initial state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelTimer1_DoubleClick(object sender, EventArgs e)
        {
            //	If the timer is running, stop it
            if (Timer1Running)
            {
                //  Kill the timer thread
                threadTimer1.Abort();
                Timer1Running = false;
            }

            //	Reset the base and text
            Timer1Clear = true;
            labelTimer1.Text = "00:00:00";
            labelTimer1Status.Text = "";
            Timer2AddMs = 0;
        }

        /// <summary>
        /// Handles the click event for the second timer label, starting or stopping the timer depending on 
        /// its current state.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void labelTimer2_Click(object sender, EventArgs e)
        {
            //	Note the start time and kick off a thread to count seconds
            //	unless we are already running in which case stop the thread
            if (!Timer2Running)
            {
                //  Set a base time if none set
                if (Timer2Clear)
                {
                    Timer2StartedAt = DateTime.Now;
                    Timer2AddMs = 0;
                    Timer2Clear = false;
                }

                //  Start the counter
                Timer2StartedAt = DateTime.Now;
                threadTimer2 = new Thread(ShowTimer2);
                threadTimer2.IsBackground = false;
                threadTimer2.Start();

                //  Note it
                Timer2Running = true;
                labelTimer2Status.Text = "Running";

            }
            else
            {
                //  Kill the timer thread
                threadTimer2.Abort();
                Timer2Running = false;
                labelTimer2Status.Text = "Paused";

                //  Compute the Ms so far
                TimeSpan ts = new TimeSpan();
                ts = DateTime.Now - Timer2StartedAt;
                Timer2AddMs += ((UInt64)ts.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Update the timer display for Timer 2.
        /// </summary>
        private void ShowTimer2()
        {
            while (true)
            {
                TimeSpan ts = new TimeSpan();
                TimeSpan tsAdd = new TimeSpan((long)(Timer2AddMs * 10000));

                ts = DateTime.Now - Timer2StartedAt + tsAdd;

                labelTimer2.Text = $"{ts.Hours,2:00}:{ts.Minutes,2:00}:{ts.Seconds,2:00}";

                //  Wobble the mouse ptr. Simulates activity to prevent screen savers/sleep.
                var origpos = Cursor.Position;
                var newpos = new Point(origpos.X + 1, origpos.Y);

                Cursor.Position = newpos;
                Cursor.Position = origpos;

                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Handles the double-click event for the timer label, stopping the timer if it is running and resetting the
        /// timer display.
        /// </summary>
        private void labelTimer2_DoubleClick(object sender, EventArgs e)
        {
            //	If the timer is running, stop it
            if (Timer2Running)
            {
                //  Kill the timer thread
                threadTimer2.Abort();
                Timer2Running = false;
            }

            //	Reset the base and text
            Timer2Clear = true;
            labelTimer2.Text = "00:00:00";
            labelTimer2Status.Text = "";
        }

        /// <summary>
        /// Single click on the event count label to increment the count.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label1EventCount_Click(object sender, EventArgs e)
        {
            iEventCount++;
            label1EventCount.Text = $"{iEventCount,5}";
        }

        /// <summary>
        /// Double click on the event count label to reset the count.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void label1EventCount_DoubleClick(object sender, EventArgs e)
        {
            iEventCount = 0;
            label1EventCount.Text = $"{iEventCount,5}";
        }

        /// <summary>
        /// On quit button clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonQuit_Click(object sender, EventArgs e)
        {
            if (Timer1Running)
                threadTimer1.Abort();

            if (Timer2Running)
                threadTimer2.Abort();

            threadShowDateTime.Abort();

            Application.Exit();
        }
    }
}
