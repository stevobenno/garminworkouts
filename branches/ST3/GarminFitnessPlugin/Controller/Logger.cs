using System;
using System.IO;
using System.Windows.Forms;

namespace GarminFitnessPlugin.Controller
{
    class Logger
    {
        private Logger()
        {
            m_LogFile = File.CreateText("GF_Log.txt");
            m_LogFile.AutoFlush = true;

            //MessageBox.Show((m_LogFile.BaseStream as FileStream).Name);
        }

        ~Logger()
        {
            m_LogFile = null;
        }

        public static Logger Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new Logger();
                }

                return m_Instance;
            }
        }

        public void LogText(String textToLog)
        {
            if (m_LogFile != null &&
                m_LogFile.BaseStream != null)
            {
                m_LogFile.WriteLine(String.Format("{0} : {1}", DateTime.Now.ToShortTimeString(), textToLog));
            }
        }

        private static Logger m_Instance = null;

        private StreamWriter m_LogFile = null;
    }
}
