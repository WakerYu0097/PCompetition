using PrisonerCompetition.Core;
using PrisonerCompetition.Tournament;
using PrisonerCompetition.StrategyModule;
using System.Threading.Tasks;

class TournamentConfig
{
    // 可配置参数
    public PayoffMatrix customMatrix = PayoffMatrix.normal;
    public int Rounds { get; set; } = 1000;
    //要加入的玩家
    public StrategyCategory[] Groups { get; set; } =
    [
        //StrategyCategory.Default
    ];
    //单局每轮对战情况
    public (string, string, int, int)[] Records { get; set; } =
    [
        ("Evil", "Random Choice", 1, 20),
    ];
    //单玩家对战所有局情况
    public string[] NamesToDisplay { get; set; } =
    [
        "Evil",
        //"Tit for Tat"
        //"Random Choice"
    ];
}

class Program
{
    static Tournament CreateTournament(TournamentConfig config)
    {
        var tournament = new Tournament(roundsPerMatch: config.Rounds, payoffMatrix: config.customMatrix);
        var players = StrategyLoader.LoadAllStrategies(config.Groups);
        foreach (var (strategy1, strategy2, start, end) in config.Records)
        {
            tournament.AddMatchRecordRequest(strategy1, strategy2, start, end);
        }
        tournament.AddPlayers(players);
        return tournament;
    }

    static async Task Main(string[] args)
    {


        Console.WriteLine("Welcome to the Prisoner's Dilemma Tournament!");

        // 配置参数集中管理
        var config = new TournamentConfig();
        // 创建并配置 tournament
        Tournament tournament = CreateTournament(config);

        int n = tournament.CompetitorCount; // 策略数量
        int totalMatches = n * (n - 1) / 2;
        Console.WriteLine($"players:{n},matches:{totalMatches}");

        // 运行比赛
        await tournament.RunAsync();

        // 输出结果
        
        try
        {
            tournament.PrintSortedScores();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }

        // 输出单策略对战
        foreach (var name in config.NamesToDisplay)
        {
            tournament.PrintAllMatchesFor(name);
        }

        //输出单局对战
        tournament.PrintRecordedMatches();
    }

}
