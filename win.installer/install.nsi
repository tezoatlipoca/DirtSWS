; Basic installer script for DirtSWS
!include "MUI2.nsh"
!include "VersionCompare.nsh"

; Update ALL version references (change these for each version)
!define VERSION "0.2.0"

; General Settings  
Name "DirtSWS"
OutFile "DirtSWS-${VERSION}-setup.exe"
InstallDir "$PROGRAMFILES\DirtSWS"
InstallDirRegKey HKCU "Software\DirtSWS" ""
RequestExecutionLevel admin

; Version Information
VIProductVersion "${VERSION}.0"
VIAddVersionKey "ProductName" "DirtSWS"
VIAddVersionKey "FileDescription" "Dirt Simple Web Server"
VIAddVersionKey "LegalCopyright" "© tezoatlipoca@gmail.com"
VIAddVersionKey "FileVersion" "${VERSION}"

; Interface Settings
!define MUI_ABORTWARNING
!define MUI_ICON "dirtsws.ico" ; Optional: Replace with your icon path

; Pages
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

; Languages
!insertmacro MUI_LANGUAGE "English"

; Installer Section
Section "Install"
  SetOutPath "$INSTDIR"

  ; Add files
  File /oname=DirtSWS.exe "..\bin\Release\net8.0\win-x64\publish\DirtSWS.exe"
  File "..\bin\Release\net8.0\win-x64\publish\dirt_default_icon.jpg"
  File "..\bin\Release\net8.0\win-x64\publish\dirt_default.css"

  ; Check for existing version in registry
  ReadRegStr $R0 HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "DisplayVersion"
  StrCpy $R1 "${VERSION}" ; This installer's version

  ${If} $R0 != ""
    ${VersionCompare} $R0 $R1 $R2
    ${If} $R2 == "1" ; R0 > R1 (installed version is newer)
      MessageBox MB_ICONSTOP "A newer version ($R0) of DirtSWS is already installed.$\nCurrent installer: $R1$\nInstaller will now close."
      Quit
    ${ElseIf} $R2 == "0" ; R0 == R1 (same version)
      MessageBox MB_ICONQUESTION|MB_YESNO "DirtSWS version $R0 is already installed. Do you want to reinstall?" IDNO quit_installer
    ${Else}
      MessageBox MB_ICONQUESTION|MB_YESNO "Upgrading DirtSWS from version $R0 to ${VERSION}. Continue?" IDNO quit_installer
    ${EndIf}
  ${EndIf}

  Goto continue_install
  
  quit_installer:
    Quit
  
  continue_install:

  ; Create Start Menu shortcuts
  CreateDirectory "$SMPROGRAMS\DirtSWS"
  CreateShortcut "$SMPROGRAMS\DirtSWS\DirtSWS.lnk" "$INSTDIR\DirtSWS.exe"
  CreateShortcut "$SMPROGRAMS\DirtSWS\Uninstall.lnk" "$INSTDIR\uninstall.exe"

  ; Create uninstaller
  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; Write uninstall info to registry
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "DisplayName" "DirtSWS"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "UninstallString" "$\"$INSTDIR\uninstall.exe$\""
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "DisplayVersion" "${VERSION}"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "Publisher" "tezoatlipoca"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS" "InstallLocation" "$INSTDIR"

SectionEnd

; Uninstaller Section
Section "Uninstall"
  ; Remove files and folders
  Delete "$INSTDIR\DirtSWS.exe"
  Delete "$INSTDIR\dirt_default_icon.jpg"
  Delete "$INSTDIR\dirt_default.css"
  Delete "$INSTDIR\uninstall.exe"
  
  ; Remove shortcuts
  Delete "$SMPROGRAMS\DirtSWS\DirtSWS.lnk"
  Delete "$SMPROGRAMS\DirtSWS\Uninstall.lnk"
  RMDir "$SMPROGRAMS\DirtSWS"
  
  ; Remove directories
  RMDir "$INSTDIR"
  
  ; Remove registry key
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\DirtSWS"
SectionEnd