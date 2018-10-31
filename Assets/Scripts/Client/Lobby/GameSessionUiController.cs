using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class GameSessionUiController : MonoBehaviour
{
    public Button playButton;
    public InputField passwordField;
    public Text usernameField;
    public Text publicPrivateText;

    public void SetPlayCallback(UnityAction callback)
    {
        playButton.onClick.AddListener(callback);
    }

    public void SetUsername(string username)
    {
        usernameField.text = username;
    }

    public void SetPrivacy(bool isPrivate)
    {
        publicPrivateText.text = isPrivate ? "Private" : "Public";
        passwordField.gameObject.SetActive(isPrivate);
    }

    public string GetPassword()
    {
        return passwordField.text;
    }

    public void DeactivateButton()
    {
        playButton.interactable = false;
    }
}
