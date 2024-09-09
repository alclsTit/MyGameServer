SET PROTOC_PATH=.\
SET SOURCE_PATH=..\proto\csharp
SET OUT_PATH=..\..\MyGameServer\ServerEngine\Protocol\output

CALL protoc.exe -I=%SOURCE_PATH% --csharp_out=%OUT_PATH% %SOURCE_PATH%\client_message.proto
CALL protoc.exe -I=%SOURCE_PATH% --csharp_out=%OUT_PATH% %SOURCE_PATH%\server_message.proto

pause