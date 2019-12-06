using UnityEngine;

public class Main : MonoBehaviour
{
    public UGS UGSInstance;

    void Start()
    {
        Debug.Log("START");
        UGSInstance.Login("api_test", "passwd123", OnLoginSuccess, OnLoginFailed);
    }

    public void OnLoginSuccess()
    {
        Debug.Log("Successfully Logged In as " + UGSInstance.USERNAME + ", Auth Token: " + UGSInstance.TOKEN);
        UGSInstance.Achieve("achieve-uuid", 1, OnAchieveSuccess, OnAchieveFailed);
    }

    public void OnLoginFailed(string error)
    {
        Debug.Log("Failed to login with error " + error);
    }

    private void OnAchieveSuccess(string details)
    {
        Debug.Log("Successfully Achieved: " + details);
    }

    private void OnAchieveFailed(string error)
    {
        Debug.Log("Failed to Achieve: " + error);
    }
}
