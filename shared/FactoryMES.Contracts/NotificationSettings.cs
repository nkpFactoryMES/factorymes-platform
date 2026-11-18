namespace FactoryMES.Contracts;

// 관리자 사용자 선택 목록과 설정 상세에 필요한 식별자·역할만 전달한다.
// 이메일 주소와 외부 목적지는 노출하지 않고 서버의 기존 수신 범위 정책을 따른다.
public sealed record NotificationSettingUserView(
    string UserId,
    string Login,
    string Name,
    string[] Roles,
    bool Active
);

// 이벤트별 채널 모드와 낙관적 잠금 버전을 API에서 화면으로 전달한다.
// 아직 저장하지 않은 설정은 Version=0, OFF/OFF, UpdatedAtUtc=null로 표현한다.
public sealed record NotificationSettingView(
    string EventKind,
    string EmailMode,
    string ChatMode,
    long Version,
    DateTime? UpdatedAtUtc
);

// 서버가 역할별로 허용한 이벤트 설정과 대상 사용자를 한 응답으로 반환한다.
// 관리자는 여섯 이벤트, 작업자는 경보 네 이벤트를 조회한다.
public sealed record NotificationSettingsView(
    NotificationSettingUserView User,
    NotificationSettingView[] Settings
);

// 화면이 확인한 버전·선택 모드·변경 사유를 관리자 변경 API로 보낸다.
// 서버는 권한과 모드를 검증하고 ExpectedVersion이 현재 값과 다르면 충돌로 거절한다.
public sealed record NotificationSettingUpdate(
    long ExpectedVersion,
    string EmailMode,
    string ChatMode,
    string Reason
);
