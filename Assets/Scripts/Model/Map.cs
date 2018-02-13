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
                spaces[y * Width + x] = Space.Create(cells[x][0]);
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

    public Space VecToSpace(Vector2Int v)
    {
        return VecToSpace(v.x,v.y);
    }

    public Space VecToSpace(int x, int y)
    {
        if (y < 0 || y >= Height || x < 0 || x >= Width) return null;
        return spaces[y * Width + x];
    }

    public bool IsVoid(Vector2Int v)
    {
        Space s = VecToSpace(v);
        if (s == null) return true;
        return s is Void;
    }

    public bool IsBattery(Vector2Int v)
    {
        Space s = VecToSpace(v);
        if (s == null) return false;
        return s is Battery;
    }

    public bool IsQueue(Vector2Int v)
    {
        Space s = VecToSpace(v);
        if (s == null) return false;
        return s is Queue;
    }

    public bool IsPrimary(Vector2Int v)
    {
        Space s = VecToSpace(v);
        if (s == null) return true;
        else if (s is Battery) return ((Battery)s).isPrimary;
        else if (s is Queue) return ((Queue)s).isPrimary;
        else return true;
    }

    public short GetIdOnSpace(Vector2Int v)
    {
        return GetIdOnSpace(VecToSpace(v));
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

    public Vector2Int GetQueuePosition(byte i, bool isPrimary)
    {
        Space[] queueSpaces = Array.FindAll(spaces, (Space s) => s is Queue);
        Space queueSpace = Array.Find(queueSpaces, (Space s) => ((Queue)s).index == i % queueSpaces.Length && ((Queue)s).isPrimary == isPrimary);
        return new Vector2Int(queueSpace.x, queueSpace.y);
    }

    public bool IsSpaceOccupied(Vector2Int v)
    {
        return IsSpaceOccupied(VecToSpace(v));
    }

    public bool IsSpaceOccupied(Space s)
    {
        return objectLocations.ContainsValue(s);
    }

    internal void UpdateObjectLocation(int x, int y, short objectId) {
        objectLocations[objectId] = VecToSpace(x,y);
    }
    internal void RemoveObjectLocation(short objectId)
    {
        objectLocations.Remove(objectId);
    }

    public abstract class Space
    {
        internal int x;
        internal int y;
        internal static Space Create(char s)
        {
            switch (s)
            {
                case 'W':
                    return new Blank();
                case 'P':
                case 'p':
                    return new Battery(char.IsUpper(s));
                case 'A':
                case 'a':
                case 'B':
                case 'b':
                case 'C':
                case 'c':
                case 'D':
                case 'd':
                    bool p = char.IsUpper(s);
                    byte i = (byte)(p ? s - 'A': s - 'a');
                    return new Queue(i, p);
                case 'V':
                default:
                    return new Void();
            }
        }
        public abstract void Serialize(NetworkWriter writer);
        public static Space Deserialize(NetworkReader reader)
        {
            byte s = reader.ReadByte();
            if (s == 0)
            {
                return new Void();
            } else if (s == 1)
            {
                return new Blank();
            } else if (s <= 3)
            {
                return new Battery(s == 2);
            } else if (s <= 11)
            {
                bool p = s <= 7;
                byte i = (byte)(p ? s - 4 : s - 8);
                return new Queue(i, p);
            }else
            {
                return new Void();
            }
        }
    }
    private class Void : Space
    {
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)0);
        }
    }
    private class Blank : Space
    {
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)1);
        }
    }
    private class Battery : Space
    {
        internal bool isPrimary { private set; get; }
        internal Battery(bool p)
        {
            isPrimary = p;
        }
        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)(isPrimary ? 2 : 3));
        }
    }
    private class Queue : Space
    {
        internal bool isPrimary { private set; get; }
        internal byte index { private set; get; }
        internal Queue(byte i, bool p)
        {
            index = i;
            isPrimary = p;
        }
        public override void Serialize(NetworkWriter writer)
        {
            byte s = (byte)((isPrimary ? 4:8) + index);
            writer.Write(s);
        }

    }
}

