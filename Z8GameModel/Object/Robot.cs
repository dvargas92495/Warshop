namespace Z8GameModel
{
    internal class Robot : Actor
    {
        internal readonly string name;
        internal readonly int spawnCost;
        internal readonly int priority;

        internal Robot(int maxHealth, int strength, int spawnCost, int priority, string name) 
                : base(-1, -1, true, true, Direction.North, maxHealth, strength)
        {
            this.name = name;
            this.spawnCost = spawnCost;
            this.priority = priority;
        }
    }
}
