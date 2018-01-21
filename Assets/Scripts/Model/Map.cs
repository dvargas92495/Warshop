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

    public short GetIdOnSpace(Space s)
    {
        return objectLocations.Keys.ToList().Find((short k) => objectLocations[k].Equals(s));
    }

    public Space.SpaceType getSpaceType(int x, int y)
    {
        return spaces[y * Width + x].spaceType;
    }

    public bool IsSpaceOccupied(Space s)
    {
        return objectLocations.ContainsValue(s);
    }

    internal void UpdateObjectLocation(int x, int y, short objectId) {
        objectLocations[objectId] = spaces[y*Width + x];
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

