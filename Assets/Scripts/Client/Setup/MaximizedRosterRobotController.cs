using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MaximizedRosterRobotController : Controller
{
    public Image selectedRobot;
    public HorizontalLayoutGroup ratingGroup;
    public TMP_Text nameField;
    public TMP_Text attackField;
    public TMP_Text healthField;
    public TMP_Text descriptionField;
    public Transform container;

    private byte rating;

    public void Select(Sprite robotSprite)
    {
        container.gameObject.SetActive(true);

        selectedRobot.sprite = robotSprite;
        nameField.SetText(robotSprite.name);
        Robot robot = Robot.create(robotSprite.name);
        attackField.SetText(robot.attack.ToString());
        healthField.SetText(robot.health.ToString());
        descriptionField.SetText(robot.description);
        rating = (byte)robot.rating;
        Util.ToIntList(ratingGroup.transform.childCount).ForEach(SetRating);
    }

    public Sprite GetRobotSprite()
    {
        return selectedRobot.sprite;
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        rating = 0;
    }

    public byte GetRating()
    {
        return rating;
    }

    private void SetRating(int i)
    {
        ratingGroup.transform.GetChild(i).gameObject.SetActive(i < rating);
    }
}
