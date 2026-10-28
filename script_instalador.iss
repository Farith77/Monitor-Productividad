; Inno Setup Script para Sistema Monitor Productividad

[Setup]
AppName=Monitor Productividad
AppVersion=1.0
AppPublisher=Alan Jorge / UNSA
DefaultDirName={autopf}\MonitorProductividad
DefaultGroupName=Monitor Productividad
OutputDir=.\
OutputBaseFilename=Instalador_MonitorProductividad
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible

[Files]
Source: "D:\Semestre2026A\ST\Tesis1_MT\MonitorProductividad.Agent\bin\Release\net10.0\win-x64\publish\MonitorProductividad.Agent.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Semestre2026A\ST\Tesis1_MT\MonitorProductividad.Agent\appsettings.json"; DestDir: "{app}"; Flags: ignoreversion
Source: "D:\Semestre2026A\ST\Tesis1_MT\MonitorProductividad.Watcher\bin\Release\net10.0\win-x64\publish\WatcherMP.exe"; DestDir: "{app}"; Flags: ignoreversion

[Run]
Filename: "{sys}\sc.exe"; Parameters: "create ""MonitorWatchdog"" binpath= ""{app}\WatcherMP.exe"" start= delayed-auto"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "failure ""MonitorWatchdog"" reset= 86400 actions= restart/30000/restart/30000/restart/30000"; Flags: runhidden
Filename: "{sys}\sc.exe"; Parameters: "start ""MonitorWatchdog"""; Flags: runhidden

[UninstallRun]
Filename: "{sys}\sc.exe"; Parameters: "stop ""MonitorWatchdog"""; Flags: runhidden; RunOnceId: "StopWatchdog"
Filename: "{sys}\sc.exe"; Parameters: "delete ""MonitorWatchdog"""; Flags: runhidden; RunOnceId: "DeleteWatchdog"