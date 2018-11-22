using UnityEngine;
using UnityEngine.UI;

public class StatusModalController : Controller
{
    public Text statusText;

    public void ShowLoading()
    {
        gameObject.SetActive(true);
        statusText.color = Color.white;
        statusText.text = "Loading...";
    }

    public void DisplayError(string message)
    {
        gameObject.SetActive(true);
        statusText.color = Color.red;
        statusText.text = message;
    }
}
