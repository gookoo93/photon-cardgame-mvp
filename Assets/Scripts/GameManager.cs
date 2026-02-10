using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum GameState
    {
        WaitingForPlayers,
        Round_Pick,
        Round_Reveal,
        Round_Result,
        Game_End
    }

    public GameState currentState;

    private void Start()
    {
        currentState = GameState.WaitingForPlayers;
        Debug.Log("Game State: " + currentState);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Game State Changed To: " + currentState);
    }
}
