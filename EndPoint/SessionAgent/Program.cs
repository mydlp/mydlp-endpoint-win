using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using MyDLP.EndPoint.SessionAgent;
using System.Threading;

namespace MyDLP.EndPoint.SessionAgent
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>

        public static MainForm form; 

        [STAThread]
        static void Main()
        {
            //This is required to make application single instance for each user
            string user = Environment.UserName;
            Mutex mutex;
            try
            {
                mutex = Mutex.OpenExisting(user);
                return;
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                mutex = new Mutex(true, user);
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm();
            Application.Run(form);

            mutex.Close();
        }
    }
}
