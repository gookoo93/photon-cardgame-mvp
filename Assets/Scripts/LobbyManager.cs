using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

// ----------------------------------------------
//  LobbyManager
//  - 방 목록 표시 / 새로고침 / 방 검색
//  - 방 생성 팝업 / 코드 참가 팝업 열기
//  - 방 클릭 참가 (공개방 즉시 / 비공개방 비번 입력)
// ----------------------------------------------
public class LobbyManager : MonoBehaviourPunCallbacks
{
    public static LobbyManager Instance { get; private set; }

    [Header("버튼")]
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button codeJoinButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button searchButton;

    [Header("방 검색 입력창")]
    [SerializeField] private InputField searchInputField;

    [Header("방 목록")]
    [SerializeField] private Transform roomListContent;
    [SerializeField] private GameObject roomListItemPrefab;

    [Header("팝업 패널")]
    [SerializeField] private GameObject createRoomPanel;
    [SerializeField] private GameObject codeJoinPanel;
    [SerializeField] private GameObject notFoundPanel;      // "방을 찾을 수 없습니다"
    [SerializeField] private GameObject playerFullPanel;    // "인원이 초과되었습니다"
    [SerializeField] private GameObject passwordPanel;      // 비밀번호 입력 팝업

    [Header("비밀번호 팝업")]
    [SerializeField] private InputField passwordInputField;
    [SerializeField] private Button passwordConfirmButton;
    [SerializeField] private Button passwordCancelButton;
    [SerializeField] private Text passwordRoomNameText;

    // -- 내부 상태 -----------------------------
    private readonly Dictionary<string, RoomInfo> cachedRoomList = new();
    private string pendingRoomName = "";

    // -- Photon Custom Properties 키 -----------
    public const string PROP_CODE = "code";
    public const string PROP_PASSWORD = "pw";
    public const string PROP_TITLE = "title";

    // ===========================================
    // Unity Lifecycle
    // ===========================================

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        createRoomButton?.onClick.AddListener(OpenCreateRoomPanel);
        codeJoinButton?.onClick.AddListener(OpenCodeJoinPanel);
        refreshButton?.onClick.AddListener(RefreshRoomList);
        searchButton?.onClick.AddListener(OnClickSearch);

        passwordConfirmButton?.onClick.AddListener(OnConfirmPassword);
        passwordCancelButton?.onClick.AddListener(() => SetPanelActive(passwordPanel, false));

        // 모든 팝업 닫기
        SetPanelActive(createRoomPanel, false);
        SetPanelActive(codeJoinPanel, false);
        SetPanelActive(notFoundPanel, false);
        SetPanelActive(playerFullPanel, false);
        SetPanelActive(passwordPanel, false);

        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();
    }

    // ===========================================
    // 팝업 열기
    // ===========================================

    public void OpenCreateRoomPanel() => SetPanelActive(createRoomPanel, true);
    public void OpenCodeJoinPanel() => SetPanelActive(codeJoinPanel, true);
    public void ShowNotFoundPanel() => SetPanelActive(notFoundPanel, true);
    public void ShowPlayerFullPanel() => SetPanelActive(playerFullPanel, true);

    // ===========================================
    // 방 목록
    // ===========================================

    private void RefreshRoomList()
    {
        if (PhotonNetwork.InLobby)
        {
            PhotonNetwork.LeaveLobby();
            PhotonNetwork.JoinLobby();
        }
        else
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void OnClickSearch()
    {
        string keyword = searchInputField?.text.Trim() ?? "";
        RebuildRoomListUI(keyword);
    }

    private void RebuildRoomListUI(string keyword = "")
    {
        foreach (Transform child in roomListContent)
            Destroy(child.gameObject);

        foreach (var kv in cachedRoomList)
        {
            RoomInfo info = kv.Value;
            if (!info.IsOpen || !info.IsVisible) continue;

            string title = GetStringProp(info, PROP_TITLE, info.Name);
            if (!string.IsNullOrEmpty(keyword) &&
                !title.Contains(keyword, System.StringComparison.OrdinalIgnoreCase))
                continue;

            GameObject go = Instantiate(roomListItemPrefab, roomListContent);
            var item = go.GetComponent<RoomListItem>();
            if (item != null)
                item.Setup(info, OnClickRoomItem);
        }
    }

    private void OnClickRoomItem(RoomInfo info)
    {
        // 인원 초과 체크
        if (info.PlayerCount >= info.MaxPlayers)
        {
            ShowPlayerFullPanel();
            return;
        }

        string pw = GetStringProp(info, PROP_PASSWORD, "");

        if (!string.IsNullOrEmpty(pw))
        {
            // 비공개방 → 비밀번호 팝업
            pendingRoomName = info.Name;
            if (passwordRoomNameText)
                passwordRoomNameText.text = GetStringProp(info, PROP_TITLE, info.Name);
            if (passwordInputField)
                passwordInputField.text = "";
            SetPanelActive(passwordPanel, true);
        }
        else
        {
            JoinRoom(info.Name, "");
        }
    }

    private void OnConfirmPassword()
    {
        string inputPw = passwordInputField?.text ?? "";
        SetPanelActive(passwordPanel, false);
        JoinRoom(pendingRoomName, inputPw);
    }

    public void JoinRoom(string roomName, string enteredPassword)
    {
        if (!cachedRoomList.TryGetValue(roomName, out var info))
        {
            ShowNotFoundPanel();
            return;
        }

        // 인원 초과 재확인
        if (info.PlayerCount >= info.MaxPlayers)
        {
            ShowPlayerFullPanel();
            return;
        }

        string realPw = GetStringProp(info, PROP_PASSWORD, "");
        if (!string.IsNullOrEmpty(realPw) && realPw != enteredPassword)
        {
            // 비밀번호 불일치 → 팝업 다시 열기
            if (passwordInputField) passwordInputField.text = "";
            SetPanelActive(passwordPanel, true);
            Debug.Log("[Lobby] 비밀번호 불일치");
            return;
        }

        PhotonNetwork.JoinRoom(roomName);
    }

    public void JoinRoomByCode(string code)
    {
        foreach (var kv in cachedRoomList)
        {
            string roomCode = GetStringProp(kv.Value, PROP_CODE, "");
            if (roomCode != code) continue;

            RoomInfo info = kv.Value;

            // 인원 초과 체크
            if (info.PlayerCount >= info.MaxPlayers)
            {
                SetPanelActive(codeJoinPanel, false);
                ShowPlayerFullPanel();
                return;
            }

            string pw = GetStringProp(info, PROP_PASSWORD, "");
            if (!string.IsNullOrEmpty(pw))
            {
                pendingRoomName = kv.Key;
                if (passwordRoomNameText)
                    passwordRoomNameText.text = GetStringProp(info, PROP_TITLE, kv.Key);
                if (passwordInputField) passwordInputField.text = "";
                SetPanelActive(codeJoinPanel, false);
                SetPanelActive(passwordPanel, true);
            }
            else
            {
                SetPanelActive(codeJoinPanel, false);
                JoinRoom(kv.Key, "");
            }
            return;
        }

        SetPanelActive(codeJoinPanel, false);
        ShowNotFoundPanel();
    }

    // ===========================================
    // Photon Callbacks
    // ===========================================

    public override void OnJoinedLobby()
    {
        base.OnJoinedLobby();
        cachedRoomList.Clear();
        RebuildRoomListUI();
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        base.OnRoomListUpdate(roomList);

        foreach (var info in roomList)
        {
            if (info.RemovedFromList)
                cachedRoomList.Remove(info.Name);
            else
                cachedRoomList[info.Name] = info;
        }

        string keyword = searchInputField?.text.Trim() ?? "";
        RebuildRoomListUI(keyword);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        if (PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("Main");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        base.OnJoinRoomFailed(returnCode, message);
        Debug.LogWarning($"[Lobby] JoinRoomFailed: {returnCode} / {message}");

        // 에러 코드 32765 = 방 꽉 참
        if (returnCode == 32765)
            ShowPlayerFullPanel();
        else
            ShowNotFoundPanel();
    }

    // ===========================================
    // 유틸
    // ===========================================

    public static string GetStringProp(RoomInfo info, string key, string fallback = "")
    {
        if (info.CustomProperties != null && info.CustomProperties.TryGetValue(key, out var val))
            return val?.ToString() ?? fallback;
        return fallback;
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel) panel.SetActive(active);
    }
}