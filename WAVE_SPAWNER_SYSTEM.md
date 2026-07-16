# 웨이브 스포너 / 오브젝트 풀링 시스템 템플릿

Unity 2D 프로젝트용 재사용 가능한 웨이브 스폰 모듈. "정해진 순서/타이밍대로 오브젝트를 스폰하고, 스폰된 오브젝트를 오브젝트 풀로 재사용"하는 틀만 제공. 적 AI, 투사체 로직, 타워 디펜스 판정 등 실제 게임플레이는 구현하지 않고 이벤트 훅만 열어둠.

## 빠른 시작

1. Unity Editor에서 프로젝트 열기
2. 메뉴 `Wave Spawner Template > Build Demo Scene` 실행 → `Assets/Scenes/WaveSpawnerDemo.unity` 자동 생성
3. 생성된 씬 열고 Play 버튼
4. 콘솔에 웨이브 시작/완료/스폰 로그가 찍히는 것 확인, `P`키로 일시정지, `N`키로 다음 웨이브 스킵

## 폴더 구조

```
Assets/Scripts/
├── Pooling/
│   ├── IPoolable.cs           풀링 대상이 스폰/반환 시점에 상태 초기화할 수 있는 콜백 인터페이스
│   ├── ObjectPool.cs          프리팹 하나를 관리하는 오브젝트 풀 (Queue 기반)
│   ├── PooledObjectTag.cs     풀에서 생성된 오브젝트에 자동으로 붙는 원본 프리팹 태그
│   └── PoolManager.cs         여러 프리팹의 풀을 관리하는 싱글턴
├── Spawning/
│   ├── SpawnPoint.cs          씬에 배치하는 스폰 위치 마커 (카테고리 필터링 가능)
│   └── SpawnPointGroup.cs     여러 스폰 지점을 묶어 전략에 따라 하나를 선택
├── Wave/
│   ├── SpawnableData.cs       스폰 가능한 오브젝트 정의 (프리팹 + 기본 스탯) ScriptableObject
│   ├── WaveData.cs            웨이브 하나 (무엇을 몇 개, 얼마나 자주 스폰할지) ScriptableObject
│   ├── WaveSequenceData.cs    웨이브들을 순서대로 묶은 스테이지 데이터 ScriptableObject
│   └── WaveSpawner.cs         WaveSequenceData를 실제로 실행하는 스포너 매니저 (핵심 로직)
├── Demo/ (데모 전용 - 실제 프로젝트에선 삭제하고 실제 로직으로 교체)
│   ├── PooledObjectDemo.cs        일정 시간/경계이탈 시 자동으로 풀에 반환되는 예시
│   ├── WaveSpawnerDebugInput.cs   P/N 키로 스포너를 제어하는 디버그 입력
│   └── WaveSpawnerEventLogger.cs  WaveSpawner 이벤트를 콘솔에 출력하는 예시
└── Editor/
    └── WaveSpawnerSceneBuilder.cs   데모 씬 전체를 자동 생성하는 에디터 툴
```

## 스크립트별 기능 설명

### IPoolable / ObjectPool / PooledObjectTag / PoolManager (오브젝트 풀링)
- `IPoolable`: `OnSpawned()`/`OnDespawned()` 두 콜백만 정의. 풀링되는 오브젝트가 상태 초기화/정리를 하고 싶으면 구현.
- `ObjectPool`: 프리팹 하나당 하나씩 생성되는 순수 C# 클래스(비활성 오브젝트 Queue). 생성자에서 `initialSize`만큼 미리 만들어둠(워밍업). `Get()`은 큐에서 꺼내거나(없으면 새로 Instantiate) 활성화, `Release()`는 비활성화 후 다시 큐에 반환.
- `PooledObjectTag`: `ObjectPool.CreateNew()`가 생성 시 자동으로 붙여서 "이 오브젝트가 어느 프리팹 소속인지" 기억 - `PoolManager.Release()`가 올바른 풀을 찾는 데 사용.
- `PoolManager`(싱글턴): `presets` 인스펙터 목록으로 시작 시 풀을 미리 등록. `Get(prefab, pos, rot)`/`Release(obj)`가 메인 진입점. `OnReleased` 이벤트로 "오브젝트가 반환됐다"를 외부(WaveSpawner)에 알림.

### SpawnPoint / SpawnPointGroup (스폰 위치)
- `SpawnPoint`: 씬에 배치하는 마커. `restrictByCategory`를 켜면 `allowedCategories`에 있는 카테고리만 받음. Scene 뷰에 청록색 기즈모로 표시됨.
- `SpawnPointGroup`: 여러 `SpawnPoint`를 묶고 `SpawnSelectionStrategy`(Random/Sequential/RoundRobin)에 따라 카테고리 조건을 만족하는 지점 중 하나를 선택.

### SpawnableData / WaveData / WaveSequenceData (데이터, ScriptableObject)
- `SpawnableData`: 프리팹 + 카테고리 + 선택적 기본 스탯(`baseHealth`/`baseSpeed`/`baseDamage`, 실제 사용은 자유). 인스펙터 메뉴 `Wave Spawner Template/Spawnable Data`.
- `WaveData`: `WaveEntry`(스폰 데이터 + 개수) 목록, 스폰 간격, 종료 조건(`AllQuantitySpawned` 또는 `FixedDuration`). `TotalSpawnCount()`로 전체 스폰 예정 수 조회 가능.
- `WaveSequenceData`: `WaveSequenceEntry`(웨이브 + 시작 전 대기시간) 목록 - 하나의 스테이지/레벨을 구성.

### WaveSpawner (핵심 로직, MonoBehaviour)
`waveSequence`, `spawnPointGroup` 인스펙터 참조 필요. `autoStart`가 켜져 있으면 `Start()`에서 자동 시작.
- 코루틴 `RunSequence()`가 시퀀스의 웨이브를 순서대로 실행 (대기 → 시작 이벤트 → 웨이브 실행 → 완료 이벤트)
- `RunWave()`는 종료 조건에 따라 `SpawnUntilQuantityExhausted()`(수량 다 쓰면 종료) 또는 `SpawnForDuration()`(정해진 시간 동안 항목을 순환하며 계속 스폰) 중 하나를 실행
- `advanceMode`가 `OnAllSpawnedDespawned`면 스폰된 오브젝트가 전부 풀로 돌아올 때까지 다음 웨이브를 안 넘어감 (`PoolManager.OnReleased`를 구독해서 `aliveObjects` 추적)
- `Pause()`/`Resume()`/`SkipToNextWave()`로 외부에서 제어 가능
- 이벤트: `OnWaveStarted`, `OnWaveCompleted`, `OnAllWavesCompleted`, `OnObjectSpawned`

### Demo/* (데모 전용, 실제 프로젝트에서는 교체 대상)
- `PooledObjectDemo`: `IPoolable` 구현 예시 - 일정 시간 지나거나 경계 벗어나면 자동으로 `PoolManager.Release()` 호출. 실제 적/투사체 스크립트로 교체할 자리.
- `WaveSpawnerDebugInput`: 레거시 `Input` 클래스로 P(일시정지 토글)/N(웨이브 스킵) 단축키 제공. 해커톤 당일 테스트 편의용.
- `WaveSpawnerEventLogger`: `WaveSpawner`의 4개 이벤트를 구독해 `Debug.Log` 출력 - 이벤트 구독 패턴의 살아있는 예시. 실제로는 점수/UI/승리조건 로직으로 교체.

### WaveSpawnerSceneBuilder (에디터 전용 스크립트)
`Wave Spawner Template > Build Demo Scene` 메뉴로 씬 전체를 코드로 생성:
1. 데모 프리팹 3종 생성 (빨간 네모, 파란/초록 원 - 코드로 그린 단색 스프라이트, 실제 아트 불필요)
2. `SpawnableData` 3종, `WaveData` 3종(웨이브 1~3), `WaveSequenceData` 1종 생성
3. 카메라 → `PoolManager`(3개 프리팹 프리셋 등록) → `SpawnPointGroup`(귀퉁이 4곳, RoundRobin) → `WaveSpawner` 순서로 하이어라키 구성
4. 데모 편의용 `WaveSpawnerEventLogger`/`WaveSpawnerDebugInput` 부착
5. 씬을 `Assets/Scenes/WaveSpawnerDemo.unity`로 저장

## 씬 오브젝트 계층 요약

```
Main Camera                     orthographic, 배경색만 담당
PoolManager                     presets = [RedSquare, BlueCircle, GreenCircle]
SpawnPointGroup                 strategy = RoundRobin
├── SpawnPoint_0 (-6, 4)
├── SpawnPoint_1 (6, 4)
├── SpawnPoint_2 (-6, -4)
└── SpawnPoint_3 (6, -4)        restrictByCategory = true, TypeB만 허용
WaveSpawner                     waveSequence = WSD_DemoStage
├── WaveSpawnerEventLogger      콘솔 로그 출력
└── WaveSpawnerDebugInput       P/N 단축키
```

## 실제 게임 로직을 붙이는 지점

기존 스크립트를 수정하지 않고 아래 이벤트만 구독하면 됨:

| 훅 지점 | 시그니처 | 언제 발생 |
|---|---|---|
| `WaveSpawner.OnWaveStarted` | `Action<int>` | 각 웨이브 시작 시 (웨이브 인덱스) |
| `WaveSpawner.OnWaveCompleted` | `Action<int>` | 각 웨이브 완료 시 |
| `WaveSpawner.OnAllWavesCompleted` | `Action` | 시퀀스의 모든 웨이브가 끝났을 때 - 스테이지 클리어 판정에 사용 |
| `WaveSpawner.OnObjectSpawned` | `Action<GameObject, SpawnableData>` | 오브젝트 하나가 스폰될 때마다 - AI 초기화, 스탯 적용 등 |
| `PoolManager.OnReleased` | `Action<GameObject>` | 오브젝트가 풀로 반환될 때 - 처치 판정/점수 등 |

`PooledObjectDemo`, `WaveSpawnerDebugInput`, `WaveSpawnerEventLogger`는 실제 프로젝트에 그대로 쓰지 말고 삭제 후 실제 로직(적 AI, UI 갱신, 점수판)으로 교체할 것 - 파일 상단에 그 안내 주석이 달려있음.

## 한글 관련 참고사항

- 이 템플릿은 UI 텍스트(TextMeshPro)를 사용하지 않고 콘솔 로그와 코드 주석에만 한글을 씀 - 콘솔/주석은 폰트 글리프 문제가 없으므로 별도 조치 불필요. 나중에 UI(웨이브 표시, 남은 시간 등)를 TMP로 추가한다면 인벤토리 템플릿 쪽의 한글 SDF 폰트 생성 방식(`Assets/Scripts/Editor/InventorySceneBuilder.cs`의 `CreateOrLoadKoreanFontAsset` 참고, 다른 브랜치)을 재사용할 것.
- **git 한글 파일명/커밋 깨짐 방지** (이 저장소에 로컬로 적용됨, 새로 clone한 환경에서는 한 번씩 실행 필요):
  ```
  git config core.quotepath false        # git status/log에서 한글 파일명이 그대로 보이게
  git config core.precomposeunicode true # macOS 자모분리(NFD) 문제 방지
  git config i18n.commitencoding utf-8
  git config i18n.logoutputencoding utf-8
  ```
  `.gitattributes`로 텍스트 파일 line ending도 정규화해둠.

## 코드 스타일

- 모든 public 필드/함수에 한글 한 줄 설명 주석 + 핵심 로직(코루틴, 풀 반환, 웨이브 진행)마다 "왜" 설명 주석
- 매직 넘버 없이 `[SerializeField]`로 인스펙터에서 조정 가능
- 싱글턴은 단순 static 인스턴스 패턴 (`DontDestroyOnLoad` 미사용)
- 이벤트/콜백 기반 설계 - 실제 게임 로직은 기존 스크립트 수정 없이 이벤트 구독만으로 확장 가능
