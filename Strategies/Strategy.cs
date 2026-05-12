using System.Reflection;
using PrisonerCompetition.Core;
using PrisonerCompetition.Tournament;
using PrisonerCompetition.StrategyModule;
namespace PrisonerCompetition.Strategy;


#region Example Strategies

[StrategyGroup(StrategyCategory.Example)]
public class AlwaysCooperate : StrategyBase
{
    public override string Name => "Always Cooperate";

    public override string Author => "Waker";

    public override string Description => "This strategy always cooperates.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        return Choice.Cooperate;
    }

    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Example)]
public class AlwaysBetray : StrategyBase
{
    public override string Name => "Always Betray";

    public override string Author => "Waker";

    public override string Description => "This strategy always betrays.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        return Choice.Betray;
    }

    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Example)]
public class RandomChoice : StrategyBase
{
    private Random _random = new Random();

    public override string Name => "Random Choice";

    public override string Author => "Waker";
    public override string Description => "This strategy randomly chooses between cooperate and betray.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        return _random.Next(2) == 0 ? Choice.Cooperate : Choice.Betray;
    }

    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Example)]
public class GrimTrigger : StrategyBase
{
    private bool _opponentBetrayed = false;
    public override string Name => "Grim Trigger (Optimized)";
    public override string Author => "Copilot";
    public override string Description => "Cooperates until opponent betrays once, then always betrays.";

    public override string AliasName =>"";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count > 0 && opponentHistory[opponentHistory.Count - 1] == Choice.Betray)
        {
            _opponentBetrayed = true;
        }
        
        return _opponentBetrayed ? Choice.Betray : Choice.Cooperate;
    }
    
    public override void Reset()
    {
        _opponentBetrayed = false;
    }
}

[StrategyGroup(StrategyCategory.Example)]
public class TitForTwoTats : StrategyBase
{
    public override string Name => "Tit For Two Tats";
    public override string Author => "Copilot";
    public override string Description => "Betrays only if opponent betrays two times in a row.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count < 2)
            return Choice.Cooperate;
        if (opponentHistory[^1] == Choice.Betray && opponentHistory[^2] == Choice.Betray)
            return Choice.Betray;
        return Choice.Cooperate;
    }
    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Example)]
public class Pavlov : StrategyBase
{
    private Choice _lastChoice = Choice.Cooperate;
    public override string Name => "Pavlov (Win-Stay, Lose-Shift)";
    public override string Author => "Copilot";
    public override string Description => "If last round was successful, repeat; otherwise switch.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (myHistory.Count == 0)
        {
            _lastChoice = Choice.Cooperate;
            return _lastChoice;
        }
        // Win if both做同样的选择
        bool win = myHistory[^1] == opponentHistory[^1];
        _lastChoice = win ? myHistory[^1] : (myHistory[^1] == Choice.Cooperate ? Choice.Betray : Choice.Cooperate);
        return _lastChoice;
    }
    public override void Reset()
    {
        _lastChoice = Choice.Cooperate;
    }
}

[StrategyGroup(StrategyCategory.Example)]
public class GenerousTitForTat : StrategyBase
{
    private Random _random = new Random();
    public override string Name => "Generous Tit For Tat";
    public override string Author => "Copilot";
    public override string Description => "Like Tit For Tat, but sometimes forgives betrayal.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count == 0)
            return Choice.Cooperate;
        if (opponentHistory[^1] == Choice.Betray)
            return _random.NextDouble() < 0.3 ? Choice.Cooperate : Choice.Betray; // 30%概率宽容
        return Choice.Cooperate;
    }
    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Example)]
public class SuspiciousTitForTat : StrategyBase
{
    public override string Name => "Suspicious Tit For Tat";
    public override string Author => "Copilot";
    public override string Description => "Starts with betray, then mimics opponent's last move.";
    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count == 0)
            return Choice.Betray;
        return opponentHistory.Last();
    }
    public override void Reset() { }
}

#endregion


[StrategyGroup(StrategyCategory.Competitor)]
public class TitForTat : StrategyBase
{
    public override string Name => "Tit for Tat";

    public override string Author => "Waker";

    public override string Description => "This strategy cooperates on the first move, then mimics the opponent's last move.";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count == 0)
        {
            return Choice.Cooperate;
        }
        return opponentHistory.Last();
    }

    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Competitor)]
public class ByHanbing : StrategyBase
{
    public override string Name => "Choice By hanbing";
    public override string Author => "hanbing";
    public override string Description => "idk and idc how it work";

    public override string AliasName => "";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count == 0)
        {
            return Choice.Cooperate;
        }

        var trust = 0.0;

        var trustScore = 2.0 / (opponentHistory.Count + 0.0) / (opponentHistory.Count + 1.0);
        var index = 1;
        foreach (var myItem in myHistory)
        {
            var oppoItem = opponentHistory[index - 1];
            double trustScorePerChoice = trustScore * index;

            if (myItem == Choice.Cooperate && oppoItem == Choice.Cooperate)       //I trust   you trust
            {
                trust += trustScorePerChoice;
            }
            else if (myItem == Choice.Cooperate && oppoItem != Choice.Cooperate)  //I trust   you betray
            {
                if (index - 2 >= 0)
                {
                    trust -= (trustScorePerChoice / 2);
                    if (opponentHistory[index - 2] == Choice.Betray)
                    {
                        trust -= (trustScorePerChoice / 2);
                    } else {
                        trust += (trustScorePerChoice / 2);
                    }
                }
            }
            else if (myItem != Choice.Cooperate && oppoItem == Choice.Cooperate)  //I betray  you trust
            {
                trust += (trustScorePerChoice / 2);
                /*if (index - 2 >= 0)
                {
                    if (opponentHistory[index - 2] == Choice.Cooperate)
                    {
                        trust += (trustScorePerChoice / 2)
                    } else
                    {
                        trust += (trustScorePerChoice / 2)
                    }
                }*/
            }
            else        //I betray  you betray
            { }

            index++;
        }

        if (trust >= 0.5)
        {
            return Choice.Cooperate;
        }
        else
        {
            return Choice.Betray;
        }
    }

    public override void Reset() { }
}

[StrategyGroup(StrategyCategory.Competitor)]
public class Frequency : StrategyBase
{
    public override string Name => "Frequency to Choice";
    public override string Author => "hanbing";
    public override string Description => "Calculate the frequency and maximize the profit for this time";

    public override string AliasName => "频率依赖决策";

    public override Choice MakeChoice(IList<Choice> myHistory, IList<Choice> opponentHistory)
    {
        if (opponentHistory.Count == 0)
        {
            return Choice.Cooperate;
        }

        float Frequency = 0f;
        float ReciprocalOfSum = 1f / opponentHistory.Count;
        for (int k = 0; k < opponentHistory.Count || k < 10; k++)
        {
            Choice i;
            if (k < opponentHistory.Count)
            {
                i = opponentHistory[k];
            }
            else
            {
                i = Choice.Cooperate;
            }

            if (opponentHistory.Count >= 10)
            {
                if (i == Choice.Cooperate)
                {
                    Frequency += ReciprocalOfSum;
                }
            }
            else
            {
                if (i == Choice.Cooperate)
                {
                    Frequency += 0.1f;
                }
            }

        }

        if ((Frequency >= 0.75) && scores.betraySuccess > (scores.bothCooperate * 2))
        {
            return Choice.Betray;
        }
        else if (Frequency >= 0.5)
        {
            return Choice.Cooperate;
        }
        else
        {
            return Choice.Betray;
        }
    }

    private PayoffMatrix scores;
    public override void Start(PayoffMatrix scores)
    {
        this.scores = scores;
    }
}

