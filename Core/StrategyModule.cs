using System.Reflection;
using PrisonerCompetition.Core;
using PrisonerCompetition.Tournament;

namespace PrisonerCompetition.StrategyModule;

#region Strategy Loader
public static class StrategyLoader
{
    public static List<StrategyBase> LoadAllStrategies(params StrategyCategory[] groups)
    {
        if (groups == null || groups.Length == 0)
            groups = Enum.GetValues<StrategyCategory>();

        var strategies = new List<StrategyBase>();
        var assembly = Assembly.GetEntryAssembly();
        var strategyTypes = assembly!.GetTypes()
            .Where(t => typeof(StrategyBase).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Distinct()
            .OrderBy(t => t.Name)
            .ToList();

        foreach (var type in strategyTypes)
        {
            try
            {
                // 只尝试无参构造
                var ctor = type.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    Console.WriteLine($"✗ 跳过 {type.Name}: 无无参构造函数");
                    continue;
                }

                var groupAttr = type.GetCustomAttribute<StrategyGroupAttribute>();
                if (groupAttr == null)
                    continue;// 没有标记组别的策略不加载

                if (!groups.Contains(groupAttr.Category))
                {
                    // Console.WriteLine($"✗ 跳过 {type.Name}: 不在指定组别内 ({groupAttr.Category})");
                    continue;
                }


                var strategy = (StrategyBase)ctor.Invoke(null);
                if (strategy != null)
                    strategies.Add(strategy);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ 加载失败 {type.Name}: {ex.Message}");
            }
        }
        return strategies;
    }
}

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class StrategyGroupAttribute : Attribute
{
    public StrategyCategory Category { get; }

    public StrategyGroupAttribute(StrategyCategory category)
    {
        Category = category;
    }
}

public enum StrategyCategory
{
    Default,
    Example,
    Competitor
}
#endregion

