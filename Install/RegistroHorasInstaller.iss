[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId=RegistroHoras
AppName=RegistroHoras
AppVersion=1.0.0.0
AppVerName=RegistroHoras
AppPublisher=Transdata Smart
AppPublisherURL=http://www.transdatasmart.com.br/
AppSupportURL=http://www.transdatasmart.com.br/
AppUpdatesURL=http://www.transdatasmart.com.br/
DisableProgramGroupPage=yes
DefaultDirName={pf}/Transdata Smart/RegistroHoras
DefaultGroupName=Transdata Smart
OutputDir=Setup
OutputBaseFilename=RegistroHoras
Compression=lzma
SolidCompression=yes
UsePreviousAppDir=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "..\TogglJiraService\bin\Release\TogglJiraService.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TogglJiraService\*.xml"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraService\*.config"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\TogglJiraService.exe"; Parameters: "--install"

[UninstallRun]
Filename: "{app}\TogglJiraService.exe"; Parameters: "--uninstall"