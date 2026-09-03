using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AutoTyping : NetworkBehaviour
{
    [Header("Typing Settings")]
    [SerializeField] public string fullText = "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.";
    [SerializeField] public float typingSpeed = 0.05f;

    [Header("UI References (local, not networked)")]
    [SerializeField] public TMP_Text displayText;
    [SerializeField] public Button toggleButton;
    [SerializeField] public Image buttonImage;

    [Header("Colors")]
    [SerializeField] public Color activeColor = Color.green;
    [SerializeField] public Color inactiveColor = Color.red;

    [SyncVar(hook = nameof(OnCurrentTextChanged))]
    private string currentText = "";

    [SyncVar(hook = nameof(OnActiveChanged))]
    private bool active = false;

    private Coroutine typingCoroutine;

    public override void OnStartClient()
    {
        base.OnStartClient();
        OnCurrentTextChanged(string.Empty, currentText);
        OnActiveChanged(false, active);
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
    }

    private void OnCurrentTextChanged(string oldText, string newText)
    {
        if (displayText != null)
        {
            displayText.text = newText;
        }
    }

    private void OnActiveChanged(bool oldValue, bool newValue)
    {
        if (buttonImage != null)
        {
            buttonImage.color = newValue ? activeColor : inactiveColor;
        }
    }

    public void OnToggleButtonPressed()
    {
        if (!isServer)
        {
            return;
        }

        StartTyping();
    }

    [Server]
    public void StartTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(TypeTextRoutine());
    }

    [Server]
    private IEnumerator TypeTextRoutine()
    {
        active = true;
        currentText = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (this == null)
            {
                yield break;
            }

            currentText += fullText[i];
            yield return new WaitForSeconds(typingSpeed);
        }

        active = false;
        typingCoroutine = null;
    }
}