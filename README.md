# MyGameServer(.NetCore)
- 게임서버_C#(.NetCore)
- C#으로 구현한 분산 게임서버

1. 장르: MMORPG 를 염두해 두고 구현 진행 중
2. 언어: C# (.NET Core 6.0) > (.Net Core 8.0 으로 업그레이드 완료)
3. 도구: Visual Studio 2022, MSSQL 사용
4. 서버구조: 서버에 대한 기능적 분산 및 중계서버로 구성된 분산 네트워크 게임서버
5. 기타
   * log4net 을 통한 log 저장 > (SeriLog 기능 추가)
   * 패킷 직렬화에 protobuf 사용
   * ObjectPool 사용 > (Microsoft.Extensions.ObjectPool 및 관련 래퍼클래스를 추가하여 사용)
   * 디자인 패턴 (싱글톤, 팩토리 등...)
   * config 작업은 json 파일로 진행
   * redis 및 webserver 기능 추가 예정
