# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 저장소에서 작업할 때 참고하는 가이드입니다.

## 게임 개요

- **게임명**: 선잠
- **장르**: 2D 러닝 액션 (로그라이크 강화 포함)
- **컨셉**: 악몽 속에서 몰려오는 몬스터/장애물을 피하고 부수며 버티는 게임.
- **연출**: 화면 고정 + 배경 패럴랙스 스크롤로 달리는 느낌을 냄. 실제로는 배경이 움직이고 플레이어는 화면 안에서 좌우 이동 + 점프로만 움직임.
- **게임오버**: "잠에서 깸"으로 표현. 재화 **꿈의 파편**을 모아 상점에서 영구 강화 후 재도전하는 메타 진행 구조.

### 모드 2개
1. **무한 모드**: 점수 획득 + 꿈의 파편 파밍.
2. **엔딩 스테이지**: 강화 4종 모두 1강 이상일 때 해금. 파편 드랍 없음. 끝까지 도달하면 엔딩 컷씬 재생.

## 강화 시스템 (4종)

`GameManager`에 레벨 필드가 이미 구현되어 있음.

| 강화 | 필드명 | 최대 레벨 | 효과 |
|---|---|---|---|
| 파괴(돌진) | `destructionLevel` | 무제한 | 강화 레벨에 비례하는 시간 동안 전방 돌진. 돌진 중 닿는 장애물 전부 파괴. 쿨타임은 고정. |
| 단련 | `fortitudeLevel` | 5 | 레벨만큼 최대 하트 증가 (기본 3칸 → 최대 8칸). |
| 재생 | `regenLevel` | 5 | `(100 ÷ 레벨)`초마다 하트 1칸 회복. 최대 하트 초과 불가. |
| 도약 | `leapLevel` | 1 | 2단 점프 해금. `GameManager.canDoubleJump` 프로퍼티로 판정. |

## 체력 규칙

- 체력은 하트 칸 단위.
- 피격 시 하트 1칸 감소 + 약 1.5초 무적(깜빡임 연출).
- 하트 0이 되면 즉시 "잠에서 깸" (게임오버) 처리.

## 기존 코드 구조 — 반드시 이 패턴을 따를 것

- **`Singleton<T>`**: 모든 매니저의 베이스 클래스. `DontDestroyOnLoad` 적용, 애플리케이션 종료 시 `Instance`는 null 반환. **새 매니저는 반드시 이걸 상속.**
- **`GameManager`**: `GameState`(Ready/Playing/Pause/GameOver) + `OnStateChanged` 이벤트로 상태 변화를 브로드캐스트. 점수/최고점수(`PlayerPrefs` 저장)/`money`(꿈의 파편)/강화 레벨 보유. `playerDamage`/`maxPlayerHp`/`canDoubleJump` 등 계산 프로퍼티 제공. **모든 상태 판정은 `GameManager.GameState` 기준으로 할 것.**
- **`PoolManager`**: `SpawnFromPool`로 오브젝트 풀링. **몬스터/장애물/파편/이펙트는 전부 풀링으로 생성할 것. `Instantiate` 직접 호출 금지.**
- **`SoundManager`**: 이름 기반 SFX 재생 (`PlaySFX(string clipName)`).
- **`UIManager`**: 패널 전환 + 페이드 담당. 코루틴은 `Time.timeScale` 영향 안 받도록 `Time.unscaledDeltaTime` 사용.
- **`MoveBackground`**: 배경 무한 스크롤 기존 구현. 패럴랙스 연출의 기반.

## 작업 규칙

- 한 번에 요청받은 기능만 구현한다. 요청 범위 밖 리팩터링 금지.
- 새 기능은 기존 매니저/이벤트 구조에 연결한다 (새 매니저 남발 금지, `GameManager.OnStateChanged` 등 기존 이벤트 재사용).
- 상태 판정은 항상 `GameManager.GameState` 기준으로 한다.
- 커밋 메시지는 한국어로 간결하게 작성한다.

## 알려진 이슈 (수정하지 말고 인지만 할 것)

- `GameManager.Start()`의 `money = 5000`은 테스트용 하드코딩. 출시 전 제거 대상이나 별도 요청 없이 임의로 고치지 말 것.
- `ShopUIManager`의 `leapLevelText`가 `Lv.0/1`로 하드코딩되어 있음 (실제 `leapLevel` 값 미참조).
- `PoolManager`는 풀이 소진되면 자동으로 새 오브젝트를 `Instantiate`해서 확장함 (풀 크기 무제한 증가 가능).
