using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    [Tooltip("この秒数が経過した後に、このオブジェクトを破壊します")]
    public float lifetime = 20f;

    void Start()
    {
        // ゲームオブジェクトを、lifetime秒後に破壊する
        Destroy(gameObject, lifetime);
    }
}