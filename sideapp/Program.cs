/*
 * PROGRAM: Sideapp
 * 
 * PURPOSE: Windows desktop app which provides timers, an event counter, and some other stuff for programmer
 *          assistance. Viz:
 *          
 *          1. Shows current date/time
 * 
 *          2. Split timer, on click shows elapsed time since last click but keeps the timer running.
 * 
 *          3. Lap timer, on click stops the clock, restarts on next click.
 *             - Also jiggles the mouse pointer while running to simulate system activity.
 * 
 *          4. Event counter, just counts clicks.
 *          
 *          5. Shows if certain drive letters have been mapped using Windows SUBST command.
 * 
 *          Controls are hidden when not in use to avoid screen clutter.
 *          
 *          Intended to be run with a black desktop background; moves itself to top-right of the main
 *          display on start. Every tick moves back to the top-right if it has been displaced by the O/S.
 *          
 * AUTHOR:  Mark Parker (markstjohnparker@gmail.com)
 * 
 */

using System;
using System.Windows.Forms;

namespace sideapp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
