﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using HaltroyTabs;

namespace Korot
{
    static class Program
    {
        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            frmMain testApp = new frmMain();
            bool isIncognito = args.Contains("-incognito") ? true : false;
            if (Properties.Settings.Default.LastUser == "") { Properties.Settings.Default.LastUser = "user0"; }
            testApp.Tabs.Add(
                new TitleBarTab(testApp)
                {
                    Content = new frmCEF(testApp,isIncognito,Properties.Settings.Default.Homepage,Properties.Settings.Default.LastUser){}
                });
            foreach (string x in args)
            {
                if (x == Application.ExecutablePath) { } else if (x == "-incognito") { } else { testApp.CreateTab(x); }
            }
            testApp.isIncognito = args.Contains("-incognito");
            testApp.SelectedTabIndex = 0;
            TitleBarTabsApplicationContext applicationContext = new TitleBarTabsApplicationContext();
            applicationContext.Start(testApp);
            Application.Run(applicationContext);
        }

      
    }
}
