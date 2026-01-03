@echo off
echo Updating Git Submodules...
git submodule update --init --recursive
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
