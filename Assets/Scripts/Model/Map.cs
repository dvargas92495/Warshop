using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class Map
{
    public TextAsset[] boardfiles;
    internal int Width { get; set; }
    internal int Height { get; set; }
    internal Space[,] spaces;
    private Dictionary<short, Space> objectLocations;

    private Map(int width, int height)
    {
        Width = width;
        Height = height;
        spaces = new Space[width, height];
    }

    internal Map(TextAsset content)
    {
        string[] lines = content.text.Split('\n');
        int[] boardDimensions = lines[0].Trim().Split(null).Select(int.Parse).ToArray();
        Width = boardDimensions[0];
        Height = boardDimensions[1];
        spaces = new Space[Width, Height];
        int id = 0;
        for (int i = 1; i < lines.Length; i++)
        {
            string[] cells = lines[i].Trim().Split(' ');
            int y = i - 1;
            for (int x = 0; x < cells.Length; x++)
            {
                spaces[x, y] = new Space(cells[x]);
                spaces[x, y].id = id;
                id++;
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

    internal void UpdateObjectLocation(int x, int y, short objectId) {
        objectLocations[objectId] = spaces[x, y];
    }

    internal void RemoveObject(int objectID)
    {
        //objectToSpace.Remove(objectID);
    }

    public class Space
    {
        internal SpaceType spaceType;
        internal int id;
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
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (base.Equals(obj))
            {
                return id == ((Space)obj).id;
            }
            return false;
        }
    }
}

