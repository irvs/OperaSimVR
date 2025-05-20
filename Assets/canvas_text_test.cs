using UnityEngine;
using TMPro;

public class ChangeTMPTextWithMarkup : MonoBehaviour
{
    // TextMeshProUGUI コンポーネントへの参照
    public TextMeshProUGUI myTMPText;
    public string WriteWord;

    void Start()
    {
        // <mark>タグを使って色付きの文字列を設定
        if (myTMPText != null)
        {
            string coloredText = "<mark=#ff000055>Sensorpod 1</mark>";
            myTMPText.text = coloredText;
        }
    }

    void Update()
    {
        UpdateTextWithMarkup(WriteWord, "#ff000055");
    }

    // 引数で渡された文字列に色をつけて変更する場合
    public void UpdateTextWithMarkup(string baseText, string colorCode)
    {
        if (myTMPText != null)
        {
            string coloredText = $"<mark={colorCode}>{baseText}</mark>";
            myTMPText.text = coloredText;
        }
    }
}
