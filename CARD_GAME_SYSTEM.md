# 카드 게임 시스템 템플릿

Unity 2D 프로젝트용 재사용 가능한 카드 게임 모듈. "카드가 덱에서 뽑혀 손패로 오고, 드래그로 존(플레이/버리기)에 놓이는" 틀만 제공. 실제 카드 효과(공격, 버프, 코스트 소모 등)와 턴/승패 규칙은 구현하지 않고 확장 지점만 열어둠.

## 빠른 시작

1. Unity Editor에서 프로젝트 열기
2. 메뉴 `Card Game Template > Build Demo Scene` 실행 → `Assets/Scenes/CardGameTemplate.unity` 자동 생성
3. 생성된 씬 열고 Play 버튼
4. 화면 왼쪽 Draw 존의 뒷면 카드를 드래그해서 빼면 실제 카드 한 장이 손패에 추가됨
5. 손패 카드를 가운데 Play 존이나 오른쪽 Discard 존으로 드래그 - 콘솔에 로그 출력 확인

씬을 밀고 다시 만들고 싶으면 메뉴를 다시 실행하면 됨 (기존 씬 파일을 지우고 새로 만듦).

## 폴더 구조

```
Assets/Scripts/
├── Card/
│   ├── CardData.cs      카드 한 장의 고정 데이터 (ScriptableObject)
│   ├── Card.cs           카드 오브젝트 루트 - 데이터 바인딩 + 앞/뒷면 뒤집기
│   └── CardView.cs       CardData를 실제 스프라이트/텍스트로 반영하는 뷰
├── Deck/
│   └── DeckManager.cs    덱 보관 + 셔플 + 드로우 (싱글턴)
├── DragDrop/
│   └── CardDragHandler.cs  마우스 드래그로 카드를 집어 존에 놓는 상호작용 (레거시 OnMouseDown 계열)
├── Zones/
│   ├── CardZoneBase.cs   손패를 받아들이는 존의 공통 베이스 (PlayZone/DiscardZone가 상속)
│   ├── PlayZone.cs       카드를 플레이하는 존
│   ├── DiscardZone.cs    카드를 버리는 존
│   └── DrawZone.cs       카드를 뽑아내는 존 (반대 방향이라 베이스 상속 안 함)
├── UI/
│   └── HandManager.cs    손패 카드 생성/배치(부채꼴 또는 일렬)/제거
└── GameManager.cs        턴 소유자/게임 상태만 보관하는 최소 골격

Assets/Editor/
└── CardGameTemplateBuilder.cs   데모 씬 전체를 자동 생성하는 에디터 툴
```

## 스크립트별 기능 설명

### CardData (ScriptableObject)
카드 한 장의 정의: `cardId`, `cardName`, `description`, `icon`, `cost`, `power`. 인스펙터 메뉴 `Card Game Template/Card Data`로 생성. 실제 카드 효과 로직은 없음 - `cost`/`power`는 임의의 수치일 뿐, 실제 사용은 게임 로직 쪽에서 구현.

### Card / CardView (MonoBehaviour)
- `Card`: 카드 오브젝트의 루트. `Setup(data, faceUp)`으로 데이터 바인딩, `Flip()`/`SetFaceUp()`으로 앞뒷면 전환. `animateFlip`이 켜져있으면 가로 스케일을 0으로 줄였다 늘리는 코루틴으로 뒤집기 연출.
- `CardView`: `Bind(data)`로 실제 스프라이트/TMP 텍스트(이름/설명/코스트/공격력)를 갱신. `CardDragHandler`가 `[RequireComponent]`로 함께 요구함.

### DeckManager (싱글턴)
`initialDeckList`(인스펙터 카드 목록)로 시작, `Awake()`에서 런타임 덱을 채우고 `shuffleOnStart`가 켜져있으면 `Start()`에서 셔플. `Shuffle()`은 Fisher-Yates 알고리즘. `Draw(count)`는 리스트 끝(=덱 맨 위)에서 최대 count장을 꺼내 반환 (부족하면 있는 만큼만).

### CardDragHandler (MonoBehaviour)
레거시 `OnMouseDown`/`OnMouseDrag`/`OnMouseUp`/`OnMouseEnter`/`OnMouseExit` 콜백 기반 - **콜라이더 필수**(2D 월드 오브젝트 드래그라 uGUI 이벤트가 아니라 이 방식 사용). 드래그 시작 시 원위치/부모를 기억해두고, 놓았을 때 카드 하단 기준점과 겹치는 `CardZoneBase`를 찾아 `TryAcceptCard()` 시도. 받아들여지면 손패에서 제거, 아니면 이징 커브로 원위치 복귀. `DrawZone`의 소스 카드는 `MarkAsDrawSource()`로 표시해두면 일반 존 드롭 대신 "충분히 드래그했으면 실제로 드로우" 로직으로 분기됨. 호버 시 확대/위로 이동하는 효과도 포함.

### CardZoneBase / PlayZone / DiscardZone / DrawZone
- `CardZoneBase`(추상 클래스): `CanAccept`(델리게이트, 기본은 항상 true)로 수용 조건을 커스터마이징 가능. `TryAcceptCard()`가 조건 통과 시 하위 클래스의 `OnCardAccepted()`를 호출.
- `PlayZone`/`DiscardZone`: `OnCardAccepted()`를 오버라이드해 로그만 남기고 `CardZoneEvent`(UnityEvent, 인스펙터에서 리스너 연결 가능)를 발동. 실제 카드 효과 발동/버림 처리는 이 이벤트를 구독해서 구현.
- `DrawZone`: 받는 존이 아니라 내보내는 존이라 `CardZoneBase`를 상속하지 않음. 페이스다운 더미 카드를 미리 스폰해두고(`Refill()`), 임계 거리 이상 드래그되면(`HandleDraggedOut()`) 실제로 덱에서 한 장 뽑아 손패에 추가.

### HandManager (싱글턴)
`DrawToHand(count)`로 덱에서 뽑아 즉시 앞면으로 생성, `AddDrawnCard(data)`로 DrawZone처럼 페이스다운 생성 후 애니메이션으로 뒤집기. `RelayoutHand()`가 손패 카드 수에 맞춰 전체 위치/회전을 재계산 - `useFanLayout`으로 부채꼴/일렬 선택 가능.

### GameManager (싱글턴)
`GameState`(Setup/PlayerTurn/Resolving/GameOver) enum과 `currentTurnOwner`만 보관하는 최소 골격. `EnterSetup()`/`EnterPlayerTurn()`/`EnterResolving()`/`EnterGameOver()` 메서드는 상태만 바꾸고 실제 로직은 비어있음 - 여기에 턴 진행/승패 판정을 채워 넣으면 됨.

### CardGameTemplateBuilder (에디터 전용 스크립트)
`Card Game Template > Build Demo Scene` 메뉴로 씬 전체를 코드로 생성:
1. TMP Essential Resources 확인/임포트, 한글 SDF 폰트 생성 (카드 설명에 한글이 들어가므로)
2. 흰색 8x8 플레이스홀더 스프라이트 생성 (실제 아트 불필요)
3. 샘플 카드 데이터 10종 생성 (`Assets/Data/Cards/`)
4. 카드 프리팹 생성 (`Assets/Prefab/Card.prefab`) - 앞면(프레임/아이콘/이름/설명/코스트/공격력 텍스트) + 뒷면 + `Card`/`CardView`/`CardDragHandler` 컴포넌트
5. 씬 하이어라키 구성: 카메라 → `GameManager` → `DeckManager`(샘플 카드 10장 등록) → `HandManager` → Draw/Play/Discard 존 3개
6. 씬을 `Assets/Scenes/CardGameTemplate.unity`로 저장

## 씬 오브젝트 계층 요약

```
Main Camera        orthographic, 배경색만 담당
GameManager         상태 골격만 (Setup/PlayerTurn/Resolving/GameOver)
DeckManager         initialDeckList = 샘플 카드 10장
HandContainer       손패 카드들의 부모 (화면 하단)
HandManager         cardPrefab = Card.prefab, handContainer = 위 오브젝트
DrawZone (왼쪽)      cardPrefab 연결, 시작 시 페이스다운 카드 자동 스폰
PlayZone (가운데)    OnCardPlayed 이벤트
DiscardZone (오른쪽)  OnCardDiscarded 이벤트
```

## 실제 게임 로직을 붙이는 지점

기존 스크립트를 수정하지 않고 아래를 구독/확장하면 됨:

| 훅 지점 | 시그니처 | 언제 발생 |
|---|---|---|
| `PlayZone.OnCardPlayed` | `CardZoneEvent`(`UnityEvent<CardView>`) | 카드가 Play 존에 놓였을 때 - 실제 카드 효과 발동 지점 |
| `DiscardZone.OnCardDiscarded` | `CardZoneEvent` | 카드가 Discard 존에 놓였을 때 |
| `CardDragHandler.OnDroppedOnZone` | `Action<Card, CardZoneBase>` | 임의의 존에 드롭 성공했을 때 (Play/Discard 구분 없이 공통 후처리 필요하면 이걸 사용) |
| `CardZoneBase.CanAccept` | `Func<CardView, bool>` (델리게이트, 직접 할당) | 카드를 받을지 말지 조건 커스터마이징 - 예: 코스트 부족하면 거부 |
| `GameManager.EnterPlayerTurn()` 등 | 직접 호출 | 턴 진행/승패 판정 로직을 이 메서드들 안에 채워 넣기 |

## 다른 장르로 응용

- **덱빌딩/로그라이크**: `DeckManager.initialDeckList`를 런타임에 동적으로 구성 (`RegisterPool`처럼 코드에서 카드 추가/제거)
- **TCG/전략 카드게임**: `CardZoneBase.CanAccept`에 코스트/타이밍 체크 로직 추가, `GameManager`에 자원(마나 등) 관리 붙이기
- **매치3/퍼즐과 결합**: `CardData`를 타일 데이터로 재해석, `CardZoneBase`를 매치 판정 존으로 재활용 가능

## 한글 관련 참고사항

- 카드 설명(`CardData.description`)에 한글 문장이 들어가고 이게 월드 스페이스 `TextMeshPro` 컴포넌트로 렌더링됨. 기본 TMP 폰트(LiberationSans SDF)는 한글 글리프가 없어 □로 깨지므로, `CardGameTemplateBuilder`가 OS에 설치된 한글 폰트 파일(macOS: `AppleGothic.ttf`, Windows: `malgun.ttf`)을 찾아 `Assets/Fonts/Korean SDF.asset`을 `AtlasPopulationMode.Dynamic`으로 생성하고 씬의 모든 카드 텍스트(`CreateWorldText`로 만드는 것 전부)에 자동 적용함.
- **git 한글 파일명/커밋 깨짐 방지** (저장소 로컬 설정, 새로 clone한 환경에서는 한 번씩 실행 필요):
  ```
  git config core.quotepath false        # git status/log에서 한글 파일명이 그대로 보이게
  git config core.precomposeunicode true # macOS 자모분리(NFD) 문제 방지
  git config i18n.commitencoding utf-8
  git config i18n.logoutputencoding utf-8
  ```
  `.gitattributes`로 텍스트 파일 line ending도 정규화해둠.

## 코드 스타일

- 모든 public 필드/함수에 한글 한 줄 설명 주석 + 핵심 로직(드래그 판정, 셔플, 손패 배치, 뒤집기 연출)마다 "왜" 설명 주석
- 매직 넘버 없이 `[SerializeField]`로 인스펙터에서 조정 가능
- 싱글턴은 단순 static 인스턴스 패턴 (`DontDestroyOnLoad` 미사용)
- 이벤트/콜백 기반 설계 - 실제 게임 로직은 기존 스크립트 수정 없이 이벤트 구독/델리게이트 할당만으로 확장 가능
