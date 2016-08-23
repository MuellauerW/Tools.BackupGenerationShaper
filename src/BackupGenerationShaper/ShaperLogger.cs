using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupGenerationShaper
{
  using System.CodeDom;

  public class ShaperLogger
  {
    private bool m_LogToFile;
    private bool m_LogToScreen;
    private IList<string> m_LogList;
    private string m_FileName;
    private TextWriter m_LogFileWriter;
    private Action<string> m_ScreenWriter;

    public IList<string> LogList => m_LogList;


    /// <summary>
    /// ctor initializes all necessary variables. If anything is missing an 
    /// exception is thrown
    /// </summary>
    /// <param name="logToScreen"></param>
    /// <param name="logToFile"></param>
    /// <param name="filename"></param>
    /// <param name="screenWriter"></param>
    public ShaperLogger(bool logToScreen, bool logToFile, string filename, Action<string>screenWriter)
    {
      m_LogToScreen = logToScreen;
      m_LogToFile = logToFile;
      m_LogList = new List<string>();

      if (m_LogToFile == true) {
        if (String.IsNullOrEmpty(filename)) {
          throw new ArgumentNullException("ShaperLogger->ctor -> Filename missing");
        }
        m_FileName = filename;
        m_LogFileWriter = new StreamWriter(m_FileName, true, Encoding.UTF8);
        if (m_LogFileWriter == null) {
          throw new ArgumentException($"ShaperLogger->ctor Logfile with name {filename} cannot be opened");
        }
      }

      if (m_LogToScreen == true) {
        if (screenWriter == null) {
          throw new ArgumentNullException("ShaperLogger->ctor -> Delegate for ScreenWriting is missing");
        }
        m_ScreenWriter = screenWriter;
      }
      this.WriteLine("Startup BackupGenerationShaperLogger Date/Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") );

    }

    /// <summary>
    /// Diese Methode wird sicherlich am häufigsten aufgerufen, sie schreibt eine 
    /// Message als Zeile ins Log
    /// </summary>
    /// <param name="logMsg"></param>
    public void WriteLine(string logMsg )
    {
      m_LogList.Add(logMsg);
      if (m_LogToFile == true) {
        m_LogFileWriter.WriteLine(logMsg);
        m_LogFileWriter.Flush();    
      }
      if (m_LogToScreen == true) {
        m_ScreenWriter(logMsg);
      }
    }


    public void CloseAll()
    {
      this.WriteLine("Shutdown BackupGenerationShaperLogger Date/Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
      if (m_LogToFile == true) {
        m_LogFileWriter.Close();
      }
    }


  } //end class ShaperLogger

} //end namespace BackupGenerationShaper

