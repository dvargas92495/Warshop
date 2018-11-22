using System.Collections.Generic;
using System.Linq;
using UnityEngine.Events;
using UnityEngine.UI;

public class StatsController : Controller
{
    public Text MyScore;
    public Text OpponentScore;
    public Text TimeText;
    public HorizontalLayoutGroup MyTeam;
    public HorizontalLayoutGroup OpponentTeam;

    public void Initialize(GameEvent.End evt, bool isPrimary)
    {
        MyScore.text = (isPrimary ? evt.primaryBattery : evt.secondaryBattery).ToString();
        OpponentScore.text = (isPrimary ? evt.secondaryBattery : evt.primaryBattery).ToString();
        TimeText.text = "Number of turns: " + evt.turnCount + "\t Time of game: " + evt.timeTaken;

        UnityAction<Dictionary<short, Game.RobotStat>, HorizontalLayoutGroup> fillStats =
            (Dictionary<short, Game.RobotStat> teamStats, HorizontalLayoutGroup g) => {
                g.GetComponentsInChildren<Text>().ToList().ForEach((Text t) => t.gameObject.SetActive(false));
                List<short> keys = teamStats.Keys.ToList();
                for (int i = 0; i < teamStats.Count; i++)
                {
                    Text stat = g.transform.GetChild(i).GetComponent<Text>();
                    Game.RobotStat robotStat = teamStats[keys[i]];
                    stat.gameObject.SetActive(true);
                    stat.text = robotStat.name + 
                                "\nSpawns: " + robotStat.spawns +
                                "\nMoves: " + robotStat.moves +
                                "\nAttacks: " + robotStat.attacks + 
                                "\nSpecials: " + robotStat.specials +
                                "\nDeaths: " + robotStat.numberOfDeaths +
                                "\nKills: " + robotStat.numberOfKills
                    ;
                }
            };

        fillStats(evt.primaryTeamStats, isPrimary ? MyTeam : OpponentTeam);
        fillStats(evt.secondaryTeamStats, isPrimary ? OpponentTeam : MyTeam);
    }
}
