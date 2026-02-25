using UnityEngine;
using UnityEngine.UI;
using System;

public class HandCardButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image image;
    [SerializeField] private Image highlight;     // 선택 테두리(자식 Highlight Image)
    [SerializeField] private Image usedOverlay;   // 회색 오버레이(있으면)
    [SerializeField] private CardType cardType;

    public bool IsSelected { get; private set; }
    public CardType CardType => cardType;         //외부에서 읽을때 용

    private int index;
    private Action<int, CardType> onClick;

    public void Init(int idx, Action<int, CardType> onClickCallback)
    {
        index = idx;
        onClick = onClickCallback;

        if (!button) button = GetComponent<Button>();

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick?.Invoke(index, cardType));

        // 시작 시 하이라이트/사용표시 정리 (본체는 건드리지 않는다!)
        SetSelected(false);

        if (usedOverlay)
            usedOverlay.gameObject.SetActive(false);

        // 회색 처리도 초기화
        if (!image) image = GetComponent<Image>();
        if (image) image.color = Color.white;
    }

    public void SetSelected(bool value)
    {
        IsSelected = value;

        if (highlight) highlight.gameObject.SetActive(value);
    }

    public void SetInteractable(bool value)
    {
        if (button) button.interactable = value;
    }

    public void SetUsedVisual(bool used)
    {
        if (usedOverlay) usedOverlay.gameObject.SetActive(used);

        // 회색 처리 (image 슬롯 비어있으면 GetComponent로라도 잡기)
        if (!image) image = GetComponent<Image>();
        if (image) image.color = used ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;

        if (used) SetSelected(false);
    }

    //가변카드
    public void SetCard(CardType type, Sprite frontSprite)
    {
        cardType = type;

        if (!image) image = GetComponent<Image>();
        if (image) image.sprite = frontSprite;
    }

    public void SetCardTypeOnly(CardType type)
    {
        cardType = type;
    }
}