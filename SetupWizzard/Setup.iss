; Script generated for SlimeBalancer

; ==================================================================================
; PATH SETTINGS
; 1. Update MyBuildPath to where your game files are.
; 2. Update MyAssetsPath to where your images (icon/banner) are.
; ==================================================================================
#define MyBuildPath "C:\Users\mrdee\OneDrive\Documenten\_MCT\containers\SlimeBalancer\SlimeBalancer\Builds\Release"

#define MyAppName "SlimeBalancer"
#define MyAppVersion "1.0"
#define MyAppPublisher "Dante Deketele"
#define MyAppURL "https://github.com/DanteDeketele/SlimeBalancer"
#define MyAppExeName "SlimeBalancer.exe"
#define MyAppId "{{A1B2C3D4-E5F6-7890-1234-56789ABCDEF0}"

[Setup]
AppId={#MyAppId}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableProgramGroupPage=yes
; We enable the welcome page to show off your Banner Image
DisableWelcomePage=no
WizardStyle=modern
OutputBaseFilename=SlimeBalancer_Setup
Compression=lzma
SolidCompression=yes
PrivilegesRequired=admin
CloseApplications=force

; --- CUSTOM STYLING ---
; The icon for the installer file and top-left corner
SetupIconFile=logoIcon.ico
; The large image on the left side of the Welcome Page (Use the City/Board image)
WizardImageFile=sidebarImage.bmp
; The small image in the top right of other pages
WizardSmallImageFile=logoIcon.bmp

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
  ModePage: TInputOptionWizardPage;
  BluetoothPage: TOutputMsgMemoWizardPage;

// -----------------------------------------------------------------------------
// HELPER: FIND UNINSTALLER
// -----------------------------------------------------------------------------
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#MyAppId}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

// -----------------------------------------------------------------------------
// HELPER: RUN UNINSTALLER
// -----------------------------------------------------------------------------
procedure RunUninstaller(Silent: Boolean);
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then
  begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Silent then
      Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode)
    else
      Exec(sUnInstallString, '', '', SW_SHOW, ewWaitUntilTerminated, iResultCode);
  end
  else
  begin
    MsgBox('No previous version was found to uninstall.', mbInformation, MB_OK);
  end;
end;

// -----------------------------------------------------------------------------
// INITIALIZE WIZARD PAGES
// -----------------------------------------------------------------------------
procedure InitializeWizard;
begin
  // 1. Create the "Choose Mode" Page (Install vs Uninstall)
  ModePage := CreateInputOptionPage(wpWelcome,
    'Installation Mode', 'Choose how you want to setup SlimeBalancer',
    'Please select an operation below:',
    True, False);
  
  // Add items to the radio button list
  ModePage.Add('Install / Update SlimeBalancer (Standard)');
  ModePage.Add('Clean Install (Uninstall old version first)');
  ModePage.Add('Uninstall SlimeBalancer Only');
  
  // Select the first option by default
  ModePage.SelectedValueIndex := 0;

  // 2. Create the Bluetooth Info Page (Using correct MsgMemo type)
  BluetoothPage := CreateOutputMsgMemoPage(ModePage.ID,
    'Bluetooth Configuration', 'Important Hardware Instructions',
    'Please read the instructions below regarding the ESP32 connection:',
    '');

  BluetoothPage.RichEditViewer.Text := 
    '1. Power on the ESP32 Balance Board.' + #13#10 +
    '   (LEDs will turn RED indicating it is waiting for a connection)' + #13#10 + #13#10 +
    '2. Ensure Bluetooth is enabled on this computer.' + #13#10 + #13#10 +
    '3. The game will automatically scan for COM ports to communicate with the balance board.' + #13#10 +
    '   Make sure you connect to "SlimeBalancer". Restart your system if you cannot connect.' + #13#10 + #13#10 +
    '4. Connection Confirmation:' + #13#10 +
    '   When connected, the board LEDs will turn TEAL/GREEN.' + #13#10 + #13#10 +
    '-------------------------------------------------------' + #13#10 +
    'Troubleshooting: https://github.com/DanteDeketele/SlimeBalancer';
end;

// -----------------------------------------------------------------------------
// HANDLE BUTTON CLICKS (LOGIC)
// -----------------------------------------------------------------------------
function NextButtonClick(CurPageID: Integer): Boolean;
begin
  Result := True;

  // If we are on the Mode Selection Page...
  if CurPageID = ModePage.ID then
  begin
    // Option 1: Clean Install (Index 1)
    if ModePage.SelectedValueIndex = 1 then
    begin
       RunUninstaller(True); // Run silent uninstall, then proceed to install
    end
    
    // Option 2: Uninstall Only (Index 2)
    else if ModePage.SelectedValueIndex = 2 then
    begin
       RunUninstaller(False); // Run normal uninstall
       Result := False;       // Stop the wizard
       PostMessage(WizardForm.Handle, $0010, 0, 0); // Close the installer application
    end;
  end;
end;

// -----------------------------------------------------------------------------
// SKIP PAGES IF UNINSTALLING
// -----------------------------------------------------------------------------
function ShouldSkipPage(PageID: Integer): Boolean;
begin
  Result := False;
  // If user selected "Uninstall Only", skip everything else
  if (ModePage.SelectedValueIndex = 2) and (PageID <> ModePage.ID) then
    Result := True;
end;