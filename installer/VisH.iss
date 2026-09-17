#define MyAppName "VisH"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Niklas"
#define MyAppExeName "VisH.exe"

[Setup]
AppId={{B7E5A9D2-6C31-4E7D-9F52-1A8C4D3B6E90}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}

DefaultDirName={autopf}\VisH
DefaultGroupName=VisH

OutputDir=.
OutputBaseFilename=VisH-Setup

Compression=lzma
SolidCompression=yes

ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "..\publish\*"; \
    DestDir: "{app}"; \
    Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\VisH"; \
    Filename: "{app}\{#MyAppExeName}"

Name: "{autodesktop}\VisH"; \
    Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; \
    Description: "VisH starten"; \
    Flags: nowait postinstall skipifsilent