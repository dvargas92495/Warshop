namespace Z8GameModel.Board
{
    class WaterSpace : Space
    {
        public WaterSpace(int guid) : base(guid) { }

        new public void LandOn(int objectID)
        {
            Game.Kill(objectID);
        }
    }
}
