@echo off
chcp 65001 > nul
cd /d %~dp0

:: 定义路径
set PROTO_DIR=./Protocol
set OUT_DIR=../Game.Shared/GenProtoCodes
:: 替换为你的 NuGet 官方文件路径
set GOOGLE_PROTO_PATH=C:\Users\xuzhihao06\.nuget\packages\google.protobuf.tools\3.33.5\tools
set PROTOC_PATH=C:\Users\xuzhihao06\.nuget\packages\google.protobuf.tools\3.33.5\tools\windows_x64\protoc.exe

:: 执行编译（多路径用 ; 分隔）
%PROTOC_PATH% -I=. -I=%GOOGLE_PROTO_PATH% --csharp_out=%OUT_DIR% %PROTO_DIR%/*.proto

echo 生成成功！
pause