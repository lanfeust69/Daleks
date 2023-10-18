namespace Daleks
{
    internal class Game
    {
        public (int x, int y) Player { get; private set; }
        public List<(int x, int y)> Daleks { get; }
        public bool IsLost { get; private set; }

        public Game((int, int) player, IEnumerable<(int, int)> daleks)
        {
            Player = player;
            Daleks = daleks.ToList();
        }

        public string Play(char move)
        {
            var (x, y) = Player;
            string crashStr = "";
            IsLost = true;

            // 1 - move
            switch (move)
            {
                case 'u':
                    y++;
                    break;
                case 'd':
                    y--;
                    break;
                case 'r':
                    x++;
                    break;
                case 'l':
                    x--;
                    break;
            }
            Player = (x, y);

            // 2 - if we moved into a dalek, that's bad, even if there are *2* (which will immediately crash)
            if (Daleks.Contains(Player))
                return $"moved into dalek at ({x}, {y})"; // we lost :-(

            // 3 - only now do we remove crashed daleks
            var crashed = new HashSet<int>();
            for (int j = 0; j < Daleks.Count - 1; j++)
            {
                if (crashed.Contains(j))
                    continue;
                for (int k = j + 1; k < Daleks.Count; k++)
                {
                    if (Daleks[j] == Daleks[k])
                    {
                        crashStr = (string.IsNullOrEmpty(crashStr) ? "crash at " : " and ") + $"({Daleks[j].x}, {Daleks[j].y})";
                        crashed.Add(j);
                        crashed.Add(k);
                    }
                }
            }
            foreach (int remove in crashed.OrderDescending())
                Daleks.RemoveAt(remove);

            // 4 - non crashed daleks move
            for (int i = 0; i < Daleks.Count; i++)
            {
                var (dalekX, dalekY) = Daleks[i];
                int movedDalekX = dalekX < x ? dalekX + 1 : dalekX > x ? dalekX - 1 : dalekX;
                int movedDalekY = dalekY < y ? dalekY + 1 : dalekY > y ? dalekY - 1 : dalekY;
                if ((movedDalekX, movedDalekY) == Player)
                    return $"dalek at ({dalekX}, {dalekY}) killed us"; // we lost :-(
                Daleks[i] = (movedDalekX, movedDalekY);
            }

            IsLost = false;
            return crashStr;
        }
    }
}
