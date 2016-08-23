using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BackupGenerationShaper
{
  using System.Net.Mail;

  /// <summary>
  /// This class can be used to send alerting mails in case of unhandled exceptions.
  /// The code is written specifically for console-applications
  /// </summary>
  public class ShaperUnhandledExceptionMailer
  {

    private string m_MailTo;
    private string m_MailFrom;
    private string m_MailServer;
    private IList<string> m_ApplicationLog;


    public ShaperUnhandledExceptionMailer(string mailTo, string mailFrom, string mailServer, IList<string> applicationLog)
    {
      m_MailTo = mailTo;
      m_MailFrom = mailFrom;
      m_MailServer = mailServer;
      m_ApplicationLog = applicationLog;
      AppDomain.CurrentDomain.UnhandledException += this.On_UnhandledExceptionConsole;
    }


    /// <summary>
    /// the actual handler for unhandled exceptions in the app.domain
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void On_UnhandledExceptionConsole(object sender, UnhandledExceptionEventArgs e)
    {
      Debug.WriteLine("In UnhandledExceptionLogger.On_UnhandledExceptionEvent()");
      List<string> sl = new List<string>();
      Exception thrownException = (Exception)e.ExceptionObject;
            
      sl.Add($"Unhandled Exception {e.ExceptionObject.ToString()}");
      sl.Add("Stack-Trace:");
      sl.Add(thrownException.StackTrace);
      foreach (string s in sl) {
        Debug.WriteLine(s);
      }
      
      if (m_ApplicationLog != null) {
        sl.Add("ApplicationLog");
        foreach (string s in m_ApplicationLog) {
          sl.Add(s);
        }
      }
      //jetzt noch schnell mail schicken.
      SmtpClient smtpClient = new SmtpClient("mail.mdc.at");
      StringBuilder sb = new StringBuilder();
      foreach (string s in sl) {
        sb.AppendLine(s);
      }
      smtpClient.Send(m_MailFrom, m_MailTo, "BackupGenerationShaper - Unhandled Exception Error", sb.ToString());
    }



  } //end   public class ShaperUnhandledExceptionMailer


} //end namespace BackupGenerationShaper



