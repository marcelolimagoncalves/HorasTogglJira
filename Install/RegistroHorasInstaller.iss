[Setup]
; NOTE: The value of AppId uniquely identifies this application.
; Do not use the same AppId value in installers for other applications.
; (To generate a new GUID, click Tools | Generate GUID inside the IDE.)
AppId=HorasTogglJira
AppName=HorasTogglJira
AppVersion=1.0.0.0
AppVerName=HorasTogglJira
AppPublisher=Transdata Smart
AppPublisherURL=http://www.transdatasmart.com.br/
AppSupportURL=http://www.transdatasmart.com.br/
AppUpdatesURL=http://www.transdatasmart.com.br/
DisableProgramGroupPage=yes
DefaultDirName={pf}/Transdata Smart/HorasTogglJira
DefaultGroupName=Transdata Smart
OutputDir=Setup
OutputBaseFilename=HorasTogglJira
Compression=lzma
SolidCompression=yes
UsePreviousAppDir=no

[Languages]
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Files]
Source: "..\HorasTogglJiraServico\bin\Release\HorasTogglJiraServico.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\HorasTogglJiraServico\bin\Release\HorasTogglJiraServico.pdb"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\HorasTogglJiraServico\bin\Release\*.dll"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\HorasTogglJiraServico\bin\Release\*.xml"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\HorasTogglJiraServico\install.bat"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\HorasTogglJiraServico\uninstall.bat"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\HorasTogglJiraServico\*.xml"; DestDir: "{app}"; Flags: ignoreversion 
Source: "..\HorasTogglJiraServico\*.xsd"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{app}\install.bat";

[UninstallRun]
Filename: "{app}\uninstall.bat";