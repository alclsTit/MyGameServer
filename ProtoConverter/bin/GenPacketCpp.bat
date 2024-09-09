SET PROTOC_PATH=.\
SET SOURCE_PATH=..\proto\cpp
SET OUT_PATH=..\..\ProtoBufferClient\protocol

CALL protoc.exe -I=%SOURCE_PATH% --cpp_out=%OUT_PATH% %SOURCE_PATH%\login.proto

pause