#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif

#define AppName      "Cassa Eventi AI"
#define AppPublisher "MarcoGazzola.com"
#define AppExeName   "CassaEventiAI.exe"
#define AppId        "3F8A1C6E-9B2D-4E7A-8F0C-2D5E9A3B6F1C"

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
Compression=lzma2/ultra
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
Name: "desktopicon"; Description: "Crea icona sul Desktop"; GroupDescription: "Icone aggiuntive:"

[Files]
Source: "..\publish\{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}";              Filename: "{app}\{#AppExeName}"
Name: "{group}\Disinstalla {#AppName}";  Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";        Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Avvia {#AppName}"; Flags: nowait postinstall skipifsilent

[Code]
const
  DotNetDesktopRuntimeKey = 'SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App\10.0';
  DotNetInstallUrl = 'https://aka.ms/dotnet/10.0/windowsdesktop-runtime-win-x64-installer';

function URLDownloadToFile(dwReserved: Cardinal; szURL, szFileName: String; dwReserved2: Cardinal; lpfnCB: Pointer): HRESULT;
  external 'URLDownloadToFileA@urlmon.dll stdcall';

function IsDotNet10Installed(): Boolean;
begin
  Result := RegKeyExists(HKLM64, DotNetDesktopRuntimeKey) or RegKeyExists(HKLM32, DotNetDesktopRuntimeKey) or RegKeyExists(HKLM, DotNetDesktopRuntimeKey);
end;

function DownloadFile(const Url, Dest: string): Boolean;
var
  hr: HRESULT;
begin
  hr := URLDownloadToFile(0, Url, Dest, 0, 0);
  Result := hr = 0;
end;

procedure InstallDotNet10();
var
  TempFile: string;
  ResultCode: Integer;
begin
  TempFile := ExpandConstant('{tmp}\\dotnet-windowsdesktop-runtime-10.0.exe');
  if not DownloadFile(DotNetInstallUrl, TempFile) then
  begin
    MsgBox('Impossibile scaricare il runtime .NET 10. Verifica la connessione internet e riprova.', mbError, MB_OK);
    Exit;
  end;
  if not Exec(TempFile, '/install /quiet /norestart', '', SW_SHOW, ewWaitUntilTerminated, ResultCode) then
  begin
    MsgBox('Impossibile avviare l''installer di .NET 10.', mbError, MB_OK);
    Exit;
  end;
  if ResultCode <> 0 then
    MsgBox('L''installazione di .NET 10 non è riuscita. Codice di uscita: ' + IntToStr(ResultCode), mbError, MB_OK);
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if not IsDotNet10Installed() then
  begin
    if MsgBox('Il runtime .NET 10 non risulta installato sul sistema. Vuoi scaricarlo e installarlo automaticamente prima di procedere?', mbConfirmation, MB_YESNO) = IDYES then
      InstallDotNet10();
  end;
end;
