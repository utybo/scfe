!include "MUI2.nsh"
!define UNINST_KEY \
  "Software\Microsoft\Windows\CurrentVersion\Uninstall\SCFE"
!define MUI_ICON "SCFE/SCFE/scfelogo.ico"
ManifestSupportedOS Win10
Name "SCFE"
Caption "Salamanders' Console File Explorer"
OutFile "scfe-installer.exe"
RequestExecutionLevel admin

InstallDir "$PROGRAMFILES64\SCFE"

AutoCloseWindow false
ShowInstDetails show

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_WELCOME
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH


!insertmacro MUI_LANGUAGE "English"


Section "SCFE"
  ; Mark this as compulsory
  SectionIn RO

  SetOutPath "$INSTDIR"
  ; Add files
  File /r "SCFE\SCFE\bin\Release\netcoreapp3.0\win-x64\publish\*"

  ; Write registery stuff about the uninstaller and write the uninstaller itself
  WriteRegStr HKLM "${UNINST_KEY}" "DisplayName" "SCFE"
  WriteRegStr HKLM "${UNINST_KEY}" "Publisher" "Salamanders' Lab"
  WriteRegStr HKLM "${UNINST_KEY}" "UninstallString" '"$INSTDIR\uninstall.exe"'
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoModify" 1
  WriteRegDWORD HKLM "${UNINST_KEY}" "NoRepair" 1
  WriteUninstaller "$INSTDIR\uninstall.exe"

  ; Add shortcuts
  CreateDirectory "$SMPROGRAMS\SCFE"
  CreateShortCut "$SMPROGRAMS\SCFE\SCFE.lnk" "$INSTDIR\SCFE.exe" \
    "%userprofile%" "$INSTDIR\SCFE.exe" 0

  CreateShortCut "$SMPROGRAMS\SCFE\Uninstall SCFE.lnk" "$INSTDIR\uninstall.exe" \
    "" "$INSTDIR\uninstall.exe" 0
SectionEnd

Section "Uninstall"
  DeleteRegKey HKLM "${UNINST_KEY}"

  RMDir /r /REBOOTOK "$INSTDIR"
  RMDir /r /REBOOTOK "$SMPROGRAMS\SCFE"
SectionEnd
