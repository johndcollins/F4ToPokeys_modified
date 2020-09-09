[InnoIDE_Settings]
UseRelativePaths=true
#define VersionMajor
#define VersionMinor
#define VersionRev
#define VersionBuild
#expr ParseVersion("..\F4ToPokeys\bin\Release\F4ToPokeys.exe", VersionMajor, VersionMinor, VersionRev, VersionBuild)
#define AppVersion Str(VersionMajor) + "." + Str(VersionMinor) + "." + Str(VersionRev)

[Setup]
AppVersion={#AppVersion}
AppCopyright=Michael Minault
AppName=F4ToPokeys
AppPublisher=Michael Minault
AppPublisherURL=https://bitbucket.org/minaultm/f4topokeys
DefaultDirName={pf}\F4ToPokeys
DefaultGroupName=F4ToPokeys
DisableDirPage=auto
DisableProgramGroupPage=auto
LicenseFile=..\License.txt
OutputBaseFilename=Setup_F4ToPokeys_{#AppVersion}
SolidCompression=true
AppMutex=F4ToPokeys

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "..\F4ToPokeys\bin\Release\F4SharedMem.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\F4SharedMem.pdb"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\F4ToPokeys.exe"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\F4ToPokeys.exe.config"; DestDir: "{app}"
Source: "..\F4ToPokeys\bin\Release\F4ToPokeys.pdb"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\Hardcodet.Wpf.TaskbarNotification.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\Hardcodet.Wpf.TaskbarNotification.pdb"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\PoKeysDevice_DLL.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\SimplifiedCommon.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\SimplifiedCommon.pdb"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\UsbWrapper.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\Usc.dll"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\Usc.pdb"; DestDir: "{app}"; Flags: IgnoreVersion
Source: "..\F4ToPokeys\bin\Release\fr\F4ToPokeys.resources.dll"; DestDir: "{app}\fr"; Flags: IgnoreVersion; Languages: french

[Icons]
Name: {group}\F4ToPokeys; Filename: {app}\F4ToPokeys.exe; WorkingDir: {app}; 
Name: "{group}\{cm:UninstallProgram, F4ToPokeys}"; Filename: {uninstallexe}; 
Name: {commondesktop}\F4ToPokeys; Filename: {app}\F4ToPokeys.exe; WorkingDir: {app}; Tasks: desktopicon; 

[Run]
Filename: "{app}\F4ToPokeys.exe"; Flags: nowait postinstall skipifsilent Unchecked; Description: "{cm:LaunchProgram,F4ToPokeys}"

[ThirdParty]
CompileLogMethod=append
