namespace BackupGenerationShaper
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Threading.Tasks;
  using System.Text.RegularExpressions;


  /// <summary>
  /// This utilty class does the low level file system heavy lifting jobs 
  /// </summary>
  public class FileSystemTools
  {
    private ShaperLogger _logger;


    public FileSystemTools(ShaperLogger logger)
    {
      _logger = logger;
      _deleteList = new List<string>();
    }


    private IList<string> _deleteList;
    public IList<string> DeleteList {
      get { return _deleteList;}
      
    }


    /// <summary>
    /// The method ParseDirectory traverses a directory and writes all Timestamped versions
    /// of files in the directory entry of the matching prefix and appended suffix.  
    /// Timestamps
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <param name="timeStampMask"></param>
    public void ParseDirectory(string directoryPath, string timeStampMask, IDictionary<string, List<string>> fileGenerations)
    {
      FileInfo[] fileList = new FileInfo[0];
      String searchPattern;
      String fileKey;

      switch (timeStampMask) {
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
          _logger.WriteLine($"Error ParseDirectory - invalide TimeStampMask!  [Directory]:{directoryPath} [TimeStamp]:{timeStampMask} [Count]:{fileGenerations}");
          return;
      }
      Regex regex = new Regex(searchPattern);
      DirectoryInfo workingDir = new DirectoryInfo(directoryPath);
      try {
        fileList = workingDir.GetFiles();
      } catch (DirectoryNotFoundException ex) {
        _logger.WriteLine($"Error Scannding Directory - FilesNotFound [Directory]:{workingDir}");
        _logger.WriteLine($"Exception ErrorMessage: {ex.ToString()}");
      }

      foreach (FileInfo fi in fileList) {
        Match match = regex.Match(fi.FullName);
        if (match.Success) {
          fileKey = match.Groups["prefix"].Value + match.Groups["suffix"].Value;
          if (fileGenerations.ContainsKey(fileKey)) {
            fileGenerations[fileKey].Add(fi.FullName);
          } else {
            fileGenerations[fileKey] = new List<String>();
            fileGenerations[fileKey].Add(fi.FullName);
          }
        }
      }
    } //end static void ParseDirectory(String directoryPath, String timeStampMask, IDictionary<String, List<String>> fileGenerations )


    /// <summary>
    /// This method simply deletes a file. It's the encapsulation, stupid
    /// </summary>
    /// <param name="filename"></param>
    public void DeleteFile(string filename)
    {
      File.Delete(filename);
    }


  } //end   public class FileSystemTools


} //end namespace BackupGenerationShaper

