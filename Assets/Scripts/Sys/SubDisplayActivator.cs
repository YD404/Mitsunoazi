using UnityEngine;

namespace Mitsunoazi
{
    /// <summary>
    /// サブディスプレイが利用可能な場合に有効化するスクリプト
    /// </summary>
    public class SubDisplayActivator : MonoBehaviour
    {
        void Awake()
        {
            // PCに接続されているディスプレイが2台以上あるかチェック
            if (Display.displays.Length > 1)
            {
                // 2台目のディスプレイを有効化する
                Display.displays[1].Activate();
                Debug.Log("Sub display activated.");
            }
            else
            {
                Debug.Log("Sub display not found.");
            }
        }
    }
}
