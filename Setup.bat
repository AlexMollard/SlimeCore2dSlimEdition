@echo off
echo Updating Git Submodules...

REM Initialize the wrapper submodule
git submodule update --init Project/Dependencies/DiligentEngine

REM Initialize specific submodules within DiligentEngine (Excluding DiligentSamples)
pushd Project\Dependencies\DiligentEngine
git submodule update --init --recursive DiligentCore DiligentTools DiligentFX
popd

if %errorlevel% neq 0 (
    echo Failed to update submodules!
    exit /b %errorlevel%
)

echo Building Diligent Engine...
call "%~dp0Project\Dependencies\BuildDiligent.bat"
if %errorlevel% neq 0 (
    echo Failed to build Diligent Engine!
    exit /b %errorlevel%
)

echo Setup Complete!
pause
