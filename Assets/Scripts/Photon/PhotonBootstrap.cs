using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

// ----------------------------------------------
//  PhotonBootstrap
//  - Photon 서버 연결만 담당
//  - DontDestroyOnLoad로 씬이 바뀌어도 유지
//  - 방 입장/생성은 LobbyManager가 담당
//
//  [사용법]
//  타이틀 씬 or 가장 먼저 로드되는 씬의
//  빈 게임오브젝트에 붙이면 됨
// ----------------------------------------------
public class PhotonBootstrap : MonoBehaviourPunCallbacks
{
    public static PhotonBootstrap Instance { get; private set; }

    [Header("Photon 설정")]
    [SerializeField] private string gameVersion = "mvp_v1";

    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (PhotonNetwork.IsConnected) return; // 이미 연결돼 있으면 스킵

        Debug.Log("[Photon] 서버 연결 시작");
        PhotonNetwork.AutomaticallySyncScene = true; // 호스트가 씬 로드하면 게스트도 따라감
        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    // ===========================================
    // Photon Callbacks
    // ===========================================

    public override void OnConnectedToMaster()
    {
        Debug.Log($"[Photon] 마스터 서버 연결 완료. Region={PhotonNetwork.CloudRegion} Ping={PhotonNetwork.GetPing()}ms");
        // 로비 입장은 LobbyManager가 알아서 함
        // 여기서 JoinLobby 하지 않음
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning($"[Photon] 연결 끊김: {cause}");

        // 의도치 않은 연결 끊김이면 재연결 시도
        if (cause != DisconnectCause.DisconnectByClientLogic)
        {
            Debug.Log("[Photon] 재연결 시도...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Photon] 방 입장: {PhotonNetwork.CurrentRoom.Name} " +
                  $"({PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers}) " +
                  $"호스트={PhotonNetwork.IsMasterClient}");
    }

    public override void OnLeftRoom()
    {
        Debug.Log("[Photon] 방 퇴장");
    }
}