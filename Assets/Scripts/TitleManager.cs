using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;

// ------------------------------------------
// TitleManager
// 타이틀씬 담당
// - Photon 연결 대기
// - 연결 완료 후 게임 시작 버튼 활성화
// - 버튼 누르면 로비씬으로 이동
// ------------------------------------------
public class TitleManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    [SerializeField] private Button startButton;
    [SerializeField] private Text startButtonText;
    [SerializeField] private Text statusText;      // 연결 상태 표시 (없어도 됨)

    [Header("씬 이름")]
    [SerializeField] private string lobbySceneName = "LobbyScene";

    private void Start()
    {
        // 버튼 비활성 (연결 전)
        SetStartButton(false, "연결 중...");

        if (statusText) statusText.text = "서버에 연결 중...";

        startButton?.onClick.AddListener(OnClickStart);

        // 이미 연결돼 있으면 (Bootstrap DontDestroyOnLoad 씬 재진입 등)
        if (PhotonNetwork.IsConnected)
        {
            SetStartButton(true, "게임 시작");
            if (statusText) statusText.text = "";
        }
    }

    private void OnClickStart()
    {
        if (!PhotonNetwork.IsConnected)
        {
            if (statusText) statusText.text = "서버 연결 중입니다. 잠시 후 다시 시도해주세요.";
            return;
        }

        SetStartButton(false, "로비 이동 중...");
        PhotonNetwork.LoadLevel("Room");
    }

    // ------------------------------------------
    // Photon Callbacks
    // ------------------------------------------

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        SetStartButton(true, "게임 시작");
        if (statusText) statusText.text = "";
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        base.OnDisconnected(cause);
        SetStartButton(false, "연결 중...");
        if (statusText) statusText.text = $"연결 끊김: {cause}";
    }

    // ------------------------------------------
    // 헬퍼
    // ------------------------------------------

    private void SetStartButton(bool interactable, string label)
    {
        if (startButton) startButton.interactable = interactable;
        if (startButtonText) startButtonText.text = label;
    }
}