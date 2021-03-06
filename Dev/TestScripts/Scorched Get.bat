git --help
if "%errorlevel%"=="0" GOTO GitCommandline
:GitInstallBin
"%programfiles(x86)%\Git\bin\git.exe" -C "%~dp0..\.." reset --hard
if not %errorlevel%==0 pause & exit 1
"%programfiles(x86)%\Git\bin\git.exe" -C "%~dp0..\.." clean -xdf
if not %errorlevel%==0 pause & exit 1
if "%1"=="" ("%programfiles(x86)%\Git\bin\git.exe" -C "%~dp0..\.." pull) else ("%programfiles(x86)%\Git\bin\git.exe" -C "%~dp0..\.." checkout %1)
if not %errorlevel%==0 pause & exit 1
GOTO exit

:GitCommandline
git -C "%~dp0..\.." reset --hard
if not %errorlevel%==0 pause & exit 1
git -C "%~dp0..\.." clean -xdf
if not %errorlevel%==0 pause & exit 1
if "%1"=="" (git -C "%~dp0..\.." pull) else (git -C "%~dp0..\.." checkout %1)
if not %errorlevel%==0 pause & exit 1
:exit