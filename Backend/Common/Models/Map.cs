using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace WarshopCommon {
    public class Map
    {
        public static Tuple<int, int> NULL_VEC = new Tuple<int, int>(-1, -1);

        public int width { get; set; }
        public int height { get; set; }
        public Space[] spaces{ get; set; }
        private Tuple<HashSet<short>, HashSet<short>> dock = new Tuple<HashSet<short>, HashSet<short>>(new HashSet<short>(GameConstants.MAX_ROBOTS_ON_SQUAD), new HashSet<short>(GameConstants.MAX_ROBOTS_ON_SQUAD));

        private Map(int w, int h)
        {
            width = w;
            height = h;
            spaces = new Space[width*height];
        }

        public Map(string content)
        {
            string[] lines = content.Split('\n');
            int[] boardDimensions = lines[0].Trim().Split(null).ToList().ConvertAll(int.Parse).ToArray();
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
        }
        public string Serialize()
        {
            return JsonSerializer.Serialize(this);
        }
        public static Map Deserialize(string msg)
        {
            return JsonSerializer.Deserialize<Map>(msg);
        }

        public Space VecToSpace(Tuple<int, int> v)
        {
            return VecToSpace(v.Item1, v.Item2);
        }

        public Space VecToSpace(int x, int y)
        {
            if (y < 0 || y >= height || x < 0 || x >= width) return null;
            return spaces[y * width + x];
        }

        public bool IsVoid(Tuple<int, int> v)
        {
            Space s = VecToSpace(v);
            if (s == null) return true;
            return s is Void;
        }

        public bool IsBattery(Tuple<int, int> v)
        {
            Space s = VecToSpace(v);
            if (s == null) return false;
            return s is Battery;
        }

        public bool IsQueue(Tuple<int, int> v)
        {
            Space s = VecToSpace(v);
            if (s == null) return false;
            return s is Queue;
        }

        public int GetQueueIndex(Tuple<int, int> v)
        {
            if (IsQueue(v))
            {
                return ((Queue)VecToSpace(v)).GetIndex();
            } else
            {
                return -1;
            }
        }

        public bool IsPrimary(Tuple<int, int> v)
        {
            Space s = VecToSpace(v);
            if (s == null) return true;
            else if (s is Battery) return ((Battery)s).GetIsPrimary();
            else if (s is Queue) return ((Queue)s).GetIsPrimary();
            else return true;
        }

        public Tuple<int, int> GetQueuePosition(byte i, bool isPrimary)
        {
            Space[] queueSpaces = spaces.ToList().FindAll(s => s is Queue).ToArray();
            Space queueSpace = queueSpaces.ToList().Find(s => ((Queue)s).GetIndex() == i && ((Queue)s).GetIsPrimary() == isPrimary);
            return new Tuple<int, int>(queueSpace.x, queueSpace.y);
        }

        internal void AddToDock(short robotId, bool isPrimary)
        {
            if (isPrimary) dock.Item1.Add(robotId);
            else dock.Item2.Add(robotId);
        }

        public abstract class Space
        {
            public int x {get; set;}
            public int y {get; set;}

            public byte type {get; set;}

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

            public string Serialize() {
                return JsonSerializer.Serialize(this);
            }

            public static Space Deserialize(string msg)
            {
                byte s = JsonSerializer.Deserialize<Space>(msg).type;
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
            public Void()
            {
                type = VOID_ID;
            }
        }

        public class Blank : Space
        {
            public Blank()
            {
                type = BLANK_ID;
            }
        }

        public class Battery : PlayerSpace
        {
            internal Battery(bool p)
            {
                isPrimary = p;
                type = (byte)(isPrimary ? 2 : 3);
            }
        }

        public class Queue : PlayerSpace
        {
            private byte index;
            internal Queue(byte i, bool p)
            {
                index = i;
                isPrimary = p;
                type = (byte)((isPrimary ? 4:8) + index);
            }

            public byte GetIndex()
            {
                return index;
            }

        }
    }
}
