[Setup]
AppName=Bit
AppVersion=1.0.0
OutputDir=.
OutputBaseFilename=SetupBit
UsePreviousAppDir=false
UsePreviousGroup=false
DefaultDirName={pf64}\Gosub\Bit
DefaultGroupName=Bit
AppPublisher=Gosub Software
UninstallDisplayName=Bit
UninstallDisplayIcon={app}\Bit.exe
LicenseFile=License.txt

[Files]
Source: "Bit.exe"; DestDir: "{app}"; flags:ignoreversion

[Icons]
Name: "{group}\Bit"; Filename: "{app}\Bit.exe"
Name: "{group}\Uninstall Bit"; Filename: "{uninstallexe}"

[Run]
FileName: "{app}\Bit.exe"; Flags: Postinstall
