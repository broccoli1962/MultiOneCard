# 원테이블 (OneTable) — 구현 기획서

> Unity 프로젝트: **MultiOneCard** (Unity 6000.3 LTS). 가제 《원테이블》.
> 원문 기획: `PlanMdFile/MultiCardGame`. **구현·판정 기준은 이 파일**이다.
> 한 줄: 확장 트럼프 91장 원카드를, 모바일과 PC가 같은 방에서 치고 대화하는 크로스플랫폼 멀티 대전.

## 1. 제품 약속

- 본편은 **항상 온라인 멀티**. 솔로는 QA·튜토리얼용.
- 승패·손패·덱은 **권한 서버(또는 동일 RuleEngine)** 가 판결. 클라는 의도만 보낸다.
- 대기실·인게임·결과에 **텍스트 채팅 + 퀵챗 8개**. 음성 없음.
- Android / iOS / Windows가 **하나의 매칭 풀**. 플랫폼 키로 풀을 나누지 않는다.
- 카드는 **고정 91장**. 가챠·시즌 카드·성장 없음.

성공 기준: 2~6인 한 판 완주, 불법 수 거절, 채팅, 이종 기기 같은 룸, 타임아웃·재접속이 규칙을 깨지 않음, 공격/방어/무색 특수/피니시가 아래 Official과 동일.

비목표(이 로드맵): 계정 연동, 랭크, 스킨 가챠, 음성, WebGL, NGO/Fusion 호스트 권한, 봇 충원 퀵매치, 원카드 외 모드.

## 2. 기술 스택 (이미 있는 것 재사용)

| 층 | 선택 |
|----|------|
| 엔진 | Unity 6000.3, URP 2D, Input System, TextMeshPro, uGUI, Addressables, UniTask, R3, LitMotion |
| 권한 | 순수 C# `Game.Rules` + 인프로세스 `MatchRuntime` → 이후 `server/` .NET 8 + WSS |
| 전송 | JSON over WSS. NGO 오브젝트 싱크 사용 금지 |
| 인증 | MVP는 익명 `accountId` + 닉 2~12자 |

기존 인프라를 다시 만들지 말 것.

- 부트: `Backend.Object.Management.Boot` → `GameManager.InitializeCore`
- UI: `UIManager` + `UIPanel<TPresenter>` / `UIPopup` / `UILayer` (HUD, Panel, Navigation, Popup)
- 리소스: `ResourceManager` + `AddressableKeys`. `Resources.Load` 금지
- 비동기: UniTask. 코루틴(`IEnumerator`) 금지
- 입력: Input System만. 구 Input Manager에 규칙을 묶지 않음
- 프리팹 경로 관례: `Assets/GameResource/Prefab/` (단수). 주소 예: `UI/UIRoot.prefab`

네임스페이스: 규칙=`Game.Rules`, 네트=`Backend.Net`, 앱 로직=`Backend.App`, UI=`Backend.Object.UI`, 매니저=`Backend.Object.Management`.

## 3. 폴더

```
Assets/GameResource/Scripts/
  Rules/          # Game.Rules.asmdef — Unity 모듈 참조 없음
  Net/            # 소켓, seq, 재접속
  App/            # Auth, Lobby/Match 로직, PresentationQueue, GamePointer
  Object/Management/   # 기존 유지
  Object/UI/           # 기존 Base + 화면
  Util/                # 기존 유지
Assets/GameResource/Data/Cards/
Assets/GameResource/Prefab/UI/
Assets/Tests/EditMode/     # Game.Rules 참조
server/                    # .NET 8 Gateway + Lobby + MatchWorker (Assets 밖)
```

화면은 씬을 늘리지 않고 패널로 연다: Title, Lobby, Room, Match, Result, HowTo, Settings. 기존 `GameState`(Ready/Playing/GameOver/StageClear)는 템플릿 잔재 — 매치 phase는 `Waiting | Starting | InMatch | Result` 로 둔다.

## 4. 식별자와 덱 (91장)

`CardInstanceId` = 0..90. `CardDefId`:

- 트럼프: `S|H|D|C|R|M` + `A|2..10|J|Q|K`  (별=`R`, 달=`M`)
- 조커: `JOKER:COLOR`(+10), `JOKER:BW`(+5), `JOKER:MOON`(+15)
- 무색: `SPEC:SPEAR`, `SPEC:PASS`×3, `SPEC:REVJOKER`, `SPEC:COUNTER`, `SPEC:MIRROR`, `SPEC:PILL_BK|RD|BL`

고유 def 89종 / 인스턴스 91. 런타임 행 추가 금지. 조커 공격값은 `MatchState.jokerAttack`이 덮어쓴다.

색: S·C=Black, H·D=Red, R·M=Blue. 별과 달은 둘 다 청이므로 **실루엣으로 구분**.

점수(동률·자동 선택): 5~10=숫자, J=10, A=15, 2/3/4/7/Q/K=20, COLOR·BW=30, PASS·PILL=25, MOON·REVJOKER·MIRROR=40, COUNTER=45, SPEAR=50.

## 5. Official 룰 (서버 판정 기준)

인원 2~6. 손패: 2~4인 7장, 5~6인 5장. 배분 후 덱 1장을 버림(특수여도 **효과 없음**). 선은 난수 좌석. 기본 방향 반시계(+1). 턴 15초.

컨테이너(서버만 전체): Deck 큐, Discard 스택(top만 공개), Hands[seat]. 불변식: 손패합+덱+버림=91. 덱 고갈 시 top 1장 남기고 나머지 셔플. 그래도 없으면 그 드로우는 실패하고 턴만 넘김(`DeckExhausted`).

### 기본 수

버림 top과 **같은 무늬 또는 같은 랭크**. 조커·무색은 알약 락이 없으면 아무 위에도 가능. 합법 수가 있어도 드로우 허용. `DrawAndPlay=true`면 드로우 장을 같은 턴에 낼 수 있다. 한 턴 1장. 예외는 K.

### 합법 함수 (클라 힌트 = 서버와 동일 코드, 판정은 서버만)

- 공격 응답: PASS, COUNTER, SPEAR, (죽창 없으면) 3·4, 또는 공격 카드(2/A/조커).
- Q 응답: Q 또는 3·4.
- `requiredColor` 있으면: 그 색 문양, 또는 같은 색 알약. 조커·다른 무색 불가.
- 그 외: wild/무색, 또는 7, 또는 top이 wild이고 `requiredSuit==null`이면 아무 장, 아니면 suit==required 또는 rank==top.rank.
- 7 이후 `requiredSuit`가 지정값. 다음 수: 그 무늬 또는 7 또는 wild(락 없을 때).

소유·턴 검사 후 accept. 마지막 장이 합법이면 **공격·Q·K여도 피니시**. 손패 0이면 효과는 적용하지 않고 1위.

### 랭크 특수

| 카드 | 효과 |
|------|------|
| 2 / A | 공격 +2 / +3. 여섯 문양 동일 |
| 조커 3종 | 와일드 공격. 값은 `jokerAttack` |
| 3 · 4 | 공격 또는 Q 응답에서만 스택 0. 평소는 일반. 죽창 섞이면 불법 |
| 7 | 내고 6문양 지정. 초과 시 낸 7의 원래 무늬 |
| J | 다음 활성 1명 스킵 |
| Q | Reverse(방향 반전. 2인이면 상대 스킵) 또는 Give(`queenStack=1`). 초과=Reverse. 마지막 Q면 효과 없음 |
| K | Extra(K 기준 합법 1장 더. 특수는 정상 발동. 또 K면 재선택) 또는 Hide(아무 1장을 K 밑, 효과 없음, 앞면 비공개). 초과=K만 내고 종료. 숨길 장 없으면 Hide 불가 |

### 무색 특수

일반 턴(알약 락 없음)에서는 아무 위에도 가능. 알약 락 중 무색은 불가(해당 색 알약으로 락을 거는 수는 가능). 공격 응답에서 낼 수 있는 무색은 **죽창·패스·역날검**뿐.

| 카드 | 효과 |
|------|------|
| 죽창 | +5. 3·4 불가. 패스·역날검·감수 가능. 스택에 한 장이라도 있으면 `spearInStack` |
| 패스 ×3 | 공격 응답 전용. 스택 유지한 채 다음 활성에게 넘김. Q에는 사용 불가 |
| 리버스 조커 | 조커값 순환 `BW ← COLOR ← MOON ← BW`. 이미 쌓인 스택은 불변. 공개 top은 이 장(와일드) |
| 역날검 | 공격 응답 전용. 직전 활성에게 `2×스택` 새 응답. 체인당 1회. 죽창 속성은 유지 |
| 미러 룸 | 일반 턴. 낸 뒤 내 손패 N. 마지막 장이면 1위, 효과 없음. 다른 좌석은 N에 맞춤(초과는 본인 선택 버림·효과 없음, 부족은 드로우). 방향의 다음부터. 처리 중 0장이면 그 좌석 1위 |
| 알약 | 일반 턴. 1장 드로우 후 `requiredColor`. 7 무늬보다 우선. 그 색을 **실제로 내면** 해제. 알약만 반복하면 락 색만 변경. 드로우 때문에 알약 단독 피니시 없음 |

### 공격 / Q 체인

동시에 열리지 않음. 공격 응답: 전이 / 패스 / 역날검 / 방어(3·4) / 감수(스택 전부 드로우, 추가 수 없음). Q 응답: Q 전이(`queenStack+=1`) / 3·4 / 감수. 감수 시 **마지막 Q 좌석**이 남은 손패에서 `queenStack`장 지급(부족분은 덱). 지급 후 0장이면 1위.

### 승패 · 활성 · AFK

손패 0 = 즉시 1위(잔여 순위전 없음). 나머지는 장수 오름차순, 동률은 점수 낮은 쪽. 기권은 최하위. 빠진 자리는 건너뜀.

초과: 일반=드로우1, 공격=스택 전부, Q선택=Reverse, Q응답=감수, Q지급=점수 높은 순, K선택=K만, K숨김=점수 낮은 1장, 7=원래 무늬, 미러 버림=점수 높은 순. **연속 타임아웃 3회=기권**.

재접속 45초. 스냅샷: 공개 상태 + 내 손패 + 채팅 최근 50 + 남은 턴. 초과=기권. 손패는 버림에 넣지 않고 매치에서 제거.

하우스룰(커스텀만): `DrawAndPlay=true`, `JokerDefendable=true`, `ContinueAfterFirstWin=false`, `TurnSeconds=15`. 퀵매치는 항상 Official.

공개 정보: discardTop(K 숨김 제외), requiredSuit/Color, jokerAttack, 방향, 턴, 타이머, 좌석 장수, attackStack/spearInStack, queenStack, 덱 장수, 최근 버림 8장(숨김 제외). 비공개: 타인 손패, 덱 순서, 시드.

## 6. 커맨드 / 이벤트

클라→서버: Ready, StartMatch, PlayCard(instanceId), Draw, ChooseSuit, ChooseQueenMode(Reverse|Give), AcceptQueen, GiveCards, ChooseKingMode(Extra|Hide), HideUnder, MirrorDiscard, Surrender, Chat, RematchVote, Heartbeat, SnapshotRequest.

공개 이벤트: RoomUpdated, MatchStarted, TurnChanged, CardPlayed, DrewCount, QueenModeChosen, QueenGiven, KingModeChosen, KingHidden, JokerValues, ColorLock, MirrorAdjusted(장수만), SuitChanged, PlayerDisconnected/Rejoined/Out, Chat, MatchEnded.

개인: HandGranted, CardDrawn, CardsReceived, Reject.

Reject: NotYourTurn, IllegalCard, NotInHand, NotAttackResponse, NotQueenResponse, NeedSuitPick, NeedQueenMode, NeedGiveCards, GiveCountMismatch, NeedKingMode, NeedHideUnder, NoCardToHide, SpearNotDefendable, CounterAlreadyUsed, NeedMirrorDiscard, ColorLocked, ChatRate, ChatEmpty, VersionMismatch, SeatTaken, RoomFull, MatchAlreadyStarted, GraceExpired.

MVP는 ack 후 연출(예측 없음). 연출 캡 800ms. 타이머는 서버 `deadlineMs`.

## 7. 세션 · 매칭

게스트 인증 → 로비: 퀵매치 2/4/6, 방 만들기, 6자리 룸코드. 봇 충원 없음. 90초 후 인원 하향 제안. 대기실: 준비, 방장 Start, 인원≥2. 시작 후 난입 없음.

결과: 순위·장수·점수, 재대결 20초(미투표=반대). 방 유지 후 대기실.

프로토콜: `protocolMajor.minor`. major 불일치 접속 거부. MVP는 minor 달라도 룸코드 거부. 리전 `ap-northeast` 단일.

## 8. UI / 입력

조작은 **2단 확정**(선택 → 같은 카드 재탭/재클릭 또는 Enter). 드래그 앤 드롭은 PC 옵션이고 최종은 PlayCard 하나.

모든 게임 입력은 `GamePointer`로 모은다. 모바일=탭/재탭, PC=클릭/Enter, 해제는 Esc/우클릭/빈곳. 드로우=덱 또는 D. 문양=6버튼 또는 S/H/D/C/R/M. Q=R/G, K=E/H. 퀵챗=F1~F8.

합법=밝기 100%, 불법=투명 40%·선택 불가, 선택=+16px, ack까지 입력 잠금, REJECT=손 복귀. 공격 중 감수 버튼 라벨=`받기 (n장)`. HUD: 턴/초/방향, 요구 무늬, +n 스택, Q×n, 조커값 `흑n 빨n 파n`, 알약 뱃지, 덱 장수. 기권은 두 번 확인.

레이아웃 프리셋은 OS가 아니라 해상도: `MobilePortrait`(1080×1920), `MobileLandscape`, `PcLandscape`(1920×1080). Safe Area 안에 손패. 2~4인 십자, 5~6인 상단 아크.

카피: 짧은 명령형. 우노 등 타사 IP 명칭 금지. MVP 문자열 한국어, 키 `ui.*` / `sys.*`.

## 9. 채팅

채널 `room`(대기실), `match`(시작~결과 이탈). 대기실 최근 30줄을 매치 상단에 이어 붙임. type: user / quick / system / emote. 퀵챗 id: q_nice, q_gg, q_hurry, q_go, q_oops, q_wow, q_thanks, q_again. 본문 최대 80자. IME 조합 중 Enter 전송 금지. 서버 레이트 1.2초, 금칙 `***`. 매치 중 Kick 없음.

## 10. 완료 / 코드 규약

- 클래스/메서드 PascalCase, 로컬 camelCase, private 필드 `_camelCase`
- View만 MonoBehaviour. Presenter에 규칙 판정을 두지 않음 (`LegalMove`는 힌트)
- 타인 카드 앞면 스프라이트를 붙이지 않음
- 컴파일 에러 0. Official 규칙 테스트 그린
- Phase 3+ (스토어 계정, Elo, 스킨, 영문, 관전, 예측 연출)는 이 로드맵에 넣지 않음

## 11. 프리팹 고정 UI (버그픽스)

Title / Lobby / Room / Match / Result 패널은 **빈 루트 + Play 시 자식 생성**이 아니다.

- 장수가 매 턴 바뀌지 않는 위젯(제목, 상태, 버튼, 입력, HUD, 선택 시트, 대기실 슬롯 6, 퀵챗 8, 결과 순위/재대결, 상대 좌석 슬롯 최대 5)은 해당 패널 프리팹의 **자식으로 미리 두고** View의 SerializeField 에 배선한다.
- 프리팹 YAML 에서 그 필드가 {fileID: 0} 이면 미배선이다. 스크립트 필드가 바뀌면 프리팹도 같이 고친다.
- EnsureLayout / FindOrCreate 는 고정 자식을 new GameObject 로 만들지 않는다. 이미 있는 자식을 찾아 이벤트를 묶거나, 꺼진 슬롯을 켜고 끄는 것만 한다.
- 예외(동적 Instantiate 허용): 내 손패 CardView(템플릿 1개를 프리팹에 두고 복제), 채팅 로그 줄. 템플릿 자체는 프리팹 자식이다.

## 12. 카드 아트 해상도와 조커 3종

앞면 89장과 뒷면 `BACK` 은 모두 **768×1080** (비율 32:45). 저해상도 원본을 단순 확대한 것이 아니라 그 크기로 다시 그린다. 경로는 `Assets/GameResource/Data/Cards/`, Addressables 주소는 기존 `Cards/{CardDefId}` · `Cards/BACK` 을 유지한다.

조커 3장은 카드 중앙에 **정확한 정원(원)** 을 두고 그 안에서만 그림을 그린다. 타사 IP를 그대로 베끼지 않는다.

- `JOKER:BW` (흑): 원 안에 초승달 얼굴. 흑색. **입가에 피 없음**
- `JOKER:COLOR` (적): 같은 달 얼굴. 적색. **입가에서 피가 흐른다**
- `JOKER:MOON` (청): 같은 달 얼굴. 청색. 원 안 빈칸은 별로 채운다. **피 없음**

숫자·무늬 장과 무색 특수·뒷면도 768×1080 이다. 규칙 판정·CardDefId·Addressables 키는 바꾸지 않는다.
