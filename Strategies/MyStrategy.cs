using PrisonerCompetition.Core;
using PrisonerCompetition.StrategyModule;
namespace PrisonerCompetition.Strategy;

[StrategyGroup(StrategyCategory.Competitor)]
public class Evil : StrategyBase
{
    public override string Name => "Evil";
    public override string Author => "Waker";
    public override string Description => "基于对手心理分析的策略";

    private int round = 0;
    private Random random = new Random();
    private int myMemory = 16;
    private int startRound = 4;
    private Queue<Choice> myChoices = new Queue<Choice>();
    private Queue<Choice> oppChoices = new Queue<Choice>();
    private OpponentCH opponentCH=new OpponentCH();
    private bool hasShownHighRevenge = false;
    private float opponentOverallBetrayRate = 0f;
    private float opponentRecentBetrayRate = 0f; // 对手近期背叛率

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        round = myHistory.Count;
        // 更新记忆队列
        if (round > 0)
        {
            if (round <= myMemory)
            {
                myChoices.Enqueue(myHistory.Last());
                oppChoices.Enqueue(opponentHistory.Last());
            }
            else
            {
                myChoices.Dequeue();
                oppChoices.Dequeue();
                myChoices.Enqueue(myHistory.Last());
                oppChoices.Enqueue(opponentHistory.Last());
            }
        }

        // 前几轮收集数据
        if (round < startRound)
            return Choice.Cooperate;

        // 分析对手心理
        if (round >= startRound)
            AnalyzePsychology(opponentHistory);

        // 基于心理分析决策
        return MakeDecision();
    }

    private void AnalyzePsychology(IList<Choice> opponentHistory)
    {
        if (myChoices.Count < 2) return;

        var myArr = myChoices.ToArray();
        var oppArr = oppChoices.ToArray();

        float evilScore = 0f;
        float revengeScore = 0f;
        int coopCount = 0, betrayCount = 0;

        for (int i = 0; i < myArr.Length - 1; i++)
        {
            if (myArr[i] == Choice.Cooperate)
            {
                coopCount++;
                if (oppArr[i + 1] == Choice.Betray)
                    evilScore += 1f;
            }
            else
            {
                betrayCount++;
                if (oppArr[i + 1] == Choice.Betray)
                    revengeScore += 1f;
            }
        }

        // 计算心理值
        float newEvil = coopCount > 0 ? evilScore / coopCount : 0f;
        float newRevenge = betrayCount > 0 ? revengeScore / betrayCount : 0f;

        opponentCH.evilBalance.Update(newEvil);
        opponentCH.revengeBalance.Update(newRevenge);

        opponentCH.evil = opponentCH.evilBalance.Value;
        opponentCH.revenge = opponentCH.revengeBalance.Value;

        // 计算对手总体背叛率
        int totalBetray = 0;
        for (int i = 0; i < opponentHistory.Count; i++)
        {
            if (opponentHistory[i] == Choice.Betray)
                totalBetray++;
        }
        opponentOverallBetrayRate = (float)totalBetray / opponentHistory.Count;

        // 计算对手近期背叛率（最近8轮）
        int recentBetray = 0;
        int recentRounds = Math.Min(8, opponentHistory.Count);
        for (int i = opponentHistory.Count - recentRounds; i < opponentHistory.Count; i++)
        {
            if (i >= 0 && opponentHistory[i] == Choice.Betray)
                recentBetray++;
        }
        opponentRecentBetrayRate = recentRounds > 0 ? (float)recentBetray / recentRounds : 0f;

        // 记录对手是否曾表现出高revenge特性
        if (opponentCH.revenge > 0.7f)
            hasShownHighRevenge = true;
    }

    private Choice MakeDecision()
    {
        // 如果对手近期背叛率很高，直接背叛
        if (opponentRecentBetrayRate > 0.6f)
            return Choice.Betray;

        // 如果对手总体背叛率很高，直接背叛
        if (opponentOverallBetrayRate > 0.7f)
            return Choice.Betray;

        // 如果对手曾表现出高revenge特性，且当前revenge值仍较高，倾向于合作
        if (hasShownHighRevenge && opponentCH.revenge > 0.3f && opponentCH.evil < 0.3f)
            return Choice.Cooperate;

        // 高evil对手：经常背叛 → 我们背叛
        if (opponentCH.evil > 0.5f)
            return Choice.Betray;

        // 高revenge对手：报复心强 → 我们合作
        if (opponentCH.revenge > 0.6f && opponentCH.evil < 0.3f) // 提高阈值并添加evil条件
            return Choice.Cooperate;

        // 宽容对手：evil和revenge都低 → 我们背叛获得最大收益
        if (opponentCH.evil < 0.3f && opponentCH.revenge < 0.3f)
            return Choice.Betray;

        // 默认情况：倾向于背叛
        return Choice.Betray;
    }

    public override void Reset()
    {
        opponentCH = new OpponentCH();
        myChoices.Clear();
        oppChoices.Clear();
        round = 0;
        hasShownHighRevenge = false;
        opponentOverallBetrayRate = 0f;
        opponentRecentBetrayRate = 0f;
    }

    private struct OpponentCH
    {
        public float revenge;
        public float evil;
        public SmoothValue revengeBalance;
        public SmoothValue evilBalance;

        public OpponentCH()
        {
            revenge = 0f;
            evil = 0f;
            revengeBalance = new SmoothValue(0.5f);
            evilBalance = new SmoothValue(0.5f);
        }
    }

    private class SmoothValue
    {
        private readonly float sensitivity;
        private float _value;
        private float _target;

        public float Value => _value;

        public void Update(float input)
        {
            _target = input;
            _value += (input - _value) * sensitivity;
            if (Math.Abs(_value - _target) < 0.001f)
                _value = _target;
        }

        public SmoothValue(float sensitivity = 0.5f)
        {
            this.sensitivity = Math.Clamp(sensitivity, 0f, 1f);
            _value = 0f;
            _target = 0f;
        }
    }
}