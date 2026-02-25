using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

// ------------------------------------------
// GameManager - Host-Authoritative
//
// 권한 분리
// 호스트만        : 게임 로직, 점수/라운드 계산, 상태 전이
// 모든 클라이언트 : RPC 수신 후 UI 업데이트만
// 게스트 튕김     : 호스트만 60초 대기 -> 초과 시 전체 리셋
// 호스트 튕김     : 게스트 즉시 로비 이동 + 팝업
// ------------------------------------------
public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("State")]
    [SerializeField] private GameState state = GameState.WaitingForPlayers;

    [Header("Round / Score")]
    [SerializeField] private int roundIndex = 1;
    [SerializeField] private int maxRounds = 5;
    [SerializeField] private int playerScore = 0;
    [SerializeField] private int enemyScore = 0;
    [SerializeField] private int winScore = 7;

    [Header("UI - Text")]
    [SerializeField] private Text roundText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text playerStateText;
    [SerializeField] private Text enemyStateText;
    [SerializeField] private Text playerResultText;
    [SerializeField] private Text enemyResultText;

    [Header("UI - Center Cards")]
    [SerializeField] private Image playerSelectImage;
    [SerializeField] private Image enemySelectImage;

    [Header("Main Button")]
    [SerializeField] private Button readyButton;
    [SerializeField] private Text readyButtonText;

    [Header("Winner Choice Buttons")]
    [SerializeField] private Button chooseSlaveButton;
    [SerializeField] private Button chooseKingButton;

    [Header("Hand Cards")]
    [SerializeField] private List<HandCardButton> handCards;

    [Header("Sprites")]
    [SerializeField] private Sprite backSprite;
    [SerializeField] private Sprite kingSprite;
    [SerializeField] private Sprite citizenSprite;
    [SerializeField] private Sprite slaveSprite;

    [Header("KeyCard Index")]
    [SerializeField] private int keyCardIndex = 2;

    [Header("Connection UI")]
    [SerializeField] private Image leftConnBox;
    [SerializeField] private Text leftConnText;
    [SerializeField] private Image rightConnBox;
    [SerializeField] private Text rightConnText;

    [Header("Conn Colors")]
    [SerializeField] private Color connGreen = new Color(0f, 1f, 0f, 1f);
    [SerializeField] private Color connRed = new Color(1f, 0f, 0f, 1f);
    [SerializeField] private Color connOrange = new Color(1f, 0.55f, 0f, 1f);

    [Header("Reconnect Grace (seconds)")]
    [SerializeField] private float rejoinGraceSeconds = 60f;

    [Header("Scene")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    [Header("호스트 연결 끊김 팝업")]
    [SerializeField] private GameObject disconnectPopup;

    // -- 내부 상수 ----------------------------
    private const string PROP_READY = "ready";
    private const string ROOM_STARTED = "started";

    // -- 픽 / 제출 ----------------------------
    private CardType? playerPick = null;
    private CardType? enemyPick = null;
    private int selectedHandIdx = -1;
    private HashSet<int> usedHandIdx = new();

    private bool selfSubmitted = false;
    private bool enemySubmitted = false;
    private bool selfIsWinner = false;

    // -- 덱 ----------------------------------
    // 덱은 오직 RPC_ApplyInitialDeckKeys / RPC_WinnerChoseRole 에서만 세팅
    private CardType selfDeckKey = CardType.Citizen;
    private CardType enemyDeckKey = CardType.Citizen;

    // -- 재접속 유예 (호스트 전용) -------------
    private Coroutine waitRejoinCo = null;
    private bool isWaitingRejoin = false;

    // -- 승리 여부 (Game_End UI용) -------------
    private bool gameEndIsWin = false;

    // =========================================
    // Unity Lifecycle
    // =========================================

    private void Awake()
    {
        if (readyButton) readyButton.onClick.AddListener(OnClickMainButton);
        if (chooseSlaveButton) chooseSlaveButton.onClick.AddListener(() => OnClickWinnerChoice(CardType.Slave));
        if (chooseKingButton) chooseKingButton.onClick.AddListener(() => OnClickWinnerChoice(CardType.King));

        for (int i = 0; i < handCards.Count; i++)
        {
            int idx = i;
            if (handCards[i]) handCards[i].Init(idx, OnSelectHandCard);
        }

        if (disconnectPopup) disconnectPopup.SetActive(false);
    }

    private void Start()
    {
        SetConnLeft(true);
        RefreshOpponentConnUI();
        EnterState(GameState.WaitingForPlayers);
    }

    // =========================================
    // State Machine
    // =========================================

    private void EnterState(GameState next)
    {
        state = next;
        UpdateHUD();

        switch (state)
        {
            case GameState.WaitingForPlayers:
                HideRound(true);
                SetWinnerChoiceInteractable(false, false);
                SetHandInteractable(false);
                ClearPickUI();
                if (playerStateText) playerStateText.text = "대기중";
                if (enemyStateText) enemyStateText.text = "대기중";
                RefreshOpponentConnUI();
                RefreshMainButtonWaiting();
                break;

            case GameState.Round_Pick:
                HideRound(false);
                ClearPickUI();
                if (playerStateText) playerStateText.text = "카드 선택";
                if (enemyStateText) enemyStateText.text = "선택중";
                RefreshHandCardInteractable();
                SetMainButton("제출", false);
                SetWinnerChoiceInteractable(false, false);
                break;

            case GameState.Round_Reveal:
                if (playerStateText) playerStateText.text = "제출 완료";
                if (enemyStateText) enemyStateText.text = "제출 완료";
                SetMainButton("처리중", false);
                SetHandInteractable(false);
                if (PhotonNetwork.IsMasterClient)
                    StartCoroutine(CoHostCalcResult());
                break;

            case GameState.Round_Result:
                SetMainButton("대기", false);
                SetWinnerChoiceInteractable(false, false);
                SetHandInteractable(false);
                break;

            case GameState.Winner_Choose:
                SetHandInteractable(false);
                SetMainButton("대기", false);
                if (selfIsWinner)
                {
                    if (playerStateText) playerStateText.text = "덱 선택";
                    if (enemyStateText) enemyStateText.text = "상대 선택중";
                    SetWinnerChoiceInteractable(true, true);
                }
                else
                {
                    if (playerStateText) playerStateText.text = "상대 선택중";
                    if (enemyStateText) enemyStateText.text = "덱 선택";
                    SetWinnerChoiceInteractable(false, false);
                }
                break;

            case GameState.Game_End:
                SetWinnerChoiceInteractable(false, false);
                SetHandInteractable(false);
                SetMainButton("새 게임", PhotonNetwork.IsMasterClient);
                if (playerStateText) playerStateText.text = "게임 종료";
                if (enemyStateText) enemyStateText.text = "게임 종료";
                break;
        }
    }

    private void UpdateHUD()
    {
        if (scoreText) scoreText.text = $"{playerScore} : {enemyScore}";

        if (!roundText) return;

        if (state == GameState.Game_End)
        {
            // gameEndIsWin: 7점 달성했을 때만 true
            roundText.text = gameEndIsWin ? "<- WIN" : "WIN ->";
            roundText.gameObject.SetActive(true);
        }
        else if (roundIndex >= maxRounds)
            roundText.text = "Final Round";
        else
            roundText.text = $"{roundIndex} Round";
    }

    private void HideRound(bool hide)
    {
        if (roundText) roundText.gameObject.SetActive(!hide);
    }

    private void ClearPickUI()
    {
        playerPick = null;
        enemyPick = null;
        selectedHandIdx = -1;
        selfSubmitted = false;
        enemySubmitted = false;

        if (playerSelectImage) playerSelectImage.sprite = null;
        if (enemySelectImage) enemySelectImage.sprite = null;
        if (playerResultText) playerResultText.text = "";
        if (enemyResultText) enemyResultText.text = "";

        for (int i = 0; i < handCards.Count; i++)
            if (handCards[i]) handCards[i].SetSelected(false);
    }

    // =========================================
    // 메인 버튼
    // =========================================

    private void OnClickMainButton()
    {
        switch (state)
        {
            case GameState.WaitingForPlayers: OnClickWaitingMain(); break;
            case GameState.Round_Pick: OnClickSubmit(); break;
            case GameState.Game_End: OnClickNewGame(); break;
        }
    }

    private void OnClickWaitingMain()
    {
        if (!PhotonNetwork.InRoom)
        {
            EnterState(GameState.Round_Pick);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            if (!HasOpponent() || !IsOpponentReady()) return;

            PhotonNetwork.CurrentRoom.SetCustomProperties(
                new PhotonHashtable { { ROOM_STARTED, true } });
            DealDeckRandomAndBroadcast();
            photonView.RPC(nameof(RPC_StartGame), RpcTarget.AllViaServer);
        }
        else
        {
            if (!IsSelfReady()) SyncReady(true);
            RefreshMainButtonWaiting();
        }
    }

    private void OnClickNewGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!PhotonNetwork.InRoom)
        {
            ResetScoreAndRound();
            SyncReady(false);
            EnterState(GameState.WaitingForPlayers);
            return;
        }

        photonView.RPC(nameof(RPC_NewGame), RpcTarget.AllViaServer);
    }

    private void RefreshMainButtonWaiting()
    {
        if (isWaitingRejoin) return;

        if (!PhotonNetwork.InRoom)
        {
            SetMainButton("준비", true);
            return;
        }

        RefreshOpponentConnUI();

        if (PhotonNetwork.IsMasterClient)
            SetMainButton("게임 시작", HasOpponent() && IsOpponentReady());
        else
        {
            if (IsSelfReady()) SetMainButton("준비완료", false);
            else SetMainButton("준비", HasOpponent());
        }
    }

    private void SetMainButton(string label, bool interactable)
    {
        if (readyButton) readyButton.interactable = interactable;
        if (readyButtonText) readyButtonText.text = label;
    }

    // =========================================
    // 손패 선택 / 제출
    // =========================================

    private void OnSelectHandCard(int handIndex, CardType cardType)
    {
        if (state != GameState.Round_Pick) return;
        if (selfSubmitted) return;
        if (usedHandIdx.Contains(handIndex)) return;

        selectedHandIdx = handIndex;
        playerPick = cardType;

        if (playerSelectImage) playerSelectImage.sprite = GetSprite(cardType);

        for (int i = 0; i < handCards.Count; i++)
            if (handCards[i]) handCards[i].SetSelected(i == handIndex);

        if (playerStateText) playerStateText.text = "선택 완료";
        SetMainButton("제출", true);
    }

    private void OnClickSubmit()
    {
        if (state != GameState.Round_Pick) return;
        if (playerPick == null || selectedHandIdx < 0) return;
        if (selfSubmitted) return;

        selfSubmitted = true;
        usedHandIdx.Add(selectedHandIdx);
        RefreshHandCardInteractable();

        SetMainButton("제출완료", false);
        if (playerStateText) playerStateText.text = "제출 완료";

        photonView.RPC(nameof(RPC_SubmitPick), RpcTarget.AllViaServer, (int)playerPick.Value);
    }

    // =========================================
    // [HOST ONLY] 결과 계산
    // =========================================

    private IEnumerator CoHostCalcResult()
    {
        float timeout = 10f;
        while (!(selfSubmitted && enemySubmitted) && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (playerPick == null || enemyPick == null)
        {
            Debug.LogWarning("[Host] 픽 수집 실패 - 타임아웃");
            yield break;
        }

        yield return new WaitForSeconds(0.35f);

        int result = Compare(playerPick.Value, enemyPick.Value);
        int newHostScore = playerScore;
        int newGuestScore = enemyScore;

        if (result > 0) newHostScore++;
        else if (result < 0) newGuestScore++;

        bool matchOver = (newHostScore >= winScore || newGuestScore >= winScore);
        bool roundMaxed = (!matchOver && result == 0 && roundIndex >= maxRounds);
        int nextRound = (result == 0 && !matchOver && !roundMaxed)
                          ? roundIndex + 1
                          : roundIndex;

        photonView.RPC(nameof(RPC_BroadcastResult), RpcTarget.AllViaServer,
            result, newHostScore, newGuestScore, nextRound, matchOver, roundMaxed);
    }

    // =========================================
    // Photon RPCs
    // =========================================

    [PunRPC]
    private void RPC_StartGame()
    {
        ResetScoreAndRound();
        ResetRoundLocal();
        HideRound(false);
        EnterState(GameState.Round_Pick);
    }

    [PunRPC]
    private void RPC_NewGame()
    {
        ResetScoreAndRound();
        ResetRoundLocal();
        SyncReady(false);
        EnterState(GameState.WaitingForPlayers);
    }

    [PunRPC]
    private void RPC_SubmitPick(int cardTypeInt, PhotonMessageInfo info)
    {
        var type = (CardType)cardTypeInt;
        bool isLocal = (info.Sender == PhotonNetwork.LocalPlayer);

        if (isLocal)
        {
            playerPick = type;
            selfSubmitted = true;
        }
        else
        {
            enemyPick = type;
            enemySubmitted = true;

            if (enemySelectImage) enemySelectImage.sprite = backSprite;
            if (enemyStateText) enemyStateText.text = "제출 완료";
        }

        if (PhotonNetwork.IsMasterClient && selfSubmitted && enemySubmitted)
            photonView.RPC(nameof(RPC_BroadcastReveal), RpcTarget.AllViaServer);
    }

    [PunRPC]
    private void RPC_BroadcastReveal()
    {
        EnterState(GameState.Round_Reveal);
    }

    [PunRPC]
    private void RPC_BroadcastResult(int result, int hostScore, int guestScore,
                                     int nextRound, bool matchOver, bool roundMaxed)
    {
        bool iAmHost = PhotonNetwork.IsMasterClient;
        playerScore = iAmHost ? hostScore : guestScore;
        enemyScore = iAmHost ? guestScore : hostScore;

        bool hostWon = (result > 0);
        selfIsWinner = iAmHost ? hostWon : !hostWon;

        if (enemyPick != null && enemySelectImage)
            enemySelectImage.sprite = GetSprite(enemyPick.Value);

        if (result == 0)
        {
            if (playerResultText) playerResultText.text = "DRAW";
            if (enemyResultText) enemyResultText.text = "DRAW";
        }
        else if (selfIsWinner)
        {
            if (playerResultText) playerResultText.text = "WIN";
            if (enemyResultText) enemyResultText.text = "LOSE";
        }
        else
        {
            if (playerResultText) playerResultText.text = "LOSE";
            if (enemyResultText) enemyResultText.text = "WIN";
        }

        UpdateHUD();
        EnterState(GameState.Round_Result);
        StartCoroutine(CoPostResult(result, nextRound, matchOver, roundMaxed));
    }

    private IEnumerator CoPostResult(int result, int nextRound, bool matchOver, bool roundMaxed)
    {
        yield return new WaitForSeconds(0.8f);

        if (matchOver)
        {
            // 7점 달성 -> WIN/LOSE 표시
            gameEndIsWin = selfIsWinner;
            EnterState(GameState.Game_End);
            yield break;
        }

        if (roundMaxed)
        {
            // 파이널 라운드 무승부 -> WIN 표시 없이 종료
            gameEndIsWin = false;
            EnterState(GameState.Game_End);
            yield break;
        }

        if (result == 0)
        {
            // 무승부 -> 라운드 올리고 계속
            roundIndex = nextRound;
            UpdateHUD();
            EnterState(GameState.Round_Pick);
            yield break;
        }

        // 승패 발생 -> Round 1로 초기화 후 덱 선택
        roundIndex = 1;
        UpdateHUD();
        EnterState(GameState.Winner_Choose);
    }

    [PunRPC]
    private void RPC_WinnerChoseRole(int choiceInt, PhotonMessageInfo info)
    {
        var winnerKey = (CardType)choiceInt;
        var loserKey = (winnerKey == CardType.King) ? CardType.Slave : CardType.King;

        bool iAmWinner = (info.Sender == PhotonNetwork.LocalPlayer);
        selfDeckKey = iAmWinner ? winnerKey : loserKey;
        enemyDeckKey = iAmWinner ? loserKey : winnerKey;

        ApplyDeckToHandUI();
        ResetRoundLocal();
        EnterState(GameState.Round_Pick);
    }

    [PunRPC]
    private void RPC_ForceResetToWaiting()
    {
        isWaitingRejoin = false;
        ResetScoreAndRound();
        ResetRoundLocal();
        SetConnRightWaiting();
        SyncReady(false);
        EnterState(GameState.WaitingForPlayers);
    }

    [PunRPC]
    private void RPC_SyncStateToRejoin(int stateInt, int round,
                                        int hostScore, int guestScore,
                                        int hostKey, int guestKey)
    {
        roundIndex = round;

        bool iAmHost = PhotonNetwork.IsMasterClient;
        playerScore = iAmHost ? hostScore : guestScore;
        enemyScore = iAmHost ? guestScore : hostScore;

        selfDeckKey = iAmHost ? (CardType)hostKey : (CardType)guestKey;
        enemyDeckKey = iAmHost ? (CardType)guestKey : (CardType)hostKey;

        ApplyDeckToHandUI();
        UpdateHUD();
        EnterState((GameState)stateInt);
    }

    [PunRPC]
    private void RPC_ApplyInitialDeckKeys(int hostKeyInt, int guestKeyInt)
    {
        bool iAmHost = PhotonNetwork.IsMasterClient;
        selfDeckKey = iAmHost ? (CardType)hostKeyInt : (CardType)guestKeyInt;
        enemyDeckKey = iAmHost ? (CardType)guestKeyInt : (CardType)hostKeyInt;
        ApplyDeckToHandUI();
    }

    // =========================================
    // 덱 / 키카드
    // =========================================

    private void DealDeckRandomAndBroadcast()
    {
        bool hostIsKing = (Random.Range(0, 2) == 0);
        var hostKey = hostIsKing ? CardType.King : CardType.Slave;
        var guestKey = hostIsKing ? CardType.Slave : CardType.King;

        photonView.RPC(nameof(RPC_ApplyInitialDeckKeys), RpcTarget.AllViaServer,
            (int)hostKey, (int)guestKey);
    }

    private void ApplyDeckToHandUI()
    {
        if (handCards == null) return;
        if (keyCardIndex < 0 || keyCardIndex >= handCards.Count) return;
        var key = handCards[keyCardIndex];
        if (!key) return;
        key.SetCard(selfDeckKey, GetSprite(selfDeckKey));
    }

    // =========================================
    // 리셋
    // =========================================

    // 점수와 라운드만 초기화 (덱 건드리지 않음)
    private void ResetScoreAndRound()
    {
        roundIndex = 1;
        playerScore = 0;
        enemyScore = 0;
    }

    // 라운드 단위 초기화 (점수/덱 유지)
    private void ResetRoundLocal()
    {
        usedHandIdx.Clear();
        selectedHandIdx = -1;
        playerPick = null;
        enemyPick = null;
        selfSubmitted = false;
        enemySubmitted = false;

        if (playerSelectImage) playerSelectImage.sprite = null;
        if (enemySelectImage) enemySelectImage.sprite = null;
        if (playerResultText) playerResultText.text = "";
        if (enemyResultText) enemyResultText.text = "";

        for (int i = 0; i < handCards.Count; i++)
            if (handCards[i]) handCards[i].SetSelected(false);

        RefreshHandCardInteractable();
    }

    // =========================================
    // 손패 인터랙션
    // =========================================

    private void SetHandInteractable(bool value)
    {
        for (int i = 0; i < handCards.Count; i++)
            if (handCards[i]) handCards[i].SetInteractable(value);
    }

    private void RefreshHandCardInteractable()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            bool used = usedHandIdx.Contains(i);
            bool canClick = !used && state == GameState.Round_Pick && !selfSubmitted;
            if (handCards[i])
            {
                handCards[i].SetInteractable(canClick);
                handCards[i].SetUsedVisual(used);
            }
        }
    }

    private void SetWinnerChoiceInteractable(bool slave, bool king)
    {
        if (chooseSlaveButton) chooseSlaveButton.interactable = slave;
        if (chooseKingButton) chooseKingButton.interactable = king;
    }

    private void OnClickWinnerChoice(CardType choice)
    {
        if (state != GameState.Winner_Choose) return;
        if (!selfIsWinner) return;

        SetWinnerChoiceInteractable(false, false);
        photonView.RPC(nameof(RPC_WinnerChoseRole), RpcTarget.AllViaServer, (int)choice);
    }

    // =========================================
    // Ready / Properties
    // =========================================

    private void SyncReady(bool ready)
    {
        if (!PhotonNetwork.InRoom) return;
        PhotonNetwork.LocalPlayer.SetCustomProperties(
            new PhotonHashtable { { PROP_READY, ready } });
    }

    private bool IsSelfReady() => GetBoolProp(PhotonNetwork.LocalPlayer, PROP_READY);
    private bool IsOpponentReady() => GetBoolProp(GetOpponent(), PROP_READY);

    private static bool GetBoolProp(Player p, string key)
    {
        if (p?.CustomProperties == null) return false;
        return p.CustomProperties.TryGetValue(key, out var v) && (bool)v;
    }

    private bool HasOpponent() => GetOpponent() != null;
    private Player GetOpponent()
    {
        if (!PhotonNetwork.InRoom) return null;
        foreach (var kv in PhotonNetwork.CurrentRoom.Players)
            if (kv.Value != PhotonNetwork.LocalPlayer) return kv.Value;
        return null;
    }

    // =========================================
    // Photon Callbacks
    // =========================================

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        SetConnLeft(true);
        RefreshOpponentConnUI();
        RefreshMainButtonWaiting();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        base.OnPlayerEnteredRoom(newPlayer);

        if (!PhotonNetwork.IsMasterClient) return;

        // 재접속 대기 코루틴 중단
        if (waitRejoinCo != null)
        {
            StopCoroutine(waitRejoinCo);
            waitRejoinCo = null;
            isWaitingRejoin = false;
        }

        SetConnRightConnected();

        if (state != GameState.WaitingForPlayers)
        {
            // 게임 중 재접속 -> 게스트에게 상태 전송 후 호스트 UI 복구
            photonView.RPC(nameof(RPC_SyncStateToRejoin), newPlayer,
                (int)state, roundIndex,
                playerScore, enemyScore,
                (int)selfDeckKey, (int)enemyDeckKey);
            EnterState(state);
        }
        else
        {
            RefreshMainButtonWaiting();
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        base.OnPlayerLeftRoom(otherPlayer);

        // 호스트만 재접속 대기 시작
        if (PhotonNetwork.IsMasterClient)
        {
            if (waitRejoinCo != null) StopCoroutine(waitRejoinCo);
            isWaitingRejoin = true;
            waitRejoinCo = StartCoroutine(CoWaitOpponentRejoin(rejoinGraceSeconds));
        }
        else
        {
            // 게스트 입장에서는 연결 UI만 업데이트
            SetConnRightWaiting();
        }
    }

    // 호스트가 나가면 -> 게스트 즉시 팝업 + 로비 이동
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        if (disconnectPopup) disconnectPopup.SetActive(true);

        // 즉시 방 나가기 -> OnLeftRoom 에서 로비씬으로 이동
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        base.OnLeftRoom();
        SceneManager.LoadScene(lobbySceneName);
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, PhotonHashtable changedProps)
    {
        base.OnPlayerPropertiesUpdate(targetPlayer, changedProps);
        if (state == GameState.WaitingForPlayers && !isWaitingRejoin)
            RefreshMainButtonWaiting();
    }

    // =========================================
    // 재접속 유예 코루틴 (호스트 전용)
    // =========================================

    private IEnumerator CoWaitOpponentRejoin(float seconds)
    {
        SetHandInteractable(false);
        SetMainButton("재접속 대기중", false);
        SetWinnerChoiceInteractable(false, false);

        float t = seconds;
        while (t > 0f)
        {
            if (rightConnBox) rightConnBox.color = connOrange;
            if (rightConnText) rightConnText.text = $"재접속중 {Mathf.CeilToInt(t)}";

            t -= 1f;
            yield return new WaitForSeconds(1f);
        }

        // 60초 초과 -> 전체 리셋
        isWaitingRejoin = false;
        waitRejoinCo = null;
        photonView.RPC(nameof(RPC_ForceResetToWaiting), RpcTarget.AllViaServer);
    }

    // =========================================
    // Connection UI
    // =========================================

    private void RefreshOpponentConnUI()
    {
        if (!PhotonNetwork.InRoom) { SetConnRightWaiting(); return; }
        if (HasOpponent()) SetConnRightConnected(); else SetConnRightWaiting();
    }

    private void SetConnLeft(bool connected)
    {
        if (leftConnBox) leftConnBox.color = connected ? connGreen : connRed;
        if (leftConnText) leftConnText.text = connected ? "접속중" : "접속대기";
    }

    private void SetConnRightWaiting()
    {
        if (rightConnBox) rightConnBox.color = connRed;
        if (rightConnText) rightConnText.text = "접속대기";
    }

    private void SetConnRightConnected()
    {
        if (rightConnBox) rightConnBox.color = connGreen;
        if (rightConnText) rightConnText.text = "접속중";
    }

    // =========================================
    // 게임 룰
    // =========================================

    // King > Citizen > Slave > King
    private int Compare(CardType a, CardType b)
    {
        if (a == b) return 0;
        if (a == CardType.King && b == CardType.Citizen) return 1;
        if (a == CardType.Citizen && b == CardType.Slave) return 1;
        if (a == CardType.Slave && b == CardType.King) return 1;
        return -1;
    }

    private Sprite GetSprite(CardType type) => type switch
    {
        CardType.King => kingSprite,
        CardType.Citizen => citizenSprite,
        CardType.Slave => slaveSprite,
        _ => null
    };
}