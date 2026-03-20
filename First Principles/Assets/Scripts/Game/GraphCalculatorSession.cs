/// <summary>
/// When set from the main menu, the next <c>Game</c> scene load opens <i>graphing calculator</i> mode
/// (transforms + zoom + pinch) instead of the derivative platformer.
/// </summary>
public static class GraphCalculatorSession
{
    private static bool pendingEnter;

    public static void RequestEnterFromMenu()
    {
        pendingEnter = true;
    }

    /// <summary>True once at Game scene start; clears the request.</summary>
    public static bool ConsumeEnterRequest()
    {
        if (!pendingEnter)
            return false;
        pendingEnter = false;
        return true;
    }
}
