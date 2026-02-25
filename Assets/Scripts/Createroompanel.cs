using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using System;

// ----------------------------------------------
//  CreateRoomPanel
//  방 생성 팝업
// ----------------------------------------------
public class CreateRoomPanel : MonoBehaviourPunCallbacks
{
    [SerializeField] private InputField titleInputField;
    [SerializeField] private Toggle privateToggle;
    [SerializeField] private InputField passwordInputField;
    [SerializeField] private GameObject passwordRow;
    [SerializeField] private Button createButton;
    [SerializeField] private Button closeButton;

    [Header("방 설정")]
    [SerializeField] private int maxPlayers = 2;

    private void Awake()
    {
        createButton?.onClick.AddListener(OnClickCreate);
        closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
        privateToggle?.onValueChanged.AddListener(OnTogglePrivate);

        if (passwordRow) passwordRow.SetActive(false);
    }

    private void OnEnable()
    {
        if (titleInputField) titleInputField.text = "";
        if (passwordInputField) passwordInputField.text = "";
        if (privateToggle) privateToggle.isOn = false;
        if (passwordRow) passwordRow.SetActive(false);
    }

    private void OnTogglePrivate(bool isPrivate)
    {
        if (passwordRow) passwordRow.SetActive(isPrivate);
        if (!isPrivate && passwordInputField)
            passwordInputField.text = "";
    }

    private void OnClickCreate()
    {
        // ? 연결 상태 체크
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("[CreateRoom] Photon 서버에 연결되지 않았습니다.");
            return;
        }

        if (!PhotonNetwork.InLobby)
        {
            Debug.LogWarning("[CreateRoom] 로비에 입장 중입니다. 잠시 후 다시 시도해주세요.");
            // 로비 재입장 시도
            PhotonNetwork.JoinLobby();
            return;
        }

        string title = titleInputField?.text.Trim() ?? "";
        if (string.IsNullOrEmpty(title))
        {
            Debug.LogWarning("[CreateRoom] 방 제목을 입력해주세요.");
            return;
        }

        bool isPrivate = privateToggle != null && privateToggle.isOn;
        string password = isPrivate ? (passwordInputField?.text ?? "") : "";

        // PasswordInputMask 사용 시 실제 비밀번호 가져오기
        if (isPrivate && passwordInputField != null)
        {
            var mask = passwordInputField.GetComponent<PasswordInputMask>();
            if (mask != null) password = mask.RealPassword;
        }

        string code = GenerateRoomCode();

        var customProps = new Hashtable
        {
            { LobbyManager.PROP_TITLE,    title    },
            { LobbyManager.PROP_CODE,     code     },
            { LobbyManager.PROP_PASSWORD, password }
        };

        string[] propsToList = { LobbyManager.PROP_TITLE, LobbyManager.PROP_CODE, LobbyManager.PROP_PASSWORD };

        var options = new RoomOptions
        {
            MaxPlayers = (byte)maxPlayers,
            IsVisible = true,
            IsOpen = true,
            CustomRoomProperties = customProps,
            CustomRoomPropertiesForLobby = propsToList
        };

        // 방 이름: 제목 + 코드 조합 (내부용)
        string roomName = $"{title}_{code}";

        PhotonNetwork.CreateRoom(roomName, options);
        gameObject.SetActive(false);
    }

    // 4자리 영문+숫자 랜덤 코드
    private static string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] result = new char[4];
        for (int i = 0; i < 4; i++)
            result[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
        return new string(result);
    }
}