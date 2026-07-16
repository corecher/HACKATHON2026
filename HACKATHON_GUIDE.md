# 해커톤 사용 가이드 - 웨이브 스포너 / 오브젝트 풀링 템플릿

이 문서는 "어떻게 만들었나"가 아니라 "해커톤에서 어떻게 빨리 갖다 쓰나"를 다룸. 구조/전체 기능 설명은 [`WAVE_SPAWNER_SYSTEM.md`](./WAVE_SPAWNER_SYSTEM.md) 참고.

## 1. 5분 안에 돌려보기

1. `Wave Spawner Template > Build Demo Scene` 메뉴 실행
2. `Assets/Scenes/WaveSpawnerDemo.unity` 열고 Play
3. 콘솔 창 열어두고 웨이브 시작/완료/스폰 로그 확인
4. `P`키로 일시정지/재개, `N`키로 다음 웨이브 강제 스킵 눌러보기
5. 여기서부터 우리 게임 프리팹/밸런스로 데이터만 바꿔나가면 됨

씬을 밀고 다시 만들고 싶으면 메뉴를 다시 실행하면 됨. 직접 손댄 씬이면 먼저 복사해두고 실행할 것 (실행 시 "저장 안 한 변경사항 저장할지" 물어봄).

## 2. 우리 게임 프리팹으로 교체하기

코드 안 건드려도 됨.

1. 우리 적/오브젝트 프리팹 준비 (Rigidbody2D + Collider2D는 있어야 물리 판정 가능, 필수는 아님)
2. 프리팹에 `IPoolable` 구현한 스크립트 추가 (선택 사항 - 스폰/반환 시 초기화할 상태가 있으면 구현, 없으면 생략 가능)
3. `Assets/Data/Spawnables`에 `Create > Wave Spawner Template > Spawnable Data`로 새 데이터 생성, 프리팹/카테고리 지정
4. `Assets/Data/Waves`에 `Create > Wave Spawner Template > Wave Data`로 웨이브 구성 (어떤 Spawnable을 몇 개, 몇 초 간격으로)
5. `Create > Wave Spawner Template > Wave Sequence Data`로 웨이브들을 순서대로 묶기
6. 씬의 `WaveSpawner`에 이 시퀀스를 연결

## 3. 실제 게임 로직 붙이기 (여기가 제일 중요)

이 템플릿은 스폰/풀링/타이밍만 담당하고 "스폰된 게 뭘 하는지"는 구현 안 해놨음. 아래 이벤트만 구독하면 기존 코드 한 줄도 안 건드리고 우리 게임 로직을 붙일 수 있음.

```csharp
// 오브젝트가 스폰될 때마다 - 여기서 AI 시작, 스탯 적용 등
waveSpawner.OnObjectSpawned += (obj, data) =>
{
    // 예: obj.GetComponent<EnemyAI>().Initialize(data.BaseHealth, data.BaseSpeed);
};

// 웨이브 하나가 끝날 때 - 보상 지급, UI 갱신 등
waveSpawner.OnWaveCompleted += (waveIndex) =>
{
    // 예: ScoreManager.Instance.AddWaveBonus(waveIndex);
};

// 모든 웨이브가 끝났을 때 - 스테이지 클리어 처리
waveSpawner.OnAllWavesCompleted += () =>
{
    // 예: GameManager.Instance.ShowVictoryScreen();
};

// 오브젝트가 풀로 반환될 때(=처치/소멸) - 점수, 남은 적 수 UI 등
PoolManager.Instance.OnReleased += (obj) =>
{
    // 예: ScoreManager.Instance.AddKill();
};
```

`Demo/` 폴더의 세 스크립트(`PooledObjectDemo`, `WaveSpawnerDebugInput`, `WaveSpawnerEventLogger`)는 예시일 뿐이니 참고만 하고, 실제 빌드에선 지우거나 우리 로직으로 바꿀 것.

## 4. 자주 바꾸는 값들 (인스펙터, 코드 수정 불필요)

| 바꾸고 싶은 것 | 어디서 |
|---|---|
| 웨이브 순서/구성 | `WaveSequenceData` 애셋의 `Waves` 리스트 |
| 한 웨이브에서 뭘 몇 개 스폰할지 | `WaveData` 애셋의 `Entries` 리스트 |
| 스폰 간격 | `WaveData`의 `Spawn Interval` |
| 웨이브 종료 조건 | `WaveData`의 `End Condition` (수량 소진 / 고정 시간) |
| 다음 웨이브로 넘어가는 타이밍 | `WaveSpawner`의 `Advance Mode` (스폰 끝나면 즉시 / 다 죽을 때까지 대기) |
| 스폰 지점 위치/전략 | `SpawnPointGroup`의 `Strategy`(Random/Sequential/RoundRobin), 자식 `SpawnPoint`들의 위치 |
| 풀 초기/최대 크기 | `PoolManager`의 `Presets` 리스트 |

## 5. 다른 장르로 바꿔 쓰기

- **타워 디펜스**: `SpawnPoint`를 경로 시작점으로, `WaveSequenceData`를 스테이지별 난이도 곡선으로 사용
- **탄막/슈팅**: 총알 프리팹을 `SpawnableData`로 등록, `SpawnPointGroup`을 발사 패턴 지점으로 재해석
- **좀비/생존**: `SpawnableCategory`로 몬스터 등급 분류, `FixedDuration` 종료 조건으로 "N분간 버티기" 웨이브 구성
- **아이템/보상 드롭**: `PoolManager`를 드롭 아이템 풀링에도 그대로 재사용 가능 (WaveSpawner와 별개로 `PoolManager.Instance.Get(...)` 직접 호출)

핵심 규칙: **이 템플릿의 기존 스크립트는 되도록 수정하지 말고, 이벤트 구독 + 새 스크립트 추가로 확장**할 것.

## 6. 자주 나는 문제 체크리스트

| 증상 | 원인/해결 |
|---|---|
| Play 눌러도 아무것도 안 스폰됨 | `WaveSpawner`의 `Wave Sequence`/`Spawn Point Group` 연결 빠짐, 또는 `autoStart` 꺼져있음 |
| 오브젝트가 계속 쌓이기만 하고 안 사라짐 | 스폰된 오브젝트가 `PoolManager.Release()`를 호출하는 로직이 없음 - `PooledObjectDemo`처럼 직접 반환 호출 필요 |
| 특정 스폰 지점에서 원하는 게 안 나옴 | `SpawnPoint`의 `Restrict By Category` 켜져있는데 `Allowed Categories`에 그 카테고리가 없음 |
| 웨이브가 끝나야 할 때 안 끝남 | `Advance Mode`가 `OnAllSpawnedDespawned`인데 스폰된 오브젝트들이 `PoolManager.Release()`를 안 불러서 영원히 대기 중일 수 있음 |
| `PoolManager.Instance`가 null이라 에러남 | 씬에 `PoolManager` 오브젝트가 있는지, 스크립트 실행 순서상 다른 `Awake()`보다 먼저 초기화됐는지 확인 (씬 재생성이 제일 빠름) |
| 웨이브 도중에 게임 멈추는 느낌 | `spawnInterval`이 너무 크거나, `WaitSeconds`가 `isPaused` 상태에서 멈춰있는 채 방치된 건 아닌지 확인 |

## 7. 시간 없을 때 우선순위

1. **꼭 필요**: 우리 프리팹 등록(2번), 스폰/처치 이벤트 훅업(3번)
2. **여유 있으면**: 웨이브 밸런스 조정(4번), 스폰 전략 커스터마이징
3. **발표 직전엔 건드리지 말 것**: `WaveSpawner.cs`의 코루틴 로직, `PoolManager`/`ObjectPool`의 Get/Release 흐름 - 여기 손대면 리스크 큼, 이벤트 구독으로 우회할 방법부터 찾을 것
