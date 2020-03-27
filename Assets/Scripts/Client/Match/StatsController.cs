using UnityEngine.UI;

public class StatsController : Controller
{
    public Text myScore;
    public Text opponentScore;
    public Text timeText;
    public Text[] myTeam;
    public Text[] opponentTeam;

    public void Initialize(GameEvent.End evt)
    {
        myScore.text = evt.primaryBatteryCost.ToString();
        opponentScore.text = evt.secondaryBatteryCost.ToString();
        timeText.text = "Number of turns: " + evt.turnCount;

        FillStats(evt.primaryTeamStats, myTeam);
        FillStats(evt.secondaryTeamStats, opponentTeam);
    }

    private void FillStats(Dictionary<short, Game.RobotStat> teamStats, Text[] g)
    {
        Util.ToList(g).ForEach(t => t.gameObject.SetActive(false));
        teamStats.ForEach((i, robotStat) => FillRobotStat(g[i], robotStat));
    }

    private void FillRobotStat(Text stat, Game.RobotStat robotStat)
    {
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
}
