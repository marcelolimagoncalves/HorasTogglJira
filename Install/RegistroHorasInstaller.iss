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
Source: "..\TogglJiraConsole\bin\Release\TogglJiraConsole.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TogglJiraConsole\bin\Release\TogglJiraConsole.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TogglJiraConsole\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraConsole\bin\Release\*.xml"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraConsole\install.bat"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraConsole\uninstall.bat"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraConsole\*.xml"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\TogglJiraConsole\*.config"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TogglJiraConsole\*.xsd"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TogglJiraConsole\View\cadastro.html"; DestDir: "{app}\View"; Flags: ignoreversion

[Run]
Filename: "{app}\install.bat";

[UninstallRun]
Filename: "{app}\uninstall.bat";