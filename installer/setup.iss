#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName      "CPL Cassa Eventi"
#define AppPublisher "MarcoGazzola.com"
#define AppExeName   "CassaEventiAI.exe"
#define AppId        "{3F8A1C6E-9B2D-4E7A-8F0C-2D5E9A3B6F1C}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://github.com/marcogazzola/CassaEventiAI
AppSupportURL=https://github.com/marcogazzola/CassaEventiAI/issues
DefaultDirName={localappdata}\Programs\CassaEventiAI
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=Output
OutputBaseFilename=CassaEventiAI_Setup_{#AppVersion}
SetupIconFile=..\src\Resources\cassa.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
CloseApplications=yes
RestartApplications=no
UninstallDisplayIcon={app}\{#AppExeName}
VersionInfoVersion={#AppVersion}.0
VersionInfoProductName={#AppName}
VersionInfoCompany={#AppPublisher}

[Languages]
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"

[Tasks]
Name: "desktopicon"; Description: "Crea icona sul {cm:DesktopName}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExeName}"
Name: "{group}\Disinstalla {#AppName}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Avvia {#AppName}"; Flags: nowait postinstall skipifsilent
