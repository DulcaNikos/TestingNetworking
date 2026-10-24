using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyChatUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _ChatLog;
    [SerializeField] private TMP_InputField _InputField;
    [SerializeField] private ScrollRect _ScrollRect;

    private void OnEnable() => LobbyChatManager.OnMessageReceived += AppendMessage;
    private void OnDisable() => LobbyChatManager.OnMessageReceived -= AppendMessage;

    public void Send()
    {
        string text = _InputField.text.Trim();
        if (string.IsNullOrEmpty(text)) return;

        LobbyChatManager.Instance.SendMessage(text);
        _InputField.text = "";
        _InputField.ActivateInputField();
    }

    private void AppendMessage(string sender, string message)
    {
        _ChatLog.text += $"\n<b>{sender}:</b> {message}";
        Canvas.ForceUpdateCanvases();
        _ScrollRect.verticalNormalizedPosition = 0f;
    }
}