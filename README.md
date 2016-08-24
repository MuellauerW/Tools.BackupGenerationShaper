# Tools.BackupGenerationShaper #

## Revision History ##

|Date       |Author |Note|
|:----------|:------|:---|
|2009-08-22 |wm     |first version of document created|
|2014-03-30 |wm     |creation of git repository |
|2015-10-06 |wm     |new name BackupGenerationShaper and new features as documented|
|2016-08-23 |wm     |protocol support for ftp added|


## Introduction ##
This tool helps keeping only the latest n backup-files. This program helps avoid overflow of backup storage and prevents data loss in case of an overflowing volume. 
To be processed, backup-files must contain a timestamp-string of predefined format in their filenames.
The Applications supports the following timestamp formats:

- yyyy_MM_dd_HH_mm_ss
- yyyy_MM_dd_HH_mm
- yyyy_MM_dd
- yyyy-MM-dd-HH-mm-ss
- yyyy-MM-dd-HH-mm
- yyyy-MM-dd

## How does the software process data ##
All directories that are enumerated in the .exe.config file (Section DirectoryList)
are scanned and enumerated files are assigned to bags according to their corresponding plain filenames without timestamp-string.
All distinct sets are sorted on timestamp and all older files that exceed the number of files to be kept are deleted. 
All activities are written to a log-file and in case of uncaught exceptions the software can send an email if configured to do so.
The software can operate on local directories, SMB-shares, and FTP remote-directories.

## Configuration ##
As this program is a .net application the configuration is done in an .config file.
To make things more elegant, I implemented a distinct configuration object hierarchy that is deserialized at startup. Configuration is done with an text-editor by manually editing
the .exe.config file. Before altering this file save to old (working) config on a
safe place so that you can revert to this version.
In order to keep private configuration private, the app.config file references an file named
ShaperConfig.private.config. This file is not part of the git repository, but a tempalte file namend ShaperConfig.template.config is included. So you only have to copy and rename this file and configure it properly.
Configuration looks as follows:

## Layout of .exe.config Options ##

- ShaperConfig
  - ShaperDebugOptions
    - IsSimulateOnly: bool
	  - IsInteractive: bool
	  - OnExceptionSendMail: bool
	  - MailReceiverList: string
	  - MailSender: string
	  - MailSmtpHostname: string
	  - MailSmtpPort: int
	  - MailSmtpUsername: string
	  - MialSmtpPassword: string
  - ShaperLogOptions
    - LogToScreen: bool
    - LogToFile: bool
	  - LoggingDirectory: string
	- FtpOptions
	  - Username: string
	  - Password: string
	  - HostName: string 
  - DirectoryList
    - EntryList
      - ProtocolName
      - DirectoryName
      - TsFormat
      - GenerationCount

