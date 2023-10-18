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

        public (bool, string) Solve()
        {
            if (Daleks.Count == 0)
                return (true, "");

            var (groups, error) = FindGroups();
            if (groups.Count == 0)
                return (false, error);

            var game = new Game(Player, Daleks);
            string solution = "";
            while (groups.Count > 0)
            {
                char best = '\0';
                int bestImprovement = 0;
                var (x, y) = game.Player;
                int closestDist = 1000000;
                foreach (var (isCol, group) in groups)
                    closestDist = Math.Min(closestDist, Math.Abs(isCol ? group[0].x - x : group[0].y - y));

                foreach (var move in "udlr")
                {
                    var (xx, yy) = move switch
                    {
                        'u' => (x, y + 1),
                        'd' => (x, y - 1),
                        'l' => (x - 1, y),
                        'r' => (x + 1, y),
                        _ => throw new Exception()
                    };
                    bool ok = true;
                    int improvement = 0;
                    foreach (var (isCol, group) in groups)
                    {
                        List<int> vals;
                        int groupOther, cur, moved, curOther, movedOther;
                        if (isCol)
                        {
                            (groupOther, curOther, movedOther) = (group[0].x, x, xx);
                            vals = group.Select(d => d.y).Order().ToList();
                            (cur, moved) = (y, yy);
                        }
                        else
                        {
                            (groupOther, curOther, movedOther) = (group[0].y, y, yy);
                            vals = group.Select(d => d.x).Order().ToList();
                            (cur, moved) = (x, xx);
                        }
                        if (Math.Abs(movedOther - groupOther) < 2)
                        {
                            ok = false;
                            break;
                        }
                        if (vals[0] == vals[^1])
                            continue; // destroyed, won't step in because previous step
                        if (vals.Count == 3)
                        {
                            int target = (vals[0] + vals[2]) / 2;
                            if (vals[2] - vals[0] == 3)
                            {
                                target = vals[1] == vals[0] + 1 ? vals[2] : vals[0];
                            }
                            // be careful at the end : reaching target is essential
                            if (vals[2] - vals[0] <= 3)
                                improvement = moved == target ? 3 : 0;
                            else if (Math.Abs(moved - target) < Math.Abs(cur - target))
                            {
                                bool wrong_side = target >= vals[1] != cur >= vals[1];
                                improvement = Math.Max(improvement, wrong_side ? 2 : 1);
                            }
                        }
                        else
                        {
                            if ((cur < vals[0] && moved > cur) ||
                                (cur > vals[1] && moved < cur))
                                improvement = Math.Max(improvement, 1);
                        }
                        int dist = Math.Abs(curOther - groupOther);
                        if (dist == closestDist && Math.Abs(movedOther - groupOther) > dist)
                            improvement = Math.Max(improvement, 1);
                    }
                    if (!ok)
                        continue;
                    if (best == '\0' || improvement > bestImprovement)
                    {
                        best = move;
                        bestImprovement = improvement;
                    }
                }
                if (best == '\0')
                    return (false, "could not find a move");
                solution += best;
                game.Play(best);
                if (game.IsLost)
                    return (false, "weird : best move should not lose");

                // remove merged daleks
                var toRemove = new List<int>();
                for (int i = groups.Count - 1; i >= 0; i--)
                {
                    var (isCol, group) = groups[i];
                    var merged = group.GroupBy(d => isCol ? d.y : d.x);
                    int count = merged.Count();
                    int other = isCol ? group[0].x : group[0].y;
                    if (count == 1)
                    {
                        toRemove.Add(i);
                    }
                    else if (count != group.Count)
                    {
                        var newGroup = merged.Where(g => g.Count() == 1).Select(g => isCol ? (other, g.Key) : (g.Key, other)).ToList();
                        if (newGroup.Count == 0)
                            toRemove.Add(i);
                        else if (newGroup.Count == 1)
                            return (false, "failed to completely destroy a group");
                        else groups[i] = (isCol, newGroup);
                    }
                }
                foreach (int i in toRemove)
                    groups.RemoveAt(i);

                int nbSeen = 0;
                foreach (var (_, group) in groups)
                {
                    nbSeen += group.Count;
                    for (int i = 0; i < group.Count; i++)
                    {
                        var (xx, yy) = group[i];
                        xx = xx < game.Player.x ? xx + 1 : xx > game.Player.x ? xx - 1 : xx;
                        yy = yy < game.Player.y ? yy + 1 : yy > game.Player.y ? yy - 1 : yy;
                        group[i] = (xx, yy);

                        if (!game.Daleks.Contains((xx, yy)))
                            return (false, $"unexpected dalek at ({xx}, {yy})");
                    }
                }
                if (nbSeen != game.Daleks.Count)
                    return (false, $"unexpected number of daleks ({nbSeen} instead of {game.Daleks.Count})");
            }
            return (true, solution);
        }

        private (List<(bool isCol, List<(int x, int y)> members)>, string) FindGroups()
        {
            var byCol = Daleks.GroupBy(d => d.x).ToDictionary(g => g.Key, g => g.ToList());
            var byRow = Daleks.GroupBy(d => d.y).ToDictionary(g => g.Key, g => g.ToList());
            var empty = Enumerable.Empty<(bool isCol, List<(int, int)> members)>().ToList();
            var groups = new List<(bool isCol, List<(int, int)> members)>();
            var groupIds = new Dictionary<(int, int), int>();
            bool progress = true;
            while (progress)
            {
                progress = false;
                foreach (var (x, y) in Daleks)
                {
                    if (groupIds.ContainsKey((x, y)))
                        continue;
                    if (byCol[x].Count == 1 && byRow[y].Count == 1)
                        return (empty, $"dalek at ({x}, {y}) cannot be destroyed");
                    if (byCol[x].Count > 1 && byRow[y].Count > 1)
                        continue; // hopefully will be picked up by a "needy" other
                                  //return (empty, "dalek at ({x}, {y}) has other daleks both on its row and column : not supported");
                    int groupId = groups.Count;
                    var group = new List<(int, int)> { (x, y) };
                    bool added = false;
                    bool isCol;
                    if (byCol[x].Count == 1)
                    {
                        isCol = false;
                        if (y == Player.y)
                            return (empty, $"dalek at ({x}, {y}) cannot be destroyed");
                        foreach (var other in byRow[y])
                        {
                            if (other == (x, y))
                                continue;
                            if (byRow[y].Count > 2 && byCol[other.x].Count > 1)
                                continue;
                            added = true;
                            groupIds[other] = groupId;
                            group.Add(other);
                        }
                    }
                    else
                    {
                        isCol = true;
                        if (x == Player.x)
                            return (empty, $"dalek at ({x}, {y}) cannot be destroyed");
                        foreach (var other in byCol[x])
                        {
                            if (other == (x, y))
                                continue;
                            if (byCol[x].Count > 2 && byRow[other.y].Count > 1)
                                continue;
                            added = true;
                            groupIds[other] = groupId;
                            group.Add(other);
                        }
                    }
                    if (!added)
                        continue; // too many choices, hopefully some will be picked up later
                    progress = true;
                    group.Sort();
                    groups.Add((isCol, group));
                    groupIds[(x, y)] = groupId;
                    foreach (var (xx, yy) in group)
                    {
                        byCol[xx].Remove((xx, yy));
                        byRow[yy].Remove((xx, yy));
                    }
                }
            }

            if (groupIds.Count != Daleks.Count)
                return (empty, $"grouping ambiguous : not supported");

            return (groups, "");
        }
    }
}
