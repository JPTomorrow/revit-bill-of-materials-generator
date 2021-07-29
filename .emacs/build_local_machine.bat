@echo off

set compiler_path="C:\Program Files (x86)\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
set project_file="Y:\Marathon_Program_Source\Revit_C#\wirebomlite\Build.csproj"
set build=/t:Build
set config=/p:Configuration=Release
set platform=/p:Platform="AnyCPU"
set prop=/property:GenerateFullPaths=true
set il_merge=/t:ILMerge
set verb=/verbosity:quiet

%compiler_path% %project_file% %build% %config% %platform% %prop%
%compiler_path% %project_file% %il_merge% %config% %platform% %prop%






