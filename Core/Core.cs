namespace PrisonerCompetition.Core
{
    using System.Collections.Generic;

    public enum Choice
    {
        Cooperate,
        Betray
    }

    public struct RoundResult
    {
        public Choice Player1Choice { get; set; }
        public Choice Player2Choice { get; set; }
        public int Player1Score { get; set; }
        public int Player2Score { get; set; }
    }

    public struct PayoffMatrix
    {
        /// <summary>
        /// me=Betray, opponent=Cooperate
        /// </summary>
        public readonly int betraySuccess;

        /// <summary>
        /// Me=Cooperate, Opponent=Cooperate
        /// </summary>
        public readonly int bothCooperate;

        /// <summary>
        /// me=Betray, opponent=Betray
        /// </summary>
        public readonly int bothBetray;

        /// <summary>
        /// me=Cooperate, opponent=Betray
        /// </summary>
        public readonly int suckersPenalty;



        public PayoffMatrix(int betraySuccess, int bothCooperate, int bothBetray, int suckersPenalty)
        {
            if (betraySuccess <= bothCooperate)
                throw new ArgumentException($"{nameof(betraySuccess)} must be greater than {nameof(bothCooperate)}");

            if (bothCooperate <= bothBetray)
                throw new ArgumentException($"{nameof(bothCooperate)} must be greater than {nameof(bothBetray)}");

            if (bothBetray <= suckersPenalty)
                throw new ArgumentException($"{nameof(bothBetray)} must be greater than {nameof(suckersPenalty)}");

            this.betraySuccess = betraySuccess;
            this.bothCooperate = bothCooperate;
            this.bothBetray = bothBetray;
            this.suckersPenalty = suckersPenalty;
        }

        private static readonly PayoffMatrix defaultPayoff
            = new PayoffMatrix(5, 3, 1, 0);
        private static readonly PayoffMatrix highRisk
            = new PayoffMatrix(8, 3, 2, 0);
        private static readonly PayoffMatrix lowRisk
            = new PayoffMatrix(5, 4, 1, 0);

        public static PayoffMatrix normal => defaultPayoff;
        public static PayoffMatrix risky => highRisk;
        public static PayoffMatrix unrisky => lowRisk;
    }

    public interface IStrategy
    {
        /// <summary>
        /// 策略名称, 必须唯一
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 终端显示的名称,空时显示Name
        /// </summary>
        string AliasName { get; }

        /// <summary>
        /// 策略作者, 可空
        /// </summary>
        string Author { get; }

        /// <summary>
        /// 策略描述, 可空
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 根据历史记录做出选择
        /// </summary>
        /// <param name="myHistory">自己的历史选择</param>
        /// <param name="opponentHistory">对手的历史选择</param>
        /// <returns></returns>
        Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory);

        void Start(PayoffMatrix payoffMatrix);

        /// <summary>
        /// 重置
        /// </summary>
        /// <param name="payoffMatrix">收益矩阵</param>
        void Reset();
    }

    public abstract class StrategyBase : IStrategy
    {
        public abstract string Name { get; }
        public virtual string AliasName { get; } = "";
        public abstract string Author { get; }
        public abstract string Description { get; }

        public string DisplayName
        {
            get
            {
                if (string.IsNullOrEmpty(AliasName))
                    return Name;
                return AliasName;
            }
        }

        public abstract Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory);

        public virtual void Start(PayoffMatrix payoffMatrix) { }

        public virtual void Reset() { }
        
        public override bool Equals(object? obj)
        {
            return obj is IStrategy strategy && Name == strategy.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }

    public class PrisonerCompete
    {
        private readonly StrategyBase _player1;
        private readonly StrategyBase _player2;
        private readonly int _rounds;
        private readonly PayoffMatrix _payoffMatrix;

        private int? player1TotalScore = null;
        private int? player2TotalScore = null;
        private List<RoundResult>? history = null;

        public int Player1TotalScore
        {
            get => player1TotalScore ?? -1;
        }
        public int Player2TotalScore
        {
            get => player2TotalScore ?? -1;
        }
        public List<RoundResult>? History
        {
            get => history;
        }



        public PrisonerCompete(StrategyBase player1, StrategyBase player2, int rounds = 16, PayoffMatrix? scores = null)
        {
            _player1 = player1;
            _player2 = player2;
            _rounds = rounds;
            _payoffMatrix = scores ?? PayoffMatrix.normal;
        }

        public (int player1TotalScore, int player2TotalScore, List<RoundResult> history) Play()
        {
            _player1.Start(_payoffMatrix);
            _player2.Start(_payoffMatrix);

            history = new List<RoundResult>();
            player1TotalScore = 0;
            player2TotalScore = 0;

            List<Choice> player1History = new List<Choice>();
            List<Choice> player2History = new List<Choice>();

            for (int round = 0; round < _rounds; round++)
            {
                Choice player1Choice = _player1.MakeChoice(player1History.AsReadOnly(), player2History.AsReadOnly());
                Choice player2Choice = _player2.MakeChoice(player2History.AsReadOnly(), player1History.AsReadOnly());

                int player1RoundScore = CalculateScore(player1Choice, player2Choice);
                int player2RoundScore = CalculateScore(player2Choice, player1Choice);

                player1TotalScore += player1RoundScore;
                player2TotalScore += player2RoundScore;

                player1History.Add(player1Choice);
                player2History.Add(player2Choice);

                history.Add(new RoundResult
                {
                    Player1Choice = player1Choice,
                    Player2Choice = player2Choice,
                    Player1Score = player1RoundScore,
                    Player2Score = player2RoundScore
                });
            }
            _player1.Reset();
            _player2.Reset();
            return (player1TotalScore ?? -1, player2TotalScore ?? -1, history);
        }

        private int CalculateScore(Choice myChoice, Choice opponentChoice)
        {
            if (myChoice == Choice.Cooperate && opponentChoice == Choice.Cooperate)
                return _payoffMatrix.bothCooperate;
            if (myChoice == Choice.Cooperate && opponentChoice == Choice.Betray)
                return _payoffMatrix.suckersPenalty;
            if (myChoice == Choice.Betray && opponentChoice == Choice.Cooperate)
                return _payoffMatrix.betraySuccess;
            return _payoffMatrix.bothBetray;
        }
    }
}

namespace PrisonerCompetition.Tournament
{
    using PrisonerCompetition.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Utility.Visual;

    public class Tournament
    {
        private readonly List<StrategyBase> _players;
        private readonly int _roundsPerMatch;

        private readonly PayoffMatrix _payoffMatrix;
        private List<TournamentResult> _results;
        private readonly List<MatchRecordRequest> _recordRequests = new();
        /// <summary>
        /// Start,End:索引值,从0开始;
        /// </summary>
        private readonly List<(string Player1, string Player2, int Start, int End, List<RoundResult> Rounds)> _recordedMatches = new();

        public int CompetitorCount => _players.Count;

        /// <summary>
        /// 每场比赛的结果
        /// </summary>
        public List<TournamentResult> ResultPerMatch => _results;

        public Tournament(int roundsPerMatch, PayoffMatrix? payoffMatrix = null)
        {
            _players = new List<StrategyBase>();
            _roundsPerMatch = roundsPerMatch;
            _results = new List<TournamentResult>();
            _payoffMatrix = payoffMatrix ?? PayoffMatrix.normal;
        }

        public void AddPlayer(StrategyBase player)
        {
            _players.Add(player);
        }

        public void AddPlayers(IEnumerable<StrategyBase> players)
        {
            _players.AddRange(players);
        }

        /// <summary>
        /// 添加需要记录的对局区间
        /// </summary>
        public void AddMatchRecordRequest(string player1Name, string player2Name, int start, int end)
        {
            _recordRequests.Add(new MatchRecordRequest { Player1Name = player1Name, Player2Name = player2Name, Start = start - 1, End = end - 1 });
        }

        /// <summary>
        /// 运行比赛,暂时没加入轮输出
        /// </summary>
        public void Run()
        {
            _results.Clear();
            _recordedMatches.Clear();

            for (int i = 0; i < _players.Count; i++)
            {
                for (int j = i + 1; j < _players.Count; j++)
                {
                    StrategyBase player1 = _players[i];
                    StrategyBase player2 = _players[j];

                    var game = new PrisonerCompete(player1, player2, _roundsPerMatch, _payoffMatrix);
                    var (player1Score, player2Score, _) = game.Play();

                    _results.Add(new TournamentResult
                    {
                        Player1 = player1,
                        Player2 = player2,
                        Player1Score = player1Score,
                        Player2Score = player2Score
                    });
                }
            }
        }

        /// <summary>
        /// 异步运行比赛，支持查看 进度&正在进行的对局
        /// </summary>
        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            _results.Clear();
            _recordedMatches.Clear();
            int n = _players.Count;
            int totalMatches = n * (n - 1) / 2;
            int finished = 0;
            var results = new List<TournamentResult>();

            await Task.Run(() =>
            {
                for (int i = 0; i < n; i++)
                {
                    for (int j = i + 1; j < n; j++)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        StrategyBase player1 = _players[i];
                        StrategyBase player2 = _players[j];
                        string pair = $"{player1.Name} vs {player2.Name}";

                        // 进度显示
                        double percent = finished * 100.0 / totalMatches;
                        CleanLine();
                        Console.Write($"Progress: {percent:F2}%  当前: {pair}");

                        var game = new PrisonerCompete(player1, player2, _roundsPerMatch, _payoffMatrix);
                        var (player1Score, player2Score, history) = game.Play();

                        // 检查是否需要记录此对局的区间
                        foreach (var req in _recordRequests)
                        {
                            bool match =
                                (player1.Name == req.Player1Name && player2.Name == req.Player2Name) ||
                                (player1.Name == req.Player2Name && player2.Name == req.Player1Name);
                            if (match)
                            {
                                int start = Math.Max(0, req.Start);
                                int end = Math.Min(history.Count - 1, req.End);
                                if (end >= start)
                                {
                                    var rounds = history.GetRange(start, end - start + 1);
                                    _recordedMatches.Add((player1.Name, player2.Name, start, end, rounds));
                                }
                            }
                        }

                        results.Add(new TournamentResult
                        {
                            Player1 = player1,
                            Player2 = player2,
                            Player1Score = player1Score,
                            Player2Score = player2Score
                        });
                        finished++;
                    }
                }
            });

            CleanLine();
            Console.WriteLine("Progress: 100%. Success!!!");

            _results = results;

            // 本地方法：清除当前行
            static void CleanLine()
            {
                int currentCursorTop = Console.CursorTop;
                Console.SetCursorPosition(0, currentCursorTop);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, currentCursorTop);
            }
        }

        /// <summary>
        /// 计算总分并排序
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public Dictionary<StrategyBase, long> CalSortTotalScores()
        {
            if (_results.Count == 0)
                throw new InvalidOperationException("No results available. Please run the tournament first.");

            Dictionary<StrategyBase, long> totalScores = new Dictionary<StrategyBase, long>();

            foreach (StrategyBase player in _players)
            {
                totalScores[player] = 0;

            }


            foreach (TournamentResult result in _results)
            {
                totalScores[result.Player1] += result.Player1Score;
                totalScores[result.Player2] += result.Player2Score;
            }

            return totalScores.OrderByDescending(x => x.Value).ToDictionary(x => x.Key, x => x.Value);
        }

        public void PrintAllResults()
        {
            if (_results.Count == 0)
            {
                Console.WriteLine("No results available. Please run the tournament first.");
                return;
            }

            Console.WriteLine("Match Results:");
            foreach (TournamentResult result in _results)
            {
                Console.WriteLine($"{result.Player1.DisplayName} vs {result.Player2.DisplayName}: {result.Player1Score} - {result.Player2Score}");
            }
        }

        /// <summary>
        /// 打印指定策略所有对战情况（对手、比分）
        /// </summary>
        public void PrintAllMatchesFor(string name)
        {
            if (_results == null || _results.Count == 0)
            {
                Console.WriteLine("No results available. Please run the tournament first.");
                return;
            }

            var player = _players.FirstOrDefault(p => p.Name == name);
            string displayName = player?.DisplayName ?? name;

            var matches = _results
                .Where(r => r.Player1.Name == name || r.Player2.Name == name)
                .Select(r => new
                {
                    Opponent = r.Player1.Name == name ? r.Player2 : r.Player1,
                    MyScore = r.Player1.Name == name ? r.Player1Score : r.Player2Score,
                    OpponentScore = r.Player1.Name == name ? r.Player2Score : r.Player1Score
                })
                .ToList();

            if (matches.Count == 0)
            {
                Console.WriteLine($"{name} did not participate in any matches.");
                return;
            }
            ColorConsole.WriteLineColor($"All matches for \"{displayName}\":", ConsoleColor.Yellow);
            foreach (var match in matches)
            {
                Console.WriteLine($"vs {match.Opponent.DisplayName}: {match.MyScore.ToString("N0")} - {match.OpponentScore.ToString("N0")}");
            }
        }

        /// <summary>
        /// 打印排序后的总分
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void PrintSortedScores()
        {
            Dictionary<StrategyBase, long> sortedScores = CalSortTotalScores();
            int rank = 1;
            ColorConsole.WriteLineColor("Here are Scores for each player:",ConsoleColor.Green);
            foreach (KeyValuePair<StrategyBase, long> kvp in sortedScores)
            {
                Console.WriteLine($"{rank}. {kvp.Key.DisplayName}: {kvp.Value.ToString("N0")}");
                rank++;
            }
        }

        /// <summary>
        /// 打印所有已记录的对局区间
        /// </summary>
        public void PrintRecordedMatches()
        {
            if (_recordedMatches.Count == 0)
            {
                Console.WriteLine("No recorded match intervals.");
                return;
            }
            foreach (var rec in _recordedMatches)
            {
                // 计算动作最大长度
                int actionMaxLen = Math.Max("Cooperate".Length, "Betray".Length);

                int roundColWidth = Math.Max(7, $"Round {rec.End + 1}".Length);
                int name1Width = Math.Max(Math.Max(rec.Player1.Length + 1, 8), actionMaxLen);
                int name2Width = Math.Max(Math.Max(rec.Player2.Length + 1, 8), actionMaxLen);
                int scoreWidth = Math.Max(5, $"{rec.Rounds[0].Player1Score}-{rec.Rounds[0].Player2Score}".Length);

                // 打印表头
                ColorConsole.WriteLineColor($"Match: {rec.Player1} vs {rec.Player2}, Rounds {rec.Start + 1} to {rec.End + 1}",ConsoleColor.Blue);
                string header =
                    PadRight("Round", roundColWidth) + " " +
                    PadRight(rec.Player1, name1Width) + " " +
                    PadRight(rec.Player2, name2Width) + " " +
                    PadRight("Score", scoreWidth);
                Console.WriteLine(header);

                // 打印每轮
                for (int i = 0; i < rec.Rounds.Count; i++)
                {
                    var r = rec.Rounds[i];
                    string roundStr = $"{rec.Start + 1 + i}";
                    string p1Action = r.Player1Choice.ToString();
                    string p2Action = r.Player2Choice.ToString();
                    string scoreStr = $"{r.Player1Score}-{r.Player2Score}";
                    Console.WriteLine(
                        PadRight(roundStr, roundColWidth) + " " +
                        PadRight(p1Action, name1Width) + " " +
                        PadRight(p2Action, name2Width) + " " +
                        PadRight(scoreStr, scoreWidth)
                    );
                }
            }

            // 辅助方法
            static string PadRight(string s, int width)
                => s.PadRight(width);
            // static string PadLeft(string s, int width)
            //     => s.PadLeft(width);
        }

        /// <summary>
        /// 打印排名前N的总分
        /// </summary>
        /// <param name="topN"></param>
        /// <exception cref="InvalidOperationException"></exception>
        public void PrintRunTop(int topN = 3)
        {
            Dictionary<StrategyBase, long> sortedScores = CalSortTotalScores();

            Console.WriteLine("Total Scores:");

            int rank = 1;
            foreach (KeyValuePair<StrategyBase, long> kvp in sortedScores.Take(topN))
            {
                Console.WriteLine($"{rank}. {kvp.Key.DisplayName}: {kvp.Value}");
                rank++;
            }
        }

        /// <summary>
        /// --每场比赛的结果--
        /// Player1Name:Player1Score vs Player2Name:Player2Score
        /// </summary>
        public struct TournamentResult
        {
            public StrategyBase Player1 { get; set; }
            public StrategyBase Player2 { get; set; }

            public int Player1Score { get; set; }
            public int Player2Score { get; set; }
        }

        // 记录请求结构
        private class MatchRecordRequest
        {
            public string Player1Name { get; set; } = string.Empty;
            public string Player2Name { get; set; } = string.Empty;
            public int Start { get; set; }
            public int End { get; set; }
        }
    }
}
