using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Daleks
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public string Moves { get; set; } = "";
        public int Step { get; set; }

        int XMe, YMe, XMin, XMax, YMin, YMax;
        double XScale, YScale;
        List<(int, int)> Daleks { get; set; } = new();

        SolidColorBrush myBrush = new SolidColorBrush { Color = Colors.Blue };

        SolidColorBrush dalekBrush = new SolidColorBrush { Color = Colors.Red };

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Reset();
            DrawGame();
        }

        private (int, int, List<(int, int)>) Play()
        {
            var daleks = Daleks.ToList();
            int x = XMe, y = YMe;
            string crashStr = "";
            for (int i = 0; i < Step; i++)
            {
                // 1 - move
                switch (Moves[i])
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
                // 2 - if we moved into a dalek, that's bad, even if there are *2* (which will immediately crash)
                if (daleks.Contains((x, y)))
                    return (0, 0, new List<(int, int)>()); // we lost :-(
                // 3 - only now do we remove crashed daleks
                var crashed = new HashSet<int>();
                for (int j = 0; j < daleks.Count - 1; j++)
                {
                    if (daleks[j] == (x, y))
                        return (0, 0, new List<(int, int)>()); // we lost :-(
                    if (crashed.Contains(j))
                        continue;
                    for (int k = j + 1; k < daleks.Count; k++)
                    {
                        if (daleks[j] == daleks[k])
                        {
                            if (i == Step - 1)
                                crashStr = (string.IsNullOrEmpty(crashStr) ? "  --  crash at " : " and ") + $"({daleks[j].Item1}, {daleks[j].Item2})";
                            crashed.Add(j);
                            crashed.Add(k);
                        }
                    }
                }
                foreach (int remove in crashed.OrderDescending())
                    daleks.RemoveAt(remove);

                // 4 - non crashed daleks move
                var movedDaleks = daleks.Select(dalek =>
                    (dalek.Item1 < x ? dalek.Item1 + 1 : dalek.Item1 > x ? dalek.Item1 - 1 : dalek.Item1,
                     dalek.Item2 < y ? dalek.Item2 + 1 : dalek.Item2 > y ? dalek.Item2 - 1 : dalek.Item2)).ToList();
                for (int j = 0; j < movedDaleks.Count - 1; j++)
                {
                    if (movedDaleks[j] == (x, y))
                        return (0, 0, new List<(int, int)>()); // we lost :-(
                }
                daleks = movedDaleks;
            }
            var s = "";
            foreach (var (xd, yd) in daleks)
            {
                if (s != "")
                    s += " ";
                s += $"{xd} {yd}";
            }
            s += $" {x} {y}" + crashStr;
            CurrentPosLabel.Content = s;
            return (x, y, daleks);
        }

        private void DrawGame()
        {
            var (xMe, yMe, daleks) = Play();
            GameCanvas.Children.Clear();
            var me = new Rectangle();
            me.Width = XScale;
            me.Height = YScale;
            me.Fill = myBrush;
            Canvas.SetLeft(me, (xMe - XMin) * XScale);
            Canvas.SetTop(me, (YMax - yMe) * YScale);
            GameCanvas.Children.Add(me);
            foreach (var (x, y) in daleks)
            {
                var dalek = new Rectangle();
                dalek.Width = XScale;
                dalek.Height = YScale;
                dalek.Fill = dalekBrush;
                Canvas.SetLeft(dalek, (x - XMin) * XScale);
                Canvas.SetTop(dalek, (YMax - y) * YScale);
                GameCanvas.Children.Add(dalek);
            }
        }

        private void UpdateGame()
        {
            Step = MovesTextBox.SelectionStart;
            Moves = MovesTextBox.Text;
            DrawGame();
        }

        private void Reset()
        {
            Daleks.Clear();
            //const string s = "56 111 18 51 106 51 51 95 97 111 18 120 51 59 52 51 51 58 77 88 55 111 75 88 71 79";
            string s = InputTextBox.Text;
            List<int> coords;
            if (string.IsNullOrWhiteSpace(s))
            {
                coords = new List<int> { 0, 0 };
            }
            else
            {
                try
                {
                    coords = s.Split().Select(int.Parse).ToList();
                    if (coords.Count % 2 != 0)
                        throw new Exception("need an even number of integers to form coordinates");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid input : {ex}");
                    return;
                }
            }

            XMe = XMin = XMax = coords[^2];
            YMe = YMin = YMax = coords[^1];
            for (int i = 0; i < coords.Count / 2 - 1; i++)
            {
                int x = coords[i * 2], y = coords[i * 2 + 1];
                XMin = Math.Min(XMin, x);
                XMax = Math.Max(XMax, x);
                YMin = Math.Min(YMin, y);
                YMax = Math.Max(YMax, y);
                Daleks.Add((x, y));
            }
            XMin -= 10;
            XMax += 10;
            YMin -= 10;
            YMax += 10;

            XScale = GameCanvas.ActualWidth / (XMax - XMin);
            YScale = GameCanvas.ActualHeight / (YMax - YMin);

            Moves = "";
            Step = 0;
        }

        private void TextBox_SelectionChanged(object sender, RoutedEventArgs e)
        {
            UpdateGame();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            Reset();
            UpdateGame();
        }
    }
}
