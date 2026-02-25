using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using System;

// ------------------------------------------
// RoomListItem
// 방 목록 개별 아이템 프리팹에 붙이는 스크립트
//
// 프리팹 구성
//   TitleText       (Text) - 방 제목
//   PlayerCountText (Text) - 인원 수
//   SettingText     (Text) - 공개 / 비공개
//   CodeText        (Text) - 방 코드
// ------------------------------------------
public class RoomListItem : MonoBehaviour
{
    [SerializeField] private Text titleText;
    [SerializeField] private Text playerCountText;
    [SerializeField] private Text settingText;
    [SerializeField] private Text codeText;

    private RoomInfo roomInfo;
    private Action<RoomInfo> onClickCallback;

    public void Setup(RoomInfo info, Action<RoomInfo> onClick)
    {
        roomInfo = info;
        onClickCallback = onClick;

        string title = GetProp(info, LobbyManager.PROP_TITLE, info.Name);
        if (titleText) titleText.text = title;

        if (playerCountText)
            playerCountText.text = info.PlayerCount + " / " + info.MaxPlayers + " 명";

        bool isPrivate = !string.IsNullOrEmpty(GetProp(info, LobbyManager.PROP_PASSWORD, ""));
        if (settingText) settingText.text = isPrivate ? "비공개" : "공개";

        string code = GetProp(info, LobbyManager.PROP_CODE, "----");
        if (codeText) codeText.text = code;

        var btn = GetComponent<Button>();
        if (btn) btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        onClickCallback?.Invoke(roomInfo);
    }

    private static string GetProp(RoomInfo info, string key, string fallback)
    {
        if (info.CustomProperties == null) return fallback;
        if (info.CustomProperties.TryGetValue(key, out object val) && val != null)
            return val.ToString();
        return fallback;
    }
}