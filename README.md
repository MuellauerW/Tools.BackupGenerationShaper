# Tools.BackupGenerationShaper #

## Revision History ##

<table>
  <tr> <th >Date</th>             <th>Author</th>  <th>Note</th>  </tr>
  <tr> <td nowrap>2009-08-22</td>  <td>wm</td>  <td>first version of document created</td> </tr>
  <tr> <td nowrap>2014-03-30</td> <td>	wm</td>	 <td>creation of git repository</td> </tr>
  <tr> <td nowrap>2015-10-06</td> <td>wm</td>		<td>new name BackupGenerationShaper and neuw features as documented</td>  </tr>

</table>

## Introduction ##
This tool helps keeping only the youngest n backup-files to avoid overflow of backup storage. For beging operated backup-files must contain a timestamp of defined format in their filename.
The Applications supports the following timestamp formats:

- yyyy_MM_dd_hh_mm_ss
- yyyy_MM_dd_hh_mm
- yyyy_MM_dd
- yyyy-MM-dd-hh-mm-ss
- yyyy-MM-dd-hh-mm
- yyyy-MM-dd

## How does the software do its job ##
All directories that are enumerated in the .exe.config file (Section ShaperDirectires)
are scanned and files are assigned to sets according to their filenames without timestamp.
All distinct sets are sorted on timestamp and all older files that exced the number of files to be kept are deleted. 
All activities are written to a log-file and in case of uncaught exceptions the software can send an email if configured to do so.

## Configuration ##
As this program is a .net application the configuration is done in an .exe.config file.
To make things more elegant, I implemented a distinct configuration object hierarchy that is deserialized at startup. Configuration is done with an editor by manually editing
the .exe.config file. Before altering this file save to old (working) config on a
safe place so that you can revert to this version.
Configuration looks as follows:

## Layout of .exe.config Options ##

- ShaperOptions
  - ShaperDebugOptions
    - IsSimulateOnly: bool
	  - IsInteractive: bool
	  - OnExceptionSendMail: bool
	  - MailTo: string
	  - MailFrom: string
	  - MailSMTP: string
  - ShaperLogOptions
    - LogToScreen: bool
    - LogToFile: bool
	  - LoggingDirectory: string
  - ShaperDirectories
    - EntryList
      - DirectoryName
      - TimestampRegEx
      - GenerationCount

