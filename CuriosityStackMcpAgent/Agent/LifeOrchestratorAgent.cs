namespace CuriosityStack.Agent.LifeOrchestrator;

public interface ILifeIntentRouter
{
    string Route(string intent);
}

public sealed class LifeIntentRouter : ILifeIntentRouter
{
    public string Route(string intent)
    {
        if (intent.Contains("cash", StringComparison.OrdinalIgnoreCase) || intent.Contains("net worth", StringComparison.OrdinalIgnoreCase))
        {
            return "finance";
        }

        if (intent.Contains("training", StringComparison.OrdinalIgnoreCase) || intent.Contains("weight", StringComparison.OrdinalIgnoreCase) || intent.Contains("recovery", StringComparison.OrdinalIgnoreCase))
        {
            return "health";
        }

        if (intent.Contains("deadline", StringComparison.OrdinalIgnoreCase) || intent.Contains("project", StringComparison.OrdinalIgnoreCase))
        {
            return "projects";
        }

        if (intent.Contains("journal", StringComparison.OrdinalIgnoreCase) || intent.Contains("decision", StringComparison.OrdinalIgnoreCase))
        {
            return "journal";
        }

        return "life";
    }
}
