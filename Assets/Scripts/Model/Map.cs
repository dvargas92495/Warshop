using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Map
{
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

    internal Map(string content)
    {
        string[] lines = content.Split('\n');
        int[] boardDimensions = lines[0].Trim().Split(null).Select(int.Parse).ToArray();
        Width = boardDimensions[0];
        Height = boardDimensions[1];
        spaces = new Space[Width*Height];
        for (int y = 0; y < Height; y++)
        {
            string[] cells = lines[y+1].Trim().Split(' ');
            for (int x = 0; x < Width; x++)
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

    public short GetIdOnSpace(Vector2Int v)
    {
        if (v.y < 0 || v.y >= Height || v.x < 0 || v.x >= Width) return -1;
        return GetIdOnSpace(spaces[v.y * Width + v.x]);
    }

    public short GetIdOnSpace(Space s)
    {
        Func<short, bool> eq = (short k) => objectLocations[k].Equals(s);
        if (objectLocations.Keys.Any(eq))
        {
            return objectLocations.Keys.ToList().Find((short k) => objectLocations[k].Equals(s));
        } else
        {
            return -1;
        }
    }

    public Space.SpaceType getSpaceType(int x, int y)
    {
        return spaces[y * Width + x].spaceType;
    }

    public Vector2Int GetQueuePosition(byte i, bool isPrimary)
    {
        Space[] queueSpaces = Array.FindAll(spaces, (Space s) => s.spaceType == (isPrimary ? Space.SpaceType.PRIMARY_QUEUE : Space.SpaceType.SECONDARY_QUEUE));
        Space queueSpace = queueSpaces[i % queueSpaces.Length];
        return new Vector2Int(queueSpace.x, queueSpace.y);
    }

    public bool IsSpaceOccupied(Vector2Int v)
    {
        return IsSpaceOccupied(spaces[v.y * Width + v.x]);
    }

    public bool IsSpaceOccupied(Space s)
    {
        return objectLocations.ContainsValue(s);
    }

    internal void UpdateObjectLocation(int x, int y, short objectId) {
        objectLocations[objectId] = spaces[y*Width + x];
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
                    spaceType = SpaceType.PRIMARY_BASE;
                    break;
                case "B":
                    spaceType = SpaceType.SECONDARY_BASE;
                    break;
                case "Q":
                    spaceType = SpaceType.PRIMARY_QUEUE;
                    break;
                case "q":
                    spaceType = SpaceType.SECONDARY_QUEUE;
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
            PRIMARY_BASE,
            SECONDARY_BASE,
            BLANK,
            SPAWN,
            PRIMARY_QUEUE,
            SECONDARY_QUEUE
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

