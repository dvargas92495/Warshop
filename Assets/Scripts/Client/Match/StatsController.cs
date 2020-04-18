using UnityEngine.UI;

public class StatsController : Controller
{
    public Text myScore;
    public Text opponentScore;
    public Text timeText;

    public void Initialize(EndEvent evt)
    {
        gameObject.SetActive(true);
        myScore.text = evt.primaryBatteryCost.ToString();
        opponentScore.text = evt.secondaryBatteryCost.ToString();
        timeText.text = "Number of turns: " + evt.turnCount;
    }
}
