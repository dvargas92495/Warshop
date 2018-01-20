using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Map
{
    public TextAsset[] boardfiles;
    internal int Width { get; set; }
    internal int Height { get; set; }
    internal Space[] spaces;
    private Dictionary<short, Space> objectLocations;

    private Map(int width, int height)
    {
        Width = width;
        Height = height;
        spaces = new Space[width*height];
    }

    internal Map(TextAsset content)
    {
        string[] lines = content.text.Split('\n');
        int[] boardDimensions = lines[0].Trim().Split(null).Select(int.Parse).ToArray();
        Width = boardDimensions[0];
        Height = boardDimensions[1];
        spaces = new Space[Width*Height];
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Trim().Split(' ');
            int y = i - 1;
            for (int x = 0; x < cells.Length; x++)
            {
                spaces[y * Width + x] = new Space(cells[x]);
                spaces[y * Width + x].x = x;
                spaces[y * Width + x].y = y;
            }
        }
        objectLocations = new Dictionary<short, Space>();
    }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                spaces[y * Width + x].Serialize(writer);
            }
        }
    }
    public static Map Deserialize(NetworkReader reader)
    {
        int width = reader.ReadInt32();
        int height = reader.ReadInt32();
        Map map = new Map(width, height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map.spaces[y * width + x] = Space.Deserialize(reader);
            }
        }
        return map;
    }

    public short getIdOnSpace(Space s)
    {
        return objectLocations.Keys.ToList().Find((short k) => objectLocations[k].Equals(s));
    }

    public Space.SpaceType getSpaceType(int x, int y)
    {
        return spaces[y * Width + x].spaceType;
    }

    internal void UpdateObjectLocation(int x, int y, short objectId) {
        objectLocations[objectId] = spaces[y*Width + x];
    }

    internal HashSet<GameEvent> processMoveCommands(HashSet<Command.Move> moves, Robot[] allRobots, string primaryOwner)
    {
        HashSet<GameEvent> events = new HashSet<GameEvent>();
        Dictionary<short, Vector2> idsToDiffs = new Dictionary<short, Vector2>();
        moves.ToList().ForEach((Command.Move c) =>
        {
            Vector2 diff = Vector2.zero;
            switch (c.direction)
            {
                case Command.Direction.UP:
                    diff = Vector2.down;
                    break;
                case Command.Direction.DOWN:
                    diff = Vector2.up;
                    break;
                case Command.Direction.LEFT:
                    diff = Vector2.left;
                    break;
                case Command.Direction.RIGHT:
                    diff = Vector2.right;
                    break;
            }
            if (c.owner.Equals(primaryOwner))
            {
                diff = -diff;
            }
            idsToDiffs[c.robotId] = diff;
        });
        Array.ForEach(spaces, (Space s) => {
            List<short> idsWantSpace = idsToDiffs.Keys.ToList().FindAll((short key) => {
                Robot primaryRobot = Robot.Get(allRobots, key);
                Vector2 newspace = primaryRobot.position + idsToDiffs[key];
                return newspace.x == s.x && newspace.y == s.y;
            });
            if (idsWantSpace.Count > 1)
            {
                List<short> facing = idsWantSpace.FindAll((short key) => {
                    Robot primaryRobot = Robot.Get(allRobots, key);
                    return idsToDiffs[key].Equals(Robot.OrientationToVector(primaryRobot.orientation));
                });
                string blocker = "";
                if (facing.Count == 1)
                {
                    Robot winner = Robot.Get(allRobots, facing[0]);
                    idsWantSpace.Remove(winner.id);
                    blocker = winner.name;
                } else
                {
                    blocker = "Each Other";
                }
                if (objectLocations.ContainsValue(s))
                {
                    blocker = Robot.Get(allRobots, getIdOnSpace(s)).name;
                }
                idsWantSpace.ForEach((short key) => events.Add(new GameEvent.Block(Robot.Get(allRobots, key), blocker)));
                moves.RemoveWhere((Command.Move c) => idsWantSpace.Contains(c.robotId));
            }
        });
        HashSet<Robot> robotLocationsToUpdate = new HashSet<Robot>();
        moves.ToList().ForEach((Command.Move c) =>
        {
            Robot primaryRobot = Robot.Get(allRobots, c.robotId);
            Vector2 diff = idsToDiffs[c.robotId];
            
            Vector2 newspace = primaryRobot.position + diff;
            if (newspace.x < 0 || newspace.x >= Width || newspace.y < 0 || newspace.y >= Height)
            {
                events.Add(new GameEvent.Block(primaryRobot, "Wall"));
                moves.Remove(c);
                return;
            }
            Space space = spaces[(int)newspace.y * Width + (int)newspace.x];
            if (space.spaceType == Space.SpaceType.BASE)
            {
                events.Add(new GameEvent.Block(primaryRobot, "Base"));
                moves.Remove(c);
                return;
            }
            if (space.spaceType == Space.SpaceType.QUEUE)
            {
                events.Add(new GameEvent.Block(primaryRobot, "Queue"));
                moves.Remove(c);
                return;
            }
            if (objectLocations.ContainsValue(space))
            {
                Robot currentBot = Robot.Get(allRobots, getIdOnSpace(space));
                if (!Robot.OrientationToVector(primaryRobot.orientation).Equals(diff) || // Not Facing Direction
                Robot.OrientationToVector(currentBot.orientation).Equals(primaryRobot.position - newspace)) //Or currentBot facing it
                {
                    events.Add(new GameEvent.Block(primaryRobot, currentBot.name));
                    moves.Remove(c);
                }
                else
                {
                    events.Add(new GameEvent.Push(currentBot, primaryRobot.id));
                    events.Add(new GameEvent.Move(currentBot, diff));
                    events.Add(new GameEvent.Move(primaryRobot, diff));
                    robotLocationsToUpdate.Add(currentBot);
                    robotLocationsToUpdate.Add(primaryRobot);
                }
            }
            else
            {
                events.Add(new GameEvent.Move(primaryRobot, diff));
                robotLocationsToUpdate.Add(primaryRobot);
            }
        });
        
        robotLocationsToUpdate.ToList().ForEach(((Robot r) => {
            UpdateObjectLocation((int) r.position.x, (int) r.position.y, r.id);
        }));

        return events;
    }

    internal void RemoveObject(int objectID)
    {
        //objectToSpace.Remove(objectID);
    }

    public class Space
    {
        internal SpaceType spaceType;
        internal int x;
        internal int y;
        internal Space(string s)
        {
            switch (s)
            {
                case "V":
                    spaceType = SpaceType.VOID;
                    break;
                case "W":
                    spaceType = SpaceType.BLANK;
                    break;
                case "S":
                case "s":
                    spaceType = SpaceType.SPAWN;
                    break;
                case "A":
                case "B":
                    spaceType = SpaceType.BASE;
                    break;
                case "Q":
                case "q":
                    spaceType = SpaceType.QUEUE;
                    break;
            }
        }
        private Space(SpaceType t)
        {
            spaceType = t;
        }
        public enum SpaceType
        {
            VOID,
            BASE,
            BLANK,
            SPAWN,
            QUEUE
        }
        public void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)spaceType);
        }
        public static Space Deserialize(NetworkReader reader)
        {
            return new Space((SpaceType)reader.ReadByte());
        }
    }
}

