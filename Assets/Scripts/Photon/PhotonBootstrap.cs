using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class PhotonBootstrap : MonoBehaviourPunCallbacks
{
    [Header("Room")]
    [SerializeField] private string roomName = "room_mvp";
    [SerializeField] private byte maxPlayers = 2;

    private void Start()
    {
        Debug.Log("[Photon] Boot Start");

        // 필수: 자동 씬 동기화(방장이 씬 로드하면 모두 따라가게)
        PhotonNetwork.AutomaticallySyncScene = true;

        // 버전이 다르면 매칭이 갈라지니, 프로젝트 고정 문자열 권장
        PhotonNetwork.GameVersion = "mvp_v1";

        // 바로 마스터 서버 접속
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log($"[Photon] ConnectedToMaster. Region={PhotonNetwork.CloudRegion} Ping={PhotonNetwork.GetPing()}");

        // 로비 진입 (룸 리스트/매칭 기반 준비)
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("[Photon] JoinedLobby -> Joining or Creating Room...");

        var options = new RoomOptions
        {
            MaxPlayers = maxPlayers,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"[Photon] JoinedRoom: {PhotonNetwork.CurrentRoom.Name} " +
                  $"Players={PhotonNetwork.CurrentRoom.PlayerCount}/{PhotonNetwork.CurrentRoom.MaxPlayers} " +
                  $"IsMaster={PhotonNetwork.IsMasterClient}");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[Photon] Disconnected: {cause}");
    }
}