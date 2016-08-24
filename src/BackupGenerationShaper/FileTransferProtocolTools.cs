namespace BackupGenerationShaper
{
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Linq;
  using System.Text;
  using System.Text.RegularExpressions;
  using System.Threading.Tasks;
  using Microsoft.SqlServer.Server;
  using WinSCP;


  /// <summary>
  /// this utility class does the heave lifting in ftp handling
  /// </summary>
  public class FileTransferProtocolTools
  {
    private string _username;
    private string _password;
    private string _hostname;
    private ShaperLogger _logger;
    private SessionOptions _sessionOptions;


    /// <summary>
    /// initialisation for ftp connection
    /// </summary>
    public FileTransferProtocolTools(ShaperLogger logger, string hostname, string username, string password)
    {
      _logger = logger;
      _hostname = hostname;
      _username = username;
      _password = password;
      _deleteList = new List<string>();

      //session options setzen, dann bleiben die meissten variablen lokal
      _sessionOptions = new SessionOptions {
        Protocol = Protocol.Ftp,
        HostName = _hostname,
        PortNumber = 21,
        UserName = _username,
        Password = _password
      };

    } //end ctor



    private IList<string> _deleteList;
    public IList<string> DeleteList
    {
      get { return _deleteList; }

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
      String searchPattern;

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

      try {
        //Session zu server starten        
        using (Session session = new Session()) {
          // Connect
          session.Open(_sessionOptions);
          //jetzt muss ich Files vom directory case_files und vom Directory direct_payments abholen
          RemoteDirectoryInfo remoteDirInfo = session.ListDirectory(directoryPath);
          foreach (RemoteFileInfo fi in remoteDirInfo.Files) {
            Match match = regex.Match(fi.FullName);
            if (match.Success) {
              string fileKey = match.Groups["prefix"].Value + match.Groups["suffix"].Value;
              if (fileGenerations.ContainsKey(fileKey)) {
                fileGenerations[fileKey].Add(fi.FullName);
              } else {
                fileGenerations[fileKey] = new List<String>();
                fileGenerations[fileKey].Add(fi.FullName);
              }
            }
          }
          session.Close();
        }
      } catch (Exception ex) {
        _logger.WriteLine($"FEHLER {ex.Message}");
      }
    } //end void ParseDirectory(String directoryPath, String timeStampMask, IDictionary<String, List<String>> fileGenerations )




    /// <summary>
    /// This method deletes a file using FTP session
    /// </summary>
    /// <param name="filename"></param>
    public void DeleteFile(string filename)
    {

      try {
        //Session zu server starten        
        using (Session session = new Session()) {
          // Connect
          session.Open(_sessionOptions);
          session.RemoveFiles(filename);
          session.Close();
        }
      } catch (Exception ex) {
        _logger.WriteLine($"FEHLER {ex.Message}");
      }
    }
  }

} //end namespace BackupGenerationShaper




/*

namespace FileTransferCenter.Services
{
  using System;
  using System.Collections.Generic;
  using System.Configuration;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using System.Text;
  using WinSCP;
  using FileTransferCenter.Framework;
  using Ionic.Zip;

  /// <summary>
  /// Diese Klasse implementiert den COEO SFTP Transfer. Daten werden mit stpf geholt.
  /// Sie sind in verschlüsselten zip files und müssen dann entzippt werden.
  /// Die Konfiguration kommt aus dem Dictionary. 
  /// Es werden keine Konvertierungen durchgeführt, da das in einem späteren 
  /// manuellen Schritt erledigt wird.
  /// </summary>
  public sealed class CoeoFileTransferAgent : FileTransferAgent
  {
    private string _remotePathAuftraege;
    private bool _remoteMoveAuftraege;
    private string _localPathAuftraege;

    private string _remotePathDirektzahler;
    private bool _remoteMoveDirektzahler;
    private string _localPathDirektzahler;


 
private string _password;
private SessionOptions _sessionOptions;


/// <summary>
/// Der ctor schickt die Timestamp der Aktivieren des FileTransferCenters, damit alle Files und Directories
/// aus diesem Job den selben Timestmap haben. Die Konfiguration kommt aus dem .config file und wird wie
/// üblich als Dictionary übergeben. Message log über Event wie gehabt
/// </summary>
/// <param name="transactionTS"></param>
/// <param name="configDictionary"></param>
/// <param name="onLogEventHandler"></param>

public CoeoFileTransferAgent(DateTime transactionTS,
  IDictionary<string, string> configDictionary,
  EventHandler<LogEventArgs> onLogEventHandler = null) : base(transactionTS, configDictionary, onLogEventHandler)
    {
  string username;
  int port;
  string sshfingerprint;
  string uri;

  RaiseOnLogEvent(TraceLevel.Info, "CoeoFileTransferAgent.ctor");
  uri = configDictionary.GetAs<string>("Uri", "ftp.coeo-mandanten.de");
  RaiseOnLogEvent(TraceLevel.Info, $" Uri: {uri}");
  sshfingerprint = configDictionary.GetAs<string>("SshFingerPrint", "ssh-rsa 2048 e6:00:5e:03:8f:42:0a:87:15:4f:6f:48:57:fd:5f:f7");
  RaiseOnLogEvent(TraceLevel.Info, $" SshFingerPrint: {sshfingerprint}");
  port = configDictionary.GetAs<int>("Port", 22);
  RaiseOnLogEvent(TraceLevel.Info, $" Port: {port}");
  username = configDictionary.GetAs<string>("Username", "knp");
  RaiseOnLogEvent(TraceLevel.Info, $" Username: {username}");
  _password = configDictionary.GetAs<string>("Password", "Fiy5caif");
  RaiseOnLogEvent(TraceLevel.Info, $" Password: {_password}");
  _remotePathAuftraege = configDictionary.GetAs<string>("RemotePathAuftraege", "/voncoeo");
  RaiseOnLogEvent(TraceLevel.Info, $" RemotePathAuftraege: {_remotePathAuftraege}");
  _remoteMoveAuftraege = configDictionary.GetAs<bool>("RemoteMoveAuftraege", false);
  RaiseOnLogEvent(TraceLevel.Info, $" RemoteMoveAuftraege: {_remoteMoveAuftraege}");
  _localPathAuftraege = configDictionary.GetAs<string>("LocalPathAuftraege", @"H:\home\Kunden\KNP_Finance\Testdaten\Coeo\Aufträge");
  RaiseOnLogEvent(TraceLevel.Info, $" LocalPathAuftraege: {_localPathAuftraege}");
  if (!Directory.Exists(_localPathAuftraege)) {
    string err_msg = $" LocalPathAuftraege {_localPathAuftraege} not found";
    RaiseOnLogEvent(TraceLevel.Error, err_msg);
    throw new ConfigurationErrorsException(err_msg);
  }
  _remotePathDirektzahler = configDictionary.GetAs<string>("RemotePathDirektzahler", "/voncoeo");
  RaiseOnLogEvent(TraceLevel.Info, $" RemotePathDirektzahler: {_remotePathDirektzahler}");
  _remoteMoveDirektzahler = configDictionary.GetAs<bool>("RemoteMoveDirektzahler", false);
  RaiseOnLogEvent(TraceLevel.Info, $" RemoteMoveDirektzahler: {_remoteMoveDirektzahler}");
  _localPathDirektzahler = configDictionary.GetAs<string>("LocalPathDirektzahler", @"H:\home\Kunden\KNP_Finance\Testdaten\Coeo\Direktzahler");
  RaiseOnLogEvent(TraceLevel.Info, $" LocalPathDirektzahler: {_localPathDirektzahler}");
  if (!Directory.Exists(_localPathDirektzahler)) {
    string err_msg = $" LocalPathDirektzahler {_localPathDirektzahler} not found";
    RaiseOnLogEvent(TraceLevel.Error, err_msg);
    throw new ConfigurationErrorsException(err_msg);
  }


  //session options setzen, dann bleiben die meissten variablen lokal
  _sessionOptions = new SessionOptions {
    Protocol = Protocol.Sftp,
    HostName = uri,
    PortNumber = port,
    UserName = username,
    Password = _password,
    SshHostKeyFingerprint = sshfingerprint
  };
} //end ctor




/// <summary>
/// Hier geschieht der Download der Files für den Kunden GFKL
/// </summary>
/// <returns></returns>
public override DownloadStatus DownloadAllFiles()
{
  DownloadStatus ftds = new DownloadStatus();
  string tsdirname = "Download_" + _transactionTS.ToString("yyyy_MM_dd_HH_mm");
  RaiseOnLogEvent(TraceLevel.Info, "CoeoFileTransferAgent.DownloadAllFiles().Start");
  try {
    //Session zu server starten        
    using (Session session = new Session()) {
      // Connect
      session.Open(_sessionOptions);
      //jetzt muss ich Files vom directory case_files und vom Directory direct_payments abholen
      DownloadProcessRemoteDirectory(session, _remotePathAuftraege,
                                      ((rfi) => rfi.Name.StartsWith("50D")), Path.Combine(_localPathAuftraege, tsdirname),
                                      ftds, _remoteMoveAuftraege);
      DownloadProcessRemoteDirectory(session, _remotePathDirektzahler,
                                      ((rfi) => rfi.Name.StartsWith("51D")), Path.Combine(_localPathDirektzahler, tsdirname),
                                      ftds, _remoteMoveDirektzahler);
      session.Close();
    }
  } catch (Exception ex) {
    ftds.ErrorCount++;
    ftds.StatusMessages.Add(ex.Message);
    RaiseOnLogEvent(TraceLevel.Error, " Exception on Download Message: " + ex.Message);
    return ftds;
  }
  RaiseOnLogEvent(TraceLevel.Info, "CoeoFileTransferAgent.DonwnloadAllFiles().Finished");
  return ftds;
} //end     public override DownloadStatus DownloadAllFiles()




/// <summary>
/// Diese Implementation des Postprocessings schiebt das File in ein 
/// subdirectory namens processed. Da wir in der Praxis öfter Probleme damit hatten,
/// dass das File aus irgend einem Grund zwar schon im processed verzeichnis vorhanden
/// war, aber auch im daten-directory, ist nicht ganz klar, was da passiert. daher
/// wird vor dem moven geschaut, ob es das file nicht schon gibt, dann gibt es eine
/// fehlermeldung und dann wird auch nach dem moven geschaut, ob das file wirklich weg
/// ist, dann gibt es auch eine fehlermeldung.
/// </summary>
/// <param name="session"></param>
/// <param name="fnameRemoteLong"></param>
protected override void DownloadPostProcessRemoteFile(Session session, string fnameRemoteLong)
{
  int lastSlashPosition = fnameRemoteLong.LastIndexOf("/", StringComparison.InvariantCulture);
  string fnameRemoteLongInArchive = fnameRemoteLong.Insert(lastSlashPosition, "/processed");

  //zuerst wird kontrolliert, ob es das file nicht schon gibt.
  if (session.FileExists(fnameRemoteLongInArchive)) {
    RaiseOnLogEvent(TraceLevel.Error, $"CoeoeFileTransferAgent.DownloadPostProcessRemoteFile - das File {fnameRemoteLongInArchive} ist schon vorhanden - ARCHIVIERUNG BITTE KLÄREN");
  } else {
    session.MoveFile(fnameRemoteLong, fnameRemoteLongInArchive);
    if (session.FileExists(fnameRemoteLong)) {
      RaiseOnLogEvent(TraceLevel.Error, $"CoeoeFileTransferAgent.DownloadPostProcessRemoteFile das File {fnameRemoteLong} konnte nicht verschoben werden - BITTE ABKLÄREN");
    }
  }

}


/// <summary>
/// Ich muss die Files ja immer dann noch unzippen
/// </summary>
/// <param name="fnameLocalLong"></param>
protected override void DownloadPostProcessLocalFile(string fnameLocalLong)
{
  FileInfo fi = new FileInfo(fnameLocalLong);
  UnzipFileWithPassword(fnameLocalLong, fi.DirectoryName, _password);
}


/// <summary>
/// Die Dummy-Installtion von UploadAllFiles macht einfach nichts
/// </summary>
/// <param name="uploadJob"></param>
/// <returns></returns>
public override UploadStatus UploadAllFiles(UploadStatus uploadJob)
{
  RaiseOnLogEvent(TraceLevel.Info, "CoeoTransferAgent.UploadAllFiles not activated");
  return uploadJob;
}


/// <summary>
/// Diese Methode unzipped ein File, das mit Password gezippt ist
/// </summary>
/// <param name="srcFile"></param>
/// <param name="destDirectory"></param>
/// <returns></returns>
private bool UnzipFileWithPassword(string srcFile, string destDirectory, string pwd)
{
  using (ZipFile zf = ZipFile.Read(srcFile)) {
    foreach (ZipEntry ze in zf) {
      ze.ExtractWithPassword(destDirectory, pwd);
    }
  }
  return true;
}

    
  } //end   public sealed class CoeoFileTransferAgent : FileTransferAgent


} //end namespace FileTransferCenter.Services






namespace FileTransferCenter.Framework
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.IO;
  using System.Linq;
  using WinSCP;

  /// <summary>
  /// Diese abstrakte Klasse deklariert einen FileTransferAgent,
  /// mit dessen Hilfe Files von und zu Auftraggebern geschickt werden können.
  /// Der Agent ist eigentlich protokollunabhängig, aber im Prinzip sind
  /// alle KNPF Transfers SFTP Transfers
  /// </summary>
  public class FileTransferAgent
  {
    public event EventHandler<LogEventArgs> OnLogEvent;
    protected DateTime _transactionTS;
    protected IDictionary<string, string> _configDictionary;


    /// <summary>
    /// Im ctor wird ein Config-Dictionary übergeben, der alle Werte für das Service
    /// konfiguriert. Die Daten sind an die Funktionalität gebunden und für jedes
    /// Service individuell. Der TransactionTS wird nur bei der Instanzierung vegeben 
    /// und soll die Versionierung von Directories erleichtern. So haben dann alle 
    /// Directories und Files mit Timestamp immer den gleichen Wert im Timestamp.
    /// Das erleichtert die Nachvollzihebarkeit
    /// </summary>
    /// <param name="transactionTS"></param>
    /// <param name="configDictionary"></param>
    /// <param name="onLogEventHandler"></param>
    protected FileTransferAgent(DateTime transactionTS, IDictionary<string, string> configDictionary = null,
                                          EventHandler<LogEventArgs> onLogEventHandler = null)    
    {
      _transactionTS = transactionTS;
      _configDictionary = configDictionary;
      if (onLogEventHandler != null) { OnLogEvent += onLogEventHandler; }
    }


    /// <summary>
    /// Diese Methode erledigt dann das eigentliche Absaugen der Daten und die Speicherung
    /// an der relevanten stelle. Als Ergenis wird ein Status Objekt geliefert, wo drinnen
    /// steht, was alles passiert ist.
    /// Die Default-Implementation liefert immer einen Fehler und schreibt in das Log
    /// dass sie nichts tut 
    /// </summary>
    /// <returns></returns>
    public virtual DownloadStatus DownloadAllFiles()
    {
      RaiseOnLogEvent(TraceLevel.Error, "Base:FileTransferAgent.DownloadAllFiles NOT IMPLEMENTED"); 
      DownloadStatus downloadStatus = new DownloadStatus();
      downloadStatus.ErrorCount = 1;
      downloadStatus.StatusMessages.Add("FileTransferAgent.DownloadAllFiles NOT IMPLEMENTED");
      return downloadStatus;
    }


    /// <summary>
    /// Diese Default-Methode soll den typischen Download aller Files eines Directories
    /// implementieren. Sie saugt einfach alle Files, die sich in einem Directory befinden und
    /// die von einem Prädikat akzeptiert wurden in ein lokales Directory. Dann wird für jedes
    /// File wenn ein Flag gesetzt ist, eine methode aufgerufen, die irgendetwas mit diesem
    /// File machen kann. In der Regel ist das löschen oder in ein subdirectory moven.
    /// Aber dieses Verhalten wird vererbt 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="remoteDir"></param>
    /// <param name="selectRemoteFilePredicate"></param>
    /// <param name="localDir"></param>
    /// <param name="ftds"></param>
    /// <param name="postProcessRemote"></param>
    protected virtual void DownloadProcessRemoteDirectory(WinSCP.Session session, string remoteDir, Func<RemoteFileInfo, bool> selectRemoteFilePredicate, string localDir, DownloadStatus ftds, bool postProcessRemote)
    {
      //jetzt ist die Session offen und ich kann mir die Liste der Files, die am Remote Server liegen, holen
      RemoteDirectoryInfo remoteDirInfo = session.ListDirectory(remoteDir);
      foreach (RemoteFileInfo remoteFileInfo in remoteDirInfo.Files.Where(selectRemoteFilePredicate)) {
        //jetzt ist klar, dass es files gibt. daher muss ich nun das Directory erstellen
        if (!Directory.Exists(localDir)) {
          Directory.CreateDirectory(localDir);
        }
        string fnameRemoteLong = Path.Combine(remoteDir, remoteFileInfo.Name).Replace("\\", "/");
        string fnameLocalLong = Path.Combine(localDir, remoteFileInfo.Name);
        RaiseOnLogEvent(TraceLevel.Info, $" file: {fnameRemoteLong} ==> {fnameLocalLong} ");
        try {
          session.GetFiles(fnameRemoteLong, fnameLocalLong);
          ftds.ReceivedFiles.Add(fnameLocalLong);
          DownloadPostProcessLocalFile(fnameLocalLong);
          if (postProcessRemote == true) {
            DownloadPostProcessRemoteFile(session, fnameRemoteLong);
          }
        } catch (Exception ex) {
          RaiseOnLogEvent(TraceLevel.Error, $"  Exception Get/Delete_File {fnameRemoteLong}=>{fnameLocalLong} Message: {ex.Message}");
          ftds.ErrorCount++;
        }
      } //end foreach 
    } //end GetAllFilesFromDirectory


    /// <summary>
    /// Diese Methode wird für jedes file 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="fnameRemoteLong"></param>
    protected virtual void DownloadPostProcessRemoteFile(WinSCP.Session session, string fnameRemoteLong)
    {
      return;
    }


    /// <summary>
    /// Diese Methode dient als Hook für das Post-Processing des lokalen Files.Sie wird nach dem
    /// Empfang des Files aufgerufen und kann in den vererbten Klassen überschrieben werden.
    /// </summary>
    /// <param name="fnameLocalLong"></param>
    protected virtual void DownloadPostProcessLocalFile(string fnameLocalLong)
    {
      return;
    }




    /// <summary>
    /// Diese Methode dient dazu, Files auf ein System zu schicken. Das Objekt, das 
    /// als uploadJob übergeben wird, wird beim Upload um den Status und den error
    /// count erweitert und dann als Returnwert geliefert.
    /// Die default-Implementation setzt den Status für jedes File auf false und
    /// liefert für jedes File und generell einen Error mit Error Message
    /// </summary>
    /// <param name="uploadJob"></param>
    /// <returns></returns>
    public virtual UploadStatus UploadAllFiles(UploadStatus uploadJob)
    {
      RaiseOnLogEvent(TraceLevel.Error, "FileTransferAgent.UploadAllFiles NOT IMPLEMENTED");
      uploadJob.ErrorCount++;
      uploadJob.StatusMessages.Add("FileTransferAgent.UploadAllFiles NOT IMPLEMENTED");
      foreach (string f in uploadJob.UploadFiles) {
        uploadJob.UploadFilesFailed.Add(f);
        uploadJob.ErrorCount++;
        uploadJob.StatusMessages.Add($"File: {f}  not uploaded- NOT IMPLEMENTED");
      }
      return uploadJob;
    }


    /// <summary>
    /// Diese Default-Methode soll den typischen Upload aller Files eines Directories
    /// implementieren. Sie schiebt einfach alle Files, die sich in einem Directory befinden und
    /// die von einem Prädikat akzeptiert wurden in ein remote Directory. Dann wird für jedes
    /// File wenn ein Flag gesetzt ist, eine methode aufgerufen, die irgendetwas mit diesem
    /// File machen kann. In der Regel ist das löschen oder in ein subdirectory moven.
    /// Aber dieses Verhalten wird vererbt 
    /// </summary>
    /// <param name="selectLocalFilePredicate"></param>
    /// <param name="session"></param>
    /// <param name="remoteDir"></param>
    /// <param name="localDir"></param>
    /// <param name="ftus"></param>
    /// <param name="postProcessLocal"></param>
    protected virtual void UploadProcessLocalDirectory(string localDir, Func<string, bool> selectLocalFilePredicate,
      WinSCP.Session session, string remoteDir, UploadStatus ftus, bool postProcessLocal)
    {
      string[] allLocalFileNames = Directory.GetFiles(localDir);
      foreach (string fName in allLocalFileNames) {
        //Es werden nur Files, wo das Prädikat true sagt, bearbeitet
        if (selectLocalFilePredicate(fName) == true) {
          ftus.UploadFiles.Add(fName);
          FileInfo fi = new FileInfo(fName);
          string fnameRemoteLong = Path.Combine(remoteDir, fi.Name).Replace("\\", "/");
          RaiseOnLogEvent(TraceLevel.Info, $" file: {fName} ==> {fnameRemoteLong} ");

          try {
            TransferOperationResult result = session.PutFiles(fName, fnameRemoteLong);
            if (result.IsSuccess == true) {
              ftus.UploadFilesSuccess.Add(fName);
              if (postProcessLocal == true) {
                this.UploadPostProcessLocalFile(fName);
              }
            } else {
              ftus.ErrorCount++;
              ftus.UploadFilesFailed.Add(fName);
            }
          } catch (Exception ex) {
            RaiseOnLogEvent(TraceLevel.Error, $"  Exception PutFile {fName}=>{fnameRemoteLong} Message: {ex.Message}");
            ftus.UploadFilesFailed.Add(fName);
            ftus.ErrorCount++;
          }
        }
      }
    }



    /// <summary>
    /// Diese Methode wird für jedes file nach dem Upload aufgerufen 
    /// </summary>
    /// <param name="session"></param>
    /// <param name="fnameRemoteLong"></param>
    protected virtual void UploadPostProcessRemoteFile(WinSCP.Session session, string fnameRemoteLong)
    {
      return;
    }


    /// <summary>
    /// Diese Methode dient als Hook für das Post-Processing des lokalen Files.Sie wird nach dem
    /// Empfang des Files aufgerufen und kann in den vererbten Klassen überschrieben werden.
    /// Hier wird geschaut, ob es schon ein Directory mit dem Namen Upload_JJJJ_mm_DD_hh_mm gibt
    /// und wenn ja, kommen da das file rein, wenn es directory nicht gibt, wird es erzeugt
    /// </summary>
    /// <param name="fnameLocalLong"></param>
    protected virtual void UploadPostProcessLocalFile(string fnameLocalLong)
    {
      FileInfo fi = new FileInfo(fnameLocalLong);
      string directoryPath = fi.DirectoryName;
      string tsdirname = "Upload_" + _transactionTS.ToString("yyyy_MM_dd_HH_mm");
      string uploadDirectory = Path.Combine(directoryPath, tsdirname);
      if (!Directory.Exists(uploadDirectory)) {
        Directory.CreateDirectory(uploadDirectory);
      }
      File.Move(fnameLocalLong, Path.Combine(uploadDirectory, fi.Name));

    }



    /// <summary>
    /// Das ist eine ganz wichtige Basis-Klasse, die das OnLogEvent in die Hierarchie durchreicht.
    /// Das brauche ich, da ja nur die Basisklasse Events raisen kann, aber nicht die 
    /// derived klassen. aber über die virtuelle Methode sollte das gehen. Das ist auf jeden Fall
    /// die Standard-Methode.
    /// </summary>
    /// <param name="tl"></param>
    /// <param name="msg"></param>
    protected virtual void RaiseOnLogEvent(System.Diagnostics.TraceLevel tl, string msg)
    {
      OnLogEvent?.Invoke(this, new LogEventArgs(tl, msg));
    }

    /// <summary>
    /// Dieses DTO dient als Job-Status Beschreibung eines Download-Auftrages
    /// Da diese Klasse nur im Kontext des FileTransferAgents Sinn hat, ist sie 
    /// als inner class definiert
    /// </summary>
    public class DownloadStatus
    {
      public virtual int ErrorCount { get; set; } = 0;
      public virtual IList<string> ReceivedFiles { get; private set; } = new List<string>();
      public virtual IList<string> StatusMessages { get; private set; } = new List<string>();

    } //end  public class DownloadStatus


    /// <summary>
    /// Dieses DTO dient als Job- und Statusbeschreibung eines Upload Auftrages.
    /// Da diese Klasse nur im Kontext des FileTransferAgents Sinn hat, ist sie 
    /// als inner class definiert
    /// </summary>
    public class UploadStatus
    {
      public virtual int ErrorCount { get; set; } = 0;
      public virtual IList<string> UploadFiles { get; private set; } = new List<string>();
      public virtual IList<string> UploadFilesSuccess { get; private set; } = new List<string>();
      public virtual IList<string> UploadFilesFailed { get; private set; } = new List<string>();
      public virtual IList<string> StatusMessages { get; private set; } = new List<string>();

    } //end   public class UploadStatus



  } //end   public abstract class FileTransferAgent


} //end namespace FileTransferCenter.Framework








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
    }



  } //end   public class FileSystemTools


} //end namespace BackupGenerationShaper







*/
