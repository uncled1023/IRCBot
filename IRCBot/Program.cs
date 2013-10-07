using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Xml;

namespace IRCBot
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

            ThreadExceptionHandler handler = new ThreadExceptionHandler();

            Application.ThreadException += new ThreadExceptionEventHandler(handler.Application_ThreadException);

            Application.Run(new Interface());
        }
    }

    internal class ThreadExceptionHandler
    {
        /// 
        /// Handles the thread exception.
        /// 
        public void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            try
            {
                // Exit the program if the user clicks Abort.
                Exception result = ShowThreadExceptionDialog(sender, e.Exception);
                if (result.GetType() != typeof(OutOfMemoryException))
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath); // to start new instance of application
                }
            }
            catch
            {
                // Fatal error, terminate program
                try
                {
                    string logs_path = "";
                    string file_name = "Errors.log";
                    string time_stamp = DateTime.Now.ToString("hh:mm tt");
                    string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
                    string cur_dir = Directory.GetCurrentDirectory();
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
                    XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
                    if (Directory.Exists(list["logs_path"].InnerText))
                    {
                        logs_path = list["logs_path"].InnerText;
                    }
                    else
                    {
                        logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
                    }
                    if (Directory.Exists(logs_path + Path.DirectorySeparatorChar + "errors"))
                    {
                        StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                        log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + "Fatal Error");
                        log_file.Close();
                    }
                    else
                    {
                        Directory.CreateDirectory(logs_path + Path.DirectorySeparatorChar + "errors");
                        StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                        log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + "Fatal Error");
                        log_file.Close();
                    }
                }
                finally
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath); // to start new instance of application
                }
            }
            Application.Exit();
        }

        /// 
        /// Creates and displays the error message.
        /// 
        private Exception ShowThreadExceptionDialog(object sender, Exception ex)
        {
            string errorMessage =
                "Unhandled Exception:\n\n" +
                ex.Message + "\n\n" +
                ex.GetType() +
                "\n\nStack Trace:\n" +
                ex.StackTrace;

            string file_name = "Errors.log";
            string logs_path = "";
            string time_stamp = DateTime.Now.ToString("hh:mm tt");
            string date_stamp = DateTime.Now.ToString("yyyy-MM-dd");
            string cur_dir = Directory.GetCurrentDirectory();
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.Load(cur_dir + Path.DirectorySeparatorChar + "config" + Path.DirectorySeparatorChar + "config.xml");
            XmlNode list = xmlDoc.SelectSingleNode("/bot_settings/global_settings");
            if (Directory.Exists(list["logs_path"].InnerText))
            {
                logs_path = list["logs_path"].InnerText;
            }
            else
            {
                logs_path = cur_dir + Path.DirectorySeparatorChar + "logs";
            }
            if (Directory.Exists(logs_path + Path.DirectorySeparatorChar + "errors"))
            {
                StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                log_file.Close();
            }
            else
            {
                Directory.CreateDirectory(logs_path + Path.DirectorySeparatorChar + "errors");
                StreamWriter log_file = File.AppendText(logs_path + Path.DirectorySeparatorChar + "errors" + Path.DirectorySeparatorChar + file_name);
                log_file.WriteLine("[" + date_stamp + " " + time_stamp + "] " + errorMessage);
                log_file.Close();
            }

            return ex;
        }
    } // End ThreadExceptionHandler
}
