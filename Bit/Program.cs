// Copyright 2011-2019 by Jeremy Spiller
// See Lincense.txt for details

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Gosub.Bit
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
            Application.Run(new FormMain());

        }
    }
}
