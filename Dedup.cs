namespace BybitExecution;

static class Dedup
{
    private static readonly object _lock = new();
    private static readonly HashSet<string> _seen = new();
    private static readonly Queue<string> _order = new();

    private const int Capacity = 10000;

    public static bool IsDuplicate(string? key)
    {
        if(string.IsNullOrEmpty(key))
        {
            return false;
        }

        lock(_lock)
        {
            if(_seen.Contains(key))
            {
                return true;
            }

            _seen.Add(key);
            _order.Enqueue(key);

            if(_seen.Count > Capacity)
            {
                var old = _order.Dequeue();
                _seen.Remove(old);
            }

            return false;
        }
    }
}