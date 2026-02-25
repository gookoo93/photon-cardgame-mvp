using UnityEngine;
using UnityEngine.UI;

// ----------------------------------------------
//  CodeJoinPanel
//  코드 입력 참가 팝업에 붙이는 스크립트
//
//  [패널 구성 예시]
//  CodeJoinPanel
//  ㄴ- CodeInputField  (InputField) - 방 코드 입력
//  ㄴ- SearchButton    (Button)     - 검색/참가
//  ㄴ- CloseButton     (Button)     - X
// ----------------------------------------------
public class CodeJoinPanel : MonoBehaviour
{
    [SerializeField] private InputField codeInputField;
    [SerializeField] private Button searchButton;
    [SerializeField] private Button closeButton;

    private void Awake()
    {
        searchButton?.onClick.AddListener(OnClickSearch);
        closeButton?.onClick.AddListener(() => gameObject.SetActive(false));
    }

    private void OnEnable()
    {
        if (codeInputField) codeInputField.text = "";
    }

    private void OnClickSearch()
    {
        string code = codeInputField?.text.Trim().ToUpper() ?? "";
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("[CodeJoin] 방 코드를 입력해주세요.");
            return;
        }

        LobbyManager.Instance?.JoinRoomByCode(code);
    }
}