@echo off
pushd %~dp0\DiligentEngine
if not exist Build mkdir Build
cd Build
echo Configuring Diligent Engine...
cmake .. -A x64 -DCMAKE_INSTALL_PREFIX=../Install -DDILIGENT_BUILD_SAMPLES=OFF -DDILIGENT_BUILD_TOOLS=ON -DDILIGENT_NO_GLSLANG=OFF
if %errorlevel% neq 0 exit /b %errorlevel%

echo Building Debug...
cmake --build . --config Debug --parallel
if %errorlevel% neq 0 exit /b %errorlevel%
echo Installing Debug...
cmake --install . --config Debug
if %errorlevel% neq 0 exit /b %errorlevel%

echo Building Release...
cmake --build . --config Release --parallel
if %errorlevel% neq 0 exit /b %errorlevel%
echo Installing Release...
cmake --install . --config Release
if %errorlevel% neq 0 exit /b %errorlevel%

popd
echo Diligent Engine Build and Install Complete.
