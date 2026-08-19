using System.Collections.Generic;

public static class PortAssigner
{
    private static readonly Dictionary<string, string> _assigned = new();
    private static int _next       = 9000;   // corridoi e porte
    private static int _nextObject = 9500;   // oggetti-artefatto (range separato)

    public static string GetPort(string name)
    {
        if (!_assigned.ContainsKey(name))
            _assigned[name] = (_next++).ToString();
        return _assigned[name];
    }

    // Artefatti-oggetto: range dedicato, non interferisce con corridoi/porte
    public static string GetObjectPort(string name)
    {
        if (!_assigned.ContainsKey(name))
            _assigned[name] = (_nextObject++).ToString();
        return _assigned[name];
    }
}