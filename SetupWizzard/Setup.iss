; =====================================================================
; 1. PASTE YOUR FULL BUILD PATH HERE (inside the quotes)
;    Example: "C:\Users\mrdee\Documents\SlimeBalancer\Build\Release"
; =====================================================================
#define MyBuildPath "C:\Users\mrdee\OneDrive\Documenten\_MCT\containers\SlimeBalancer\SlimeBalancer\Builds\Release"

#define MyAppName "SlimeBalancer"
#define MyAppVersion "1.0"
#define MyAppPublisher "Dante Deketele"
#define MyAppURL "https://github.com/DanteDeketele/SlimeBalancer"
#define MyAppExeName "SlimeBalancer.exe"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-1234-56789ABCDEF0}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
WizardStyle=modern
OutputBaseFilename=SlimeBalancer_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startupicon"; Description: "Start SlimeBalancer automatically when Windows starts"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MyBuildPath}\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#MyBuildPath}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{userstartup}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startupicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#MyAppName}}"; Flags: nowait postinstall skipifsilent

[Code]
var
  InfoPage: TOutputMsgWizardPage;

procedure InitializeWizard;
begin
  InfoPage := CreateOutputMsgPage(wpWelcome,
    'Bluetooth Configuration', 'Important Information about Hardware Connection',
    'Please read the following instructions regarding the SlimeBalancer Board connection:');
    
  InfoPage.Msg1Label.Caption := 
    '1. Power on the ESP32 Balance Board.' + #13#10 +
    '   (LEDs will turn RED indicating it is waiting for a connection)' + #13#10 + #13#10 +
    '2. Ensure Bluetooth is enabled on this computer.' + #13#10 + #13#10 +
    '3. The game will automatically scan for the balance board using COM ports.' + #13#10 +
    '   Make sure you connect to "SlimeBalancer". Restart your system if it does not work.' + #13#10 + #13#10 +
    '4. Connection Confirmation:' + #13#10 +
    '   When connected, the board LEDs will turn TEAL/GREEN and tilt controls will activate.' + #13#10 + #13#10 +
    'For more troubleshooting, visit: https://github.com/DanteDeketele/SlimeBalancer';
end;