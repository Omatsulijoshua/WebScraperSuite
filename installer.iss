; Script generated for Inno Setup 6
#define MyAppName "WebScraper Pro"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Omatsulijoshua"
#define MyAppURL "https://github.com/Omatsulijoshua/WebScraperSuite"
#define MyAppExeName "WebScraperPro.exe"
#define SourceDir "C:\Users\Joshua\Downloads\WebScraperPro"

[Setup]
; App info
AppId={{D1A2E3B4-5C6D-7E8F-9A0B-1C2D3E4F5A6B}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}/releases
DefaultDirName={autopf}\WebScraperSuite
DisableProgramGroupPage=yes
DefaultGroupName={#MyAppName}
OutputDir=C:\Users\Joshua\Downloads
OutputBaseFilename=WebScraperSuite-Setup-v1.0.0
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Visual Element Picker"; Filename: "{app}\Scraper.Picker.WinForms.exe"
Name: "{group}\Media Scraper"; Filename: "{app}\Scraper.Media.WinForms.exe"
Name: "{group}\Website Cloner"; Filename: "{app}\Scraper.SiteCloner.WinForms.exe"
Name: "{group}\Automatic Scraper"; Filename: "{app}\Scraper.Automatic.WinForms.exe"
Name: "{group}\{cm:UninstallProgram,{#MyAppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
