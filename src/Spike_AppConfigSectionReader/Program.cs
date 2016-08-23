using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Spike_AppConfigSectionReader
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Spike for learning handling .exe.config Filese");
      Console.WriteLine($"This program will try to deserialize config data{Environment.NewLine}from a config section into a config object");
      Console.WriteLine($"DefaultValue form .exe.config: \"{Properties.Settings.Default.DefaultValue}\" ");

      //Hier wird die ShaperConfig aus dem userSettings herausgeholt. Aber so ist sie super gekapselt
      Configuration configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
      ConfigurationSectionGroup userSettingsGroup = configuration.GetSectionGroup("userSettings");
      ShaperConfig shaperConfig = userSettingsGroup.Sections["ShaperConfig"] as ShaperConfig;


      if (shaperConfig != null) {
        Console.WriteLine("Options Debug:");
        Console.WriteLine($" Debug->IsVerbose: {shaperConfig.DebugOptions.IsVerbose}");
        Console.WriteLine($" Debug->IsInteractive: {shaperConfig.DebugOptions.IsInteractive}");
        Console.WriteLine($" Debug->OnSendMail: {shaperConfig.DebugOptions.OnExceptionSendMail}");
        Console.WriteLine($" Debug->MailFrom: {shaperConfig.DebugOptions.MailFrom}");
        Console.WriteLine($" Debug->MailTo: {shaperConfig.DebugOptions.MailTo}");
        Console.WriteLine($" Debug->MailSMTP: {shaperConfig.DebugOptions.MailSMTP}");
        Console.WriteLine("Options Log:");
        Console.WriteLine($" Log->IsLogging: {shaperConfig.LogOptions.IsLogging}");
        Console.WriteLine($" Log->LogDirectory: {shaperConfig.LogOptions.LogDirectory}");
        Console.WriteLine("Options DirectoryList:");
        foreach (ShaperDirectoryElement sde in shaperConfig.DirectoryList) {
          Console.WriteLine($" Dir: {sde.DirectoryName} TS: {sde.TimestampFormat} Count: {sde.GenerationsCount}");
        }
        Console.WriteLine("Options End");
      } else {
        Console.WriteLine("NoConfig Found");
      }

      while (Console.KeyAvailable) { Console.ReadKey(true); }
      Console.WriteLine("Press any key to terminate");
      Console.ReadKey(true);

    } //end     static void Main(string[] args)


  } //end   class Program


} //end namespace Spike_AppConfigSectionReader


