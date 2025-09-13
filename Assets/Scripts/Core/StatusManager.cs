// Assets/Scripts/Core/StatusManager.cs
using System; // Enum.GetValues を使用するために追加

public static class StatusManager
{
    // ★修正: enumをクラスの内部に移動し、関連性を明確化
    public enum Status
    {
        Crazy,
        Attacker,
        Blocker,
        Healer
    }

    // ★修正: ハードコーディングされた数値を削除し、enumのメンバー数から動的に取得
    private static readonly int StatusCount = Enum.GetValues(typeof(Status)).Length;

    /// <summary>
    /// ステータスを正順で次に進めます。
    /// </summary>
    public static Status GetNextStatus(Status current)
    {
        int next = ((int)current + 1) % StatusCount;
        return (Status)next;
    }

    /// <summary>
    /// ステータスを逆順で次に進めます。
    /// </summary>
    public static Status GetPreviousStatus(Status current)
    {
        int prev = ((int)current - 1 + StatusCount) % StatusCount;
        return (Status)prev;
    }
}
