cd /d "C:\Dev\KernelExplorer\KExplore" &msbuild "KExplore.vcxproj" /t:sdvViewer /p:configuration="Debug" /p:platform=x64
exit %errorlevel% 