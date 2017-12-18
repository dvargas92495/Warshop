namespace Z8GameModel.Board
{
    internal class PlaytestMap : Map
    {
        public PlaytestMap(int width, int length) : base(width, length)
        {
            Width = 4;
            Length = 6;

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Length; j++)
                {
                    //Spaces.Add(new Space(Game.GetNewGuid()));
                }
            }
        }
    }
}
