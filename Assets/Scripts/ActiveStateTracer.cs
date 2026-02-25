using UnityEngine;

public class ActiveStateTracer : MonoBehaviour
{
    private void OnDisable()
    {
        Debug.LogError($"[ActiveStateTracer] {name} DISABLED!\n{StackTrace()}");
    }

    private string StackTrace()
    {
        // 누가 껐는지 호출 스택 보여줌
        return System.Environment.StackTrace;
    }
}