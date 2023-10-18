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

        private (bool, int, int, List<(int, int)>) Play()
        {
            var game = new Game((XMe, YMe), Daleks);
            string crashStr = "";
            for (int i = 0; !game.IsLost && i < Step; i++)
                crashStr = game.Play(Moves[i]);

            if (game.IsLost)
            {
                CurrentPosLabel.Content = crashStr;
                return (false, 0, 0, Enumerable.Empty<(int, int)>().ToList());
            }
            var s = "";
            foreach (var (xd, yd) in game.Daleks)
            {
                if (s != "")
                    s += " ";
                s += $"{xd} {yd}";
            }
            s += $" {game.Player.x} {game.Player.y}";
            if (!string.IsNullOrEmpty(crashStr))
                s += " -- " + crashStr;
            CurrentPosLabel.Content = s;
            return (true, game.Player.x, game.Player.y, game.Daleks);
        }

        private void DrawGame()
        {
            var (ok, xMe, yMe, daleks) = Play();
            GameCanvas.Children.Clear();
            if (!ok)
            {
                TextBlock textBlock = new TextBlock();
                textBlock.Text = "EXTERMINATE !!!";
                textBlock.Foreground = dalekBrush;
                textBlock.FontSize = 96;
                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(textBlock, (GameCanvas.ActualWidth - textBlock.DesiredSize.Width) / 2);
                Canvas.SetTop(textBlock, (GameCanvas.ActualHeight - textBlock.DesiredSize.Height) / 2);
                GameCanvas.Children.Add(textBlock);
                return;
            }
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

        private void Solve_Click(object sender, RoutedEventArgs e)
        {
            var (ok, xMe, yMe, daleks) = Play();
            if (!ok)
            {
                CurrentPosLabel.Content = $"Already lost !";
                return;
            }

            var solver = new Game((xMe, yMe), daleks);
            string solution;
            (ok, solution) = solver.Solve();
            if (!ok)
            {
                CurrentPosLabel.Content = $"No solution : {solution}";
                return;
            }

            int curPos = MovesTextBox.SelectionStart;
            MovesTextBox.Text = MovesTextBox.Text[..curPos] + solution;
            MovesTextBox.SelectionStart = curPos;
            MovesTextBox.Focus();
        }
    }
}
