// Assets/Scripts/Core/StatusManager.cs
using System;
using System.Collections.Generic; // Listを使用するために追加

namespace Mitsunoazi
{
    public static class StatusManager
    {
        public enum Status
        {
            Crazy,
            Attacker,
            Blocker,
            Healer
        }

        // ▼▼▼ 変更点1: 循環させるステータスの順番を定義するリスト ▼▼▼
        // このリストの要素の順番を入れ替えることで、アビリティの出現順を変更できます。
        private static readonly List<Status> statusOrder = new List<Status>
        {
            Status.Blocker,
            Status.Healer,
            Status.Attacker,
            Status.Crazy
        };

        private static readonly int StatusCount = statusOrder.Count;

        /// <summary>
        /// ステータスをリストの正順で次に進めます。
        /// </summary>
        public static Status GetNextStatus(Status current)
        {
            // ▼▼▼ 変更点2: リストを基準に次のステータスを探すロジックに変更 ▼▼▼
            int currentIndex = statusOrder.IndexOf(current);
            int nextIndex = (currentIndex + 1) % StatusCount;
            return statusOrder[nextIndex];
        }

        /// <summary>
        /// ステータスをリストの逆順で次に進めます。
        /// </summary>
        public static Status GetPreviousStatus(Status current)
        {
            // ▼▼▼ 変更点3: リストを基準に前のステータスを探すロジックに変更 ▼▼▼
            int currentIndex = statusOrder.IndexOf(current);
            int prevIndex = (currentIndex - 1 + StatusCount) % StatusCount;
            return statusOrder[prevIndex];
        }
    }
}