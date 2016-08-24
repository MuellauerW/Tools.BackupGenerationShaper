using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace BackupGenerationShaper {
  using System.Runtime.Remoting.Channels;

  class Program
  {

    private static ShaperConfig s_shaperConfig;
    private static ShaperLogger s_shaperLogger;


    /// <summary>
    /// The app starts at main, as expected. After writing some config-info to the console config is loaded and
    /// then we are up and running
    ///  
    /// </summary>
    /// <param name="args"></param>
    static void Main(string[] args)
    {
      
      Version version = Assembly.GetEntryAssembly().GetName().Version;
      Console.WriteLine($"Starting BackupGenerationShaper v{version.Major}.{version.Minor:00}.{version.Build}");
      //Deserializing user settings from my section ShaperConfig
      Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      ConfigurationSectionGroup userSettingsGroup = configuration.GetSectionGroup("userSettings");
      if (userSettingsGroup == null) {
        throw new ConfigurationErrorsException("No Config userSettings");
      }

      s_shaperConfig = userSettingsGroup.Sections["ShaperConfig"] as ShaperConfig;

      if (s_shaperConfig == null) {
        throw new ConfigurationErrorsException("Config Section ShaperConfig not found");
      }

      //Configure Logger
      s_shaperLogger = new ShaperLogger(
                   s_shaperConfig.LogOptions.LogToScreen,
                   s_shaperConfig.LogOptions.LogToFile,
                   Path.Combine(s_shaperConfig.LogOptions.LogDirectory, "BackupGenerationShaper_" + DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss") + ".log"),
                   Console.WriteLine);

      foreach (string s in ShaperConfigTools.EnumerateShaperConfig(s_shaperConfig)) {
        s_shaperLogger.WriteLine(s);
      }

      FileSystemTools fileSystemTools = new FileSystemTools(s_shaperLogger);
      FileTransferProtocolTools ftpTools = new FileTransferProtocolTools(s_shaperLogger, s_shaperConfig.FtpOptions.Hostname, s_shaperConfig.FtpOptions.Username, s_shaperConfig.FtpOptions.Password);
      

      //Configure mailing unhandled Exception Errors
      ShaperUnhandledExceptionMailer shaperUnhandledExceptionMailer = null;
      if (s_shaperConfig.DebugOptions.OnExceptionSendMail == true) {
        if (String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailSender) ||
            String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailReceiverList) ||
            String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailSmtpHostname)) {
          throw new ConfigurationErrorsException("Config Error SendMail enabled but Mail Settings missing");
        } else {
          shaperUnhandledExceptionMailer = new ShaperUnhandledExceptionMailer(
                                                s_shaperConfig.DebugOptions.MailReceiverList,
                                                s_shaperConfig.DebugOptions.MailSender,
                                                s_shaperConfig.DebugOptions.MailSmtpHostname,
                                                s_shaperLogger.LogList );
        }
      }
      
      //start to enumerate the directory List

      s_shaperLogger.WriteLine("Start enumerating DirectoryList");
      foreach (ShaperDirectoryElement sde in s_shaperConfig.DirectoryList) {
        s_shaperLogger.WriteLine($" [Directory]:{sde.DirectoryName} [Protocol]:{sde.ProtocolName} [TimestampFormat]:{sde.TimestampFormat} [GenerationCount]:{sde.GenerationsCount}");
        IDictionary<string, List<string>> fileGenerations = new Dictionary<string, List<string>>();
        switch (sde.ProtocolName.ToLower()) {
          case "ftp:":
            fileGenerations = new Dictionary<string, List<string>>();
            ftpTools.ParseDirectory(sde.DirectoryName, sde.TimestampFormat, fileGenerations);
            //extracting files that must be deleted
            foreach (KeyValuePair<string, List<string>> fgentry in fileGenerations) {
              fgentry.Value.Sort();
              s_shaperLogger.WriteLine($" [File Bucket Entry]:{fgentry.Key}");
              foreach (string fn in fgentry.Value) {
                s_shaperLogger.WriteLine($"  [Filename]:{fn}");
              }
              int deletionCounter = fgentry.Value.Count - sde.GenerationsCount;
              if (deletionCounter > 0) {
                s_shaperLogger.WriteLine("  FileBucket Delete List:");
                foreach (String fname in fgentry.Value) {
                  s_shaperLogger.WriteLine($"    [Filename]:{fname}");
                  ftpTools.DeleteList.Add(fname);
                  deletionCounter--;
                  if (deletionCounter == 0)
                    break;
                }
              }
            }
            break;

          case "file:":
          case "smb:":
            fileGenerations = new Dictionary<string, List<string>>();
            fileSystemTools.ParseDirectory(sde.DirectoryName, sde.TimestampFormat, fileGenerations);
            //extracting files that must be deleted
            foreach (KeyValuePair<string, List<string>> fgentry in fileGenerations) {
              fgentry.Value.Sort();
              s_shaperLogger.WriteLine($" [File Bucket Entry]:{fgentry.Key}");
              foreach (string fn in fgentry.Value) {
                s_shaperLogger.WriteLine($"  [Filename]:{fn}");
              }
              int deletionCounter = fgentry.Value.Count - sde.GenerationsCount;
              if (deletionCounter > 0) {
                s_shaperLogger.WriteLine("  FileBucket Delete List:");
                foreach (String fname in fgentry.Value) {
                  s_shaperLogger.WriteLine($"    [Filename]:{fname}");
                  fileSystemTools.DeleteList.Add(fname);
                  deletionCounter--;
                  if (deletionCounter == 0)
                    break;
                }
              }
            }
            break;

          default:
            //illegal protocol
            s_shaperLogger.WriteLine($" ERROR: Illegal Protocol [Directory]:{sde.DirectoryName} [Protocol]:{sde.ProtocolName} [TimestampFormat]:{sde.TimestampFormat} [GenerationCount]:{sde.GenerationsCount}");
            break;
        } //end switch
      } //end foreach DirectoryList



      //we have found files that should be deleted, now delete them if configured



      //just for diganostics in development: wait for the user to close the window
      if (s_shaperConfig.DebugOptions.IsInteractive == true && (fileSystemTools.DeleteList.Count > 0 || ftpTools.DeleteList.Count > 0)) {
        while (Console.KeyAvailable) { Console.ReadKey(true); }
        Console.WriteLine("Interactive Mode - Press any key to start deleting files");
        Console.ReadKey(true);
      }


     
     //all files that should be deleted are written to the fileDeletionList of the Tools-Object
      s_shaperLogger.WriteLine("starting FileSystem file deletion operation");
      foreach (string fn in fileSystemTools.DeleteList) {
        if (s_shaperConfig.DebugOptions.IsSimulateOnly == false) {
          s_shaperLogger.WriteLine($"  [FileSystem DeleteFile]:{fn}");
          fileSystemTools.DeleteFile(fn);
        }
      }


      //all files that should be deleted are written to the fileDeletionList
      s_shaperLogger.WriteLine("starting FileTransferProtocol file deletion operation");
      foreach (string fn in ftpTools.DeleteList) {
        if (s_shaperConfig.DebugOptions.IsSimulateOnly == false) {
          s_shaperLogger.WriteLine($"  [FTP DeleteFile]:{fn}");
          ftpTools.DeleteFile(fn);
        }
      }



      if (s_shaperConfig.DebugOptions.IsSimulateOnly == true) {
        s_shaperLogger.WriteLine($"# of files found for FileSystem deletion (SIMULATE ONLY):{fileSystemTools.DeleteList.Count}");
        s_shaperLogger.WriteLine($"# of files found for FTP deletion (SIMULATE ONLY):{ftpTools.DeleteList.Count}");
      } else {
        s_shaperLogger.WriteLine($"# of files deleted:{fileSystemTools.DeleteList.Count}");
        s_shaperLogger.WriteLine($"# of files deleted:{ftpTools.DeleteList.Count}");
      }
      s_shaperLogger.WriteLine("finished file deletion operation");


      s_shaperLogger.CloseAll();  
      //just for diganostics in development: wait for the user to close the window
      if (s_shaperConfig.DebugOptions.IsInteractive == true) {
        while (Console.KeyAvailable) { Console.ReadKey(true); }
        Console.WriteLine("Interactive Mode - Press any key to terminate");
        Console.ReadKey(true);
      }

    } //end   static void Main(string[] args)



    
  } // end class Program


} //end namespace BackupGenerationShaper