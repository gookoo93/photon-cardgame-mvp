using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// ──────────────────────────────────────────────
//  PasswordInputMask
//  비밀번호 입력 시 마지막 글자만 잠깐 보이고 나머지는 * 처리
//  InputField에 붙이는 스크립트
// ──────────────────────────────────────────────
[RequireComponent(typeof(InputField))]
public class PasswordInputMask : MonoBehaviour
{
    [Header("마지막 글자 표시 시간 (초)")]
    [SerializeField] private float showLastCharDuration = 0.8f;

    private InputField inputField;
    private string realPassword = "";   // 실제 비밀번호 원문
    private Coroutine maskCoroutine = null;

    // 실제 비밀번호 값을 외부에서 가져올 때 사용
    public string RealPassword => realPassword;

    private void Awake()
    {
        inputField = GetComponent<InputField>();
        inputField.onValueChanged.AddListener(OnValueChanged);

        // ContentType을 Standard로 설정 (직접 * 처리할 거라서)
        inputField.contentType = InputField.ContentType.Standard;
        inputField.ForceLabelUpdate();
    }

    private void OnValueChanged(string displayText)
    {
        // 입력 길이 비교로 추가/삭제 판단
        int displayLen = displayText.Replace("*", "").Length + CountNonAsterisk(displayText);

        if (displayText.Length > realPassword.Length)
        {
            // 글자 추가됨
            // 마지막에 입력된 실제 글자 추출 (* 아닌 마지막 글자)
            string added = "";
            for (int i = displayText.Length - 1; i >= 0; i--)
            {
                if (displayText[i] != '*')
                {
                    added = displayText[i].ToString();
                    break;
                }
            }
            realPassword += added;
        }
        else if (displayText.Length < realPassword.Length)
        {
            // 글자 삭제됨
            int diff = realPassword.Length - displayText.Length;
            if (realPassword.Length >= diff)
                realPassword = realPassword.Substring(0, realPassword.Length - diff);
        }

        // 마스킹 갱신
        if (maskCoroutine != null) StopCoroutine(maskCoroutine);
        maskCoroutine = StartCoroutine(CoMaskWithDelay());
    }

    private IEnumerator CoMaskWithDelay()
    {
        // 마지막 글자 잠깐 보여주기
        ApplyMask(showLast: true);

        yield return new WaitForSeconds(showLastCharDuration);

        // 전부 * 처리
        ApplyMask(showLast: false);
        maskCoroutine = null;
    }

    private void ApplyMask(bool showLast)
    {
        if (realPassword.Length == 0)
        {
            // onValueChanged 루프 방지
            inputField.onValueChanged.RemoveListener(OnValueChanged);
            inputField.text = "";
            inputField.onValueChanged.AddListener(OnValueChanged);
            return;
        }

        string masked;
        if (showLast)
        {
            // 마지막 글자만 원문, 나머지 *
            masked = new string('*', realPassword.Length - 1) + realPassword[realPassword.Length - 1];
        }
        else
        {
            masked = new string('*', realPassword.Length);
        }

        // onValueChanged 루프 방지
        inputField.onValueChanged.RemoveListener(OnValueChanged);
        inputField.text = masked;
        inputField.caretPosition = masked.Length;
        inputField.onValueChanged.AddListener(OnValueChanged);
    }

    private int CountNonAsterisk(string text)
    {
        int count = 0;
        foreach (char c in text)
            if (c != '*') count++;
        return count;
    }

    // 외부에서 비밀번호 초기화 시 호출
    public void Clear()
    {
        realPassword = "";
        inputField.onValueChanged.RemoveListener(OnValueChanged);
        inputField.text = "";
        inputField.onValueChanged.AddListener(OnValueChanged);
    }
}