# 버전 패키지 경계의 판단

## 제거한 빌드 결합

공통 소스 프로젝트를 각 서비스가 직접 참조하던 구조에서, 서비스가 선택한 immutable nupkg를
정확 버전과 SHA-256으로 소비하도록 바꿨다. 라이브러리의 새 커밋만으로 소비자를 다시 빌드하지 않는다.
동일 버전 패키지를 교체하지 않고 소비자가 버전/manifest를 올려 변경을 검증한다.

## 공개 Release 자산을 선택한 이유

NuGet feed를 GitHub Packages에 둘 수도 있지만 공개 NuGet package의 인증·권한 설정이
소비자 빌드의 선행 조건이 될 수 있다. 현재 포트폴리오에서는 공개 GitHub Release 자산을
SHA-256으로 고정해 내려받고 repo-local feed로 복원한다. 토큰을 소스나 Docker build에
넘기지 않고도 공개 clone이 가능하며, 최초 로컬 검증은 같은 파일을 feed에 seed할 수 있다.
라이브러리 binary를 Git 소스 이력에 반복 커밋하지 않는다.

## 정확히 무엇이 고정되는가

- csproj의 `Version`은 각 라이브러리별이다. 소비자의 FactoryMES package reference는 정확 버전이다.
- `build/release-manifest.v1.0.0.json`은 최초 실제 Release 자산 바이트의 SHA-256 기록이다.
- `pack.ps1`은 현재 프로젝트 버전을 pack한다. 선택한 패키지만 pack할 수도 있다.
- nupkg zip의 timestamp·컴파일 source revision 등으로 새 CI pack 바이트가 달라질 수 있다.
  모든 환경에서 새로 pack한 zip이 bit-for-bit 같다고 주장하지 않는다. 소비자에게 배포된
  Release 자산 바이트와 그 manifest를 권위로 삼는다.
- `publish-release.ps1`은 기존 Release를 거절하고 `--clobber`를 사용하지 않는다.
- CI는 pack/metadata 검증만 하고 artifact 저장소에 자동 업로드하지 않는다.
  공개 Release 배포는 기존 source tag를 지정한 명시적 workflow dispatch다.

## 유지한 의미적 결합

Contracts에 기존 DTO뿐 아니라 canonical 해시·고정 정책·판정 규칙도 있다.
이를 이번 리팩토링에서 급히 바꾸지 않아 wire/hash/판정 호환을 보존했다.
Source byte parity와 실제 소비자 회귀를 함께 증거로 삼는다.
Package pinning은 빌드 결합을 줄이지만 계약의 의미·호환성 검토까지 없애지는 않는다.

Foundation의 공개 framework reference는 Microsoft.AspNetCore.App이며 인증 패키지는
Microsoft.AspNetCore.Authentication.JwtBearer `[10.0.15]`다. 런타임 비밀은 포함하지 않는다.
Root 경로를 PathMap으로 치환하고 package에 source/symbol/config/인증서를 넣지 않는다.

## 실제 검증

`docs/baseline-source-parity.json`은 원본 C#과 내보낸 C#의 byte hash가 같은지 기록한다.
`build/verify-packages.ps1`은 실제 zip의 SHA/ID/version/DLL/설정·키 형태 파일 제외/
repository metadata/framework/dependency를 12개 조건으로 검사했다.
Native pack은 두 nupkg를 생성했다. Foundation 최초 복원에는 sandbox의 NuGet audit 접속 실패로
NU1900 warning이 있었으며, 이를 0경고 빌드라고 기록하지 않는다. 네트워크 가능한 호스트의
locked restore/build 및 모든 소비자 검증은 총괄이 추가 확인한다.

공식 CI action readme에서 확인한 현재 예시는 checkout v7/setup-dotnet v6이다.
버전 태그 예시는 서비스 환경과 GitHub hosted runner에서 다시 검증한다.

- [checkout](https://github.com/actions/checkout)
- [setup-dotnet](https://github.com/actions/setup-dotnet)
