using System.Collections.Generic;
using UnityEngine;

namespace Z8.Generic
{

    public class PlayerTurnObject
    {
        public string PlayerName { get; set; }
        public List<RobotObject> robotObjects = new List<RobotObject>();
        public List<int> handIDs = new List<int>();

        public PlayerTurnObject(string currentPlayerName)
        {
            PlayerName = currentPlayerName;
        }

        public void AddRobot(RobotObject robot)
        {
            robotObjects.Add(robot);
        }

        public void AddCard(int i)
        {
            handIDs.Add(i);
        }
    }

    public class RobotObject
    {
        public string Owner { get; set; }
        public string Identifier { get; set; }
        public int Id { get; set; }
        public string Name { get; set; }
        public int Health { get; set; }
        public int Attack { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public string Equipment { get; set; }
        public Vector2 Orientation { get; set; }
        public Vector2 Position { get; set; }
        public bool IsOpponent { get; set; }
        // Orientation, position, passive 

        public RobotObject()
        {
        }
    }

    public class ClientInitializationObject
    {

    }

    public class BoardLayout
    {

    }

}
