# FactoryMES Platform

FactoryMES 공통 계약과 서비스 시작 라이브러리를 **버전 패키지**로 발행하는 저장소다.
서비스 저장소는 이 소스 폴더를 프로젝트 참조하지 않고 정확한 패키지 버전을 선택한다.

- `FactoryMES.Contracts`: DTO·canonical 해시·기존 판정/고정 정책.
- `FactoryMES.Foundation`: 서비스 인증·Cookie·상태 확인·안전 로그·내부 HTTP 호출.
- 최초 기준 버전: 두 패키지 모두 `1.0.0`.

## 빌드와 패키지 생성

.NET SDK 버전은 `global.json`에 고정한다.

```powershell
pwsh -NoProfile -File build/pack.ps1
```

`artifacts/packages`에 nupkg와 SHA-256 manifest를 생성한다.
이 폴더는 Git에 커밋하지 않으며 검증한 패키지는 GitHub Release 자산으로 배포한다.
이미 배포한 버전의 자산을 다른 바이트로 덮어쓰지 않는다.

## 소비자 업그레이드

서비스의 `build/package-lock.json`에 공개 Release 저장소·태그·파일·SHA-256을 고정한다.
`build/restore-packages.ps1`은 파일을 `.packages`로 내려받아 SHA-256을 검사한다.
서비스 NuGet 설정은 이 로컬 feed를 사용하고 `PackageReference`는 `[1.0.0]`처럼 정확히 고정한다.
라이브러리 소스나 새 버전만 바뀌어도 기존 소비자 빌드·배포를 자동 유발하지 않는다.
소비자가 의도적으로 버전과 hash를 올릴 때 해당 서비스 CI가 검증한다.

## 경계와 남는 결합

현재 DTO뿐 아니라 기존 해시·판정 규칙도 패키지에 포함해 동작과 직렬화 호환을 보존했다.
배포 경계가 독립적이라는 뜻이지 공유 계약의 의미적 결합까지 없어진 것은 아니다.
업무 Backend의 DB 소유권이나 업무 영역을 이 저장소 분리만으로 바꾸지 않는다.
패키지는 실행 설정·인증서·비밀 파일을 포함하지 않는다. 소비자는 런타임에 비밀을 공급한다.

저장소 공개는 별도 소프트웨어 이용 허락을 자동으로 뜻하지 않는다.
프로젝트 자체의 라이선스 선택은 소유자가 결정한다. 사용한 외부 패키지 라이선스는 별도다.
