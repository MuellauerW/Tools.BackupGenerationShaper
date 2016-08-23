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
      Console.WriteLine($"Starting BackupGenerationShaper Ver {version.Major}.{version.Minor}.{version.Build}");
      //Deserializing user settings from my section ShaperConfig
      Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      ConfigurationSectionGroup userSettingsGroup = configuration.GetSectionGroup("userSettings");
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
      
      //Configure mailing unhandeled Exception Errors
      ShaperUnhandledExceptionMailer shaperUnhandledExceptionMailer = null;
      if (s_shaperConfig.DebugOptions.OnExceptionSendMail == true) {
        if (String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailFrom) ||
            String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailTo) ||
            String.IsNullOrEmpty(s_shaperConfig.DebugOptions.MailSMTP)) {
          throw new ConfigurationErrorsException("Config Error SendMail enabled but Mail Settings missing");
        } else {
          shaperUnhandledExceptionMailer = new ShaperUnhandledExceptionMailer(
                                                s_shaperConfig.DebugOptions.MailTo,
                                                s_shaperConfig.DebugOptions.MailFrom,
                                                s_shaperConfig.DebugOptions.MailSMTP,
                                                s_shaperLogger.LogList );
        }
      }
      
      //start to enumerate the directory List
      IList<String> fileDeletionList = new List<String>();

      s_shaperLogger.WriteLine("Start enumerating DirectoryList");
      foreach (ShaperDirectoryElement sde in s_shaperConfig.DirectoryList) {
        s_shaperLogger.WriteLine($" [Directory]:{sde.DirectoryName} [TimestampFormat]:{sde.TimestampFormat} [GenerationCount]:{sde.GenerationsCount}");


        IDictionary<string, List<string> > fileGenerations = new Dictionary<string, List<string> >();
        ParseDirectory(sde.DirectoryName, sde.TimestampFormat, fileGenerations);
        //extracting files that must be deleted
        foreach (KeyValuePair<string, List<string> > fgentry in fileGenerations) {
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
              fileDeletionList.Add(fname);
              deletionCounter--;
              if (deletionCounter == 0)
                break;
            }
          }
        }
      }

      //just for diganostics in development: wait for the user to close the window
      if (s_shaperConfig.DebugOptions.IsInteractive == true && fileDeletionList.Count > 0) {
        while (Console.KeyAvailable) { Console.ReadKey(true); }
        Console.WriteLine("Interactive Mode - Press any key to start deleting files");
        Console.ReadKey(true);
      }


      //all files that should be deleted are written to the fileDeletionList
      s_shaperLogger.WriteLine("starting file deletion operation");
      foreach (string fn in fileDeletionList) {
        if (s_shaperConfig.DebugOptions.IsSimulateOnly == false) {
          s_shaperLogger.WriteLine($"  [DeleteFile]:{fn}");
          File.Delete(fn);
        }
      }

      if (s_shaperConfig.DebugOptions.IsSimulateOnly == true) {
        s_shaperLogger.WriteLine($"# of files found for deletion (SIMULATE ONLY):{fileDeletionList.Count}");
      } else {
        s_shaperLogger.WriteLine($"# of files deleted:{fileDeletionList.Count}");
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



    /// <summary>
    /// The method ParseDirectory traverses a directory and writes all Timestamped versions
    /// of files in the directory entry of the matching prefix and appended suffix.  
    /// Timestamps
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <param name="timeStampMask"></param>
    static void ParseDirectory(string directoryPath, string timeStampMask, IDictionary<string, List<string> > fileGenerations )
    {
      FileInfo [] fileList = new FileInfo[0];
      String searchPattern;
      String fileKey;

      switch(timeStampMask) {
      case "yyyy_MM_dd":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d_\d\d_\d\d(?<suffix>.+$)";
        break;
      case "yyyy-MM-dd":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d-\d\d-\d\d(?<suffix>.+$)";
        break;
      case "yyyy_MM_dd_hh_mm":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d_\d\d_\d\d_\d\d_\d\d(?<suffix>.+$)";
        break;
      case "yyyy-MM-dd-hh-mm":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d-\d\d-\d\d-\d\d-\d\d(?<suffix>.+$)";
        break;
      case "yyyy_MM_dd_hh_mm_ss":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d_\d\d_\d\d_\d\d_\d\d_\d\d(?<suffix>.+$)";
        break;
      case "yyyy-MM-dd-hh-mm-ss":
        searchPattern = @"(?<prefix>^.+)\d\d\d\d-\d\d-\d\d-\d\d-\d\d-\d\d(?<suffix>.+$)";
        break;
      default:
        s_shaperLogger.WriteLine($"Error ParseDirectory - invalide TimeStampMask!  [Directory]:{directoryPath} [TimeStamp]:{timeStampMask} [Count]:{fileGenerations}");
          return;
      }
      Regex regex = new Regex(searchPattern);
      DirectoryInfo workingDir = new DirectoryInfo(directoryPath);
      try {
        fileList = workingDir.GetFiles();
      } catch (DirectoryNotFoundException ex) {
        s_shaperLogger.WriteLine($"Error Scannding Directory - FilesNotFound [Directory]:{workingDir}");
        s_shaperLogger.WriteLine($"Exception ErrorMessage: {ex.ToString()}");
      }

      foreach (FileInfo fi in fileList) {
        Match match = regex.Match(fi.FullName);
        if (match.Success) {
          fileKey = match.Groups["prefix"].Value  + match.Groups["suffix"].Value;
          if (fileGenerations.ContainsKey(fileKey)) {
            fileGenerations[fileKey].Add(fi.FullName);
          } else {
            fileGenerations[fileKey] = new List<String>();
            fileGenerations[fileKey].Add(fi.FullName);
          }          
        }
      }
    } //end static void ParseDirectory(String directoryPath, String timeStampMask, IDictionary<String, List<String>> fileGenerations )

    
  } // end class Program


} //end namespace BackupGenerationShaper