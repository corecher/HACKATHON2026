# 해커톤 사용 가이드 - 카드 게임 템플릿

이 문서는 "어떻게 만들었나"가 아니라 "해커톤에서 어떻게 빨리 갖다 쓰나"를 다룸. 구조/전체 기능 설명은 [`CARD_GAME_SYSTEM.md`](./CARD_GAME_SYSTEM.md) 참고.

## 1. 5분 안에 돌려보기

1. `Card Game Template > Build Demo Scene` 메뉴 실행
2. `Assets/Scenes/CardGameTemplate.unity` 열고 Play
3. 왼쪽 Draw 존의 뒷면 카드를 드래그해서 빼보기 - 실제 카드로 바뀌어 손패에 추가됨
4. 손패 카드를 가운데(Play)/오른쪽(Discard) 존으로 드래그, 콘솔 로그 확인
5. 여기서부터 우리 카드 데이터/규칙으로 채워나가면 됨

씬을 밀고 다시 만들고 싶으면 메뉴를 다시 실행하면 됨 (기존 씬 파일을 지우고 새로 만듦 - 직접 손댄 씬이면 먼저 복사해둘 것).

## 2. 새 카드 추가하기

코드 안 건드려도 됨.

1. `Assets/Data/Cards` 우클릭 → `Create > Card Game Template > Card Data`
2. 인스펙터에서 이름/설명/아이콘/코스트/공격력 채우기
3. 씬의 `DeckManager`의 `Initial Deck List`에 드래그해서 추가 (또는 코드에서 런타임에 리스트 구성)

아이콘이 아직 없으면 데모 카드들처럼 흰색 플레이스홀더로 임시 채워도 됨.

## 3. 실제 카드 효과/게임 규칙 붙이기 (여기가 제일 중요)

이 템플릿은 "카드를 어디에 놓았다"까지만 처리하고 실제로 뭘 하는지는 구현 안 해놨음. 아래를 구독/할당하면 기존 코드 한 줄도 안 건드리고 우리 게임 로직을 붙일 수 있음.

```csharp
// 카드가 Play 존에 놓였을 때 - 실제 효과 발동 지점
playZone.OnCardPlayed.AddListener((cardView) =>
{
    CardData data = cardView.BoundData;
    // 예: BattleManager.Instance.ApplyCardEffect(data);
});

// 카드가 Discard 존에 놓였을 때
discardZone.OnCardDiscarded.AddListener((cardView) =>
{
    // 예: GraveyardManager.Instance.Add(cardView.BoundData);
});

// 특정 조건에서만 카드를 받아들이게 하고 싶으면 (예: 코스트 부족하면 거부)
playZone.CanAccept = (cardView) =>
{
    return cardView.BoundData != null && cardView.BoundData.Cost <= currentMana;
};

// 턴 진행 - GameManager의 빈 메서드들 안에 실제 로직 채우기
GameManager.Instance.EnterPlayerTurn("player1");
```

`onCardPlayed`/`onCardDiscarded`는 인스펙터에서도 직접 리스너를 등록할 수 있음 (씬의 `PlayZone`/`DiscardZone` 오브젝트 선택 → 인스펙터에서 `+` 버튼).

## 4. 자주 바꾸는 값들 (인스펙터, 코드 수정 불필요)

| 바꾸고 싶은 것 | 어디서 |
|---|---|
| 시작 덱 구성 | `DeckManager`의 `Initial Deck List` |
| 시작 시 자동 셔플 여부 | `DeckManager`의 `Shuffle On Start` |
| 손패 배치 방식(부채꼴/일렬) | `HandManager`의 `Use Fan Layout`, `Card Spacing`, `Fan Angle Step` |
| 시작 시 자동 드로우 수 | `HandManager`의 `Draw On Start Count` |
| 카드 뒤집기 속도/연출 여부 | `Card` 프리팹의 `Flip Duration`, `Animate Flip` |
| 드롭 판정 민감도 | `CardDragHandler`의 `Drop Check Offset`/`Drop Check Radius` |
| 호버 효과 강도 | `CardDragHandler`의 `Hover Scale Multiplier`, `Hover Move Up` |

## 5. 다른 장르로 응용

- **덱빌딩 로그라이크**: 런타임에 `DeckManager` 덱 목록을 코드로 직접 조작(카드 추가/제거), 라운드마다 보상으로 카드 편입
- **TCG/전략**: `GameManager`에 마나/자원 필드 추가, `PlayZone.CanAccept`에서 코스트 체크, 턴 종료 시 `DrawZone.Refill()` 자동 호출
- **매치/퍼즐 결합**: `CardData`를 타일/블록 데이터로, `CardZoneBase`를 매치 판정 영역으로 재해석

핵심 규칙: **이 템플릿의 기존 스크립트는 되도록 수정하지 말고, 이벤트 구독/델리게이트 할당 + 새 스크립트로 확장**할 것.

## 6. 자주 나는 문제 체크리스트

| 증상 | 원인/해결 |
|---|---|
| 카드가 드래그가 안 됨 | 카드 프리팹에 `Collider2D`가 있는지 확인 (레거시 `OnMouseDown`은 콜라이더 필수) |
| Draw 존에서 카드를 빼도 아무 반응 없음 | `dragThreshold`(드래그 최소 거리)보다 적게 드래그했을 가능성 - 좀 더 멀리 끌어볼 것 |
| Play/Discard 존에 드롭해도 안 받아들여짐 | `CardDragHandler`의 `Drop Check Offset`/`Radius`가 존의 콜라이더와 안 겹치는지 확인, 또는 `CanAccept`가 false를 반환 중인지 확인 |
| 카드 설명 텍스트가 □로 깨짐 | `Assets/Fonts/Korean SDF.asset`이 해당 텍스트에 폰트로 지정돼 있는지 확인. 새로 만든 TMP 텍스트엔 직접 이 폰트를 지정할 것 |
| 손패 카드가 겹쳐서 나옴 | `HandManager.RelayoutHand()`가 호출 안 됐을 가능성 - 카드 추가/제거는 반드시 `HandManager`의 메서드(`DrawToHand`, `RemoveFromHand` 등)를 통해서 해야 자동으로 재배치됨 |
| 덱이 텅 비어서 Draw 존이 안 채워짐 | 정상 동작 - `DeckManager.RemainingCount`가 0이면 `Refill()`이 아무것도 안 함. 덱 리필/무한 카드 로직이 필요하면 `DeckManager`에 추가 |
| 프리팹 수정했는데 씬에 반영 안 됨 | `Build Demo Scene`을 다시 실행하면 프리팹과 씬을 전부 새로 생성함 (기존 프리팹 애셋은 삭제 후 재생성) |

## 7. 시간 없을 때 우선순위

1. **꼭 필요**: 카드 데이터 채우기(2번), 카드 효과/규칙 훅업(3번)
2. **여유 있으면**: 손패 배치/연출 조정(4번), 턴 진행 로직(`GameManager`)
3. **발표 직전엔 건드리지 말 것**: `CardDragHandler.cs`의 드래그/드롭 판정 로직, `DeckManager.Shuffle`/`Draw` - 여기 손대면 리스크 큼, 이벤트/델리게이트로 우회할 방법부터 찾을 것
