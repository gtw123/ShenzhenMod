using System;
using System.Windows.Forms;

[assembly: log4net.Config.XmlConfigurator(Watch = false)]

namespace ShenzhenMod
{
    static class Program
    {
        private static readonly log4net.ILog sm_log = log4net.LogManager.GetLogger(typeof(Program));

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            sm_log.Info("---------------------------------------------------------------------------------------");
            sm_log.Info("Starting up");

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1(args.Length > 0 ? args[0] : null));
        }
    }
}
