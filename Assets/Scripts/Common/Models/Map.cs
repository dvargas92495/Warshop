using UnityEngine;
using UnityEngine.Networking;

public class Map
{
    public static Vector2Int NULL_VEC = Vector2Int.one * -1;

    internal int width { get; set; }
    internal int height { get; set; }
    internal Space[] spaces;
    private Tuple<Set<short>, Set<short>> dock = new Tuple<Set<short>, Set<short>>(new Set<short>(GameConstants.MAX_ROBOTS_ON_SQUAD), new Set<short>(GameConstants.MAX_ROBOTS_ON_SQUAD));
    private Dictionary<short, Space> objectLocations;

    private Map(int w, int h)
    {
        width = w;
        height = h;
        spaces = new Space[width*height];
    }

    public Map(string content)
    {
        string[] lines = content.Split('\n');
        int[] boardDimensions = Util.ToList(lines[0].Trim().Split(null)).Map(int.Parse).ToArray();
        width = boardDimensions[0];
        height = boardDimensions[1];
        spaces = new Space[width*height];
        for (int y = 0; y < height; y++)
        {
            string[] cells = lines[y+1].Trim().Split(' ');
            for (int x = 0; x < width; x++)
            {
                spaces[y * width + x] = Space.Create(cells[x][0]);
                spaces[y * width + x].x = x;
                spaces[y * width + x].y = y;
            }
        }
        objectLocations = new Dictionary<short, Space>(GameConstants.MAX_ROBOTS_ON_SQUAD*2);
    }
    public void Serialize(NetworkWriter writer)
    {
        writer.Write(width);
        writer.Write(height);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                spaces[y * width + x].Serialize(writer);
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
        if (y < 0 || y >= height || x < 0 || x >= width) return null;
        return spaces[y * width + x];
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

    public int GetQueueIndex(Vector2Int v)
    {
        if (IsQueue(v))
        {
            return ((Queue)VecToSpace(v)).GetIndex();
        } else
        {
            return -1;
        }
    }

    public bool IsPrimary(Vector2Int v)
    {
        Space s = VecToSpace(v);
        if (s == null) return true;
        else if (s is Battery) return ((Battery)s).GetIsPrimary();
        else if (s is Queue) return ((Queue)s).GetIsPrimary();
        else return true;
    }

    public short GetIdOnSpace(Vector2Int v)
    {
        return GetIdOnSpace(VecToSpace(v));
    }

    public short GetIdOnSpace(Space s)
    {
        if (objectLocations.ContainsValue(s))
        {
            return objectLocations.GetKey(s);
        }
        else
        {
            return -1;
        }
    }

    public Vector2Int GetQueuePosition(byte i, bool isPrimary)
    {
        Space[] queueSpaces = Util.ToList(spaces).Filter(s => s is Queue).ToArray();
        Space queueSpace = Util.ToList(queueSpaces).Find(s => ((Queue)s).GetIndex() == i && ((Queue)s).GetIsPrimary() == isPrimary);
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

    public void UpdateObjectLocation(int x, int y, short objectId)
    {
        objectLocations.Put(objectId, VecToSpace(x,y));
    }

    public void RemoveObjectLocation(short objectId)
    {
        objectLocations.Remove(objectId);
    }

    internal void AddToDock(short robotId, bool isPrimary)
    {
        if (isPrimary) dock.GetLeft().Add(robotId);
        else dock.GetRight().Add(robotId);
    }

    public abstract class Space
    {
        internal int x;
        internal int y;

        protected const byte VOID_ID = 0;
        protected const byte BLANK_ID = 1;
        protected const byte BATTERY_ID = 2;
        protected const byte QUEUE_ID = 4;

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
                default:
                    return new Void();
            }
        }

        public abstract void accept(TileController t);
        public abstract void Serialize(NetworkWriter writer);

        public static Space Deserialize(NetworkReader reader)
        {
            byte s = reader.ReadByte();
            if (s == VOID_ID)
            {
                return new Void();
            } else if (s == BLANK_ID)
            {
                return new Blank();
            } else if (s < BATTERY_ID + 2)
            {
                return new Battery(s == BATTERY_ID);
            } else if (s < QUEUE_ID + GameConstants.MAX_ROBOTS_ON_SQUAD*2)
            {
                bool p = s < QUEUE_ID + GameConstants.MAX_ROBOTS_ON_SQUAD;
                byte i = (byte)(p ? s - GameConstants.MAX_ROBOTS_ON_SQUAD : s - (GameConstants.MAX_ROBOTS_ON_SQUAD*2));
                return new Queue(i, p);
            }else
            {
                return new Void();
            }
        }
    }

    public abstract class PlayerSpace : Space
    {
        protected bool isPrimary;

        public bool GetIsPrimary()
        {
            return isPrimary;
        }
    }

    public class Void : Space
    {
        public override void accept(TileController t){}

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(VOID_ID);
        }
    }

    public class Blank : Space
    {
        public override void accept(TileController t) { }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write(BLANK_ID);
        }
    }

    public class Battery : PlayerSpace
    {
        internal Battery(bool p)
        {
            isPrimary = p;
        }

        public override void accept(TileController t)
        {
            t.LoadBatteryTile(this);
        }

        public override void Serialize(NetworkWriter writer)
        {
            writer.Write((byte)(isPrimary ? 2 : 3));
        }
    }

    public class Queue : PlayerSpace
    {
        private byte index;
        internal Queue(byte i, bool p)
        {
            index = i;
            isPrimary = p;
        }

        public override void accept(TileController t)
        {
            t.LoadQueueTile(this);
        }

        public override void Serialize(NetworkWriter writer)
        {
            byte s = (byte)((isPrimary ? 4:8) + index);
            writer.Write(s);
        }

        public byte GetIndex()
        {
            return index;
        }

    }
}

