using UnityEngine.UI;

public class NewGameSessionUiController : GameSessionUiController
{
    public Dropdown publicPrivateDropdown;

    public void Start()
    {
        publicPrivateDropdown.onValueChanged.AddListener((int v) => DropdownCallback(v == 1));
    }

    public void DropdownCallback(bool isPrivate)
    {
        passwordField.gameObject.SetActive(isPrivate);
        playButton.interactable = !isPrivate || !passwordField.text.Equals("");
    }

    public bool GetPrivacy()
    {
        return publicPrivateDropdown.value == 1;
    }
}
