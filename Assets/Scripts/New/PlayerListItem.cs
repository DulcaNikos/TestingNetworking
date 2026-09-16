using UnityEngine;
using Steamworks;
using TMPro;
using UnityEngine.UI;

public class PlayerListItem : MonoBehaviour
{
    [SerializeField, Tooltip("")]
    private RawImage _PlayerIcon;
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _PlayerNameText;
    [SerializeField, Tooltip("")]
    private TextMeshProUGUI _PlayerReadyText;

    public bool _Ready;
    public string _PlayerName;
    public int _ConnectionID;
    public ulong _PlayerSteamID;
    private bool _AvatarReceived;

    protected Callback<AvatarImageLoaded_t> _ImageLoaded;

    private void Start()
    {
        _ImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnImageLoaded);
    }

    public void SetPlayerValues()
    {
        _PlayerNameText.text = _PlayerName;
        ChangeReadyStatus();
        if (!_AvatarReceived)
        {
            GetPlayerIcon();
        }
    }

    private void GetPlayerIcon()
    {
        int imageID = SteamFriends.GetLargeFriendAvatar((CSteamID)_PlayerSteamID);
        if (imageID == -1)
        {
            return;
        }
        _PlayerIcon.texture = GetSteamImageAsTexture(imageID);
    }

    private void OnImageLoaded(AvatarImageLoaded_t _callback)
    {
        if (_callback.m_steamID.m_SteamID == _PlayerSteamID)
        {
            _PlayerIcon.texture = GetSteamImageAsTexture(_callback.m_iImage);
        }
        else
        {
            return;
        }
    }

    private Texture2D GetSteamImageAsTexture(int _iImage)
    {
        Texture2D texture = null;
        bool isValid = SteamUtils.GetImageSize(_iImage, out uint width, out uint height);
        if (isValid)
        {
            byte[] image = new byte[width * height * 4];

            isValid = SteamUtils.GetImageRGBA(_iImage, image, (int)(width * height * 4));
            if (isValid)
            {
                texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
                texture.LoadRawTextureData(image);
                texture.Apply();
            }
        }

        _AvatarReceived = true;
        return texture;
    }

    public void ChangeReadyStatus()
    {
        if (_Ready)
        {
            _PlayerReadyText.text = "Ready";
            _PlayerReadyText.color = Color.green;
        }
        else
        {
            _PlayerReadyText.text = "Unready";
            _PlayerReadyText.color = Color.red;
        }
    }
}
