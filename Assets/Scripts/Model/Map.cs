using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Map
{
    internal int Width { get; set; }
    internal int Height { get; set; }
    internal Space[,] spaces;

    private Map(int width, int height)
    {
        Width = width;
        Height = height;
        spaces = new Space[width, height];
    }

    internal Map(string boardfile)
    {
        TextAsset content = Resources.Load<TextAsset>(boardfile);
        string[] lines = content.text.Split('\n');
        int[] boardDimensions = lines[0].Trim().Split(null).Select(int.Parse).ToArray();
        Width = boardDimensions[0];
        Height = boardDimensions[1];
        spaces = new Space[Width, Height];
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Trim().Split(' ');
            int y = i - 1;
            for (int x = 0; x < cells.Length; x++)
            {
                spaces[x, y] = new Space(cells[x]);
            }
        }


    }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(Width);
        writer.Write(Height);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                spaces[x, y].Serialize(writer);
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
                map.spaces[x, y] = Space.Deserialize(reader);
            }
        }
        return map;
    }

    public Space.SpaceType getSpaceType(int x, int y)
    {
        return spaces[x, y].spaceType;
    }

    // This method only updates what space is holding the object
    // It does not check that the object can move or cause any actions that would result from movement
    internal void UpdateObjectLocation()//int objectID, Point NewSpace)
    {
        //Point oldPoint = objectToSpace[objectID];
        //spaces[oldPoint.X, oldPoint.Y].RemoveObject(objectID);
        //objectToSpace[objectID] = NewSpace;
        //spaces[NewSpace.X, NewSpace.Y].AddObject(objectID);
    }

    internal void RemoveObject(int objectID)
    {
        //objectToSpace.Remove(objectID);
    }

    public class Space
    {
        internal SpaceType spaceType;
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

