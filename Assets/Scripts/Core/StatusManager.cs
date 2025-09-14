// Assets/Scripts/Core/StatusManager.cs
using System;

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
}
