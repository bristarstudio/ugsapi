# UGSAPI

UGS API for external Unity Developers

# Initialize

1. Create new Game Object on scene
2. Add public UGS UGSInctance property to your main class
3. Set GameObject to UGSInctance property

# Methods

Login(string username, string password "passwd123", OnLoginSuccess, OnLoginFailed)

Achieve(string achieveUUID, int amount, OnAchieveSuccess, OnAchieveFailed);

# Usage

First, you need to login user and obtain authorization token. Token is Public and stored in the UGS object.
If you user are looged in - he can make an Achieve.
When your application will registered in our Banner system, we will give you some achievement uuids.
Use that uuids to make achievements in our billing system

# Examples

Check Main.cs to see, how it works.

```
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
```

# Errors

**OnLoginFailed** and **OnAchieveFailed** always returns one of error string below.

**wrong_credentials**
Unable to login with provided credentials.

**maintenance_going**
Maintenance is going

**rest_throttled**
Request was throttled. Expected available in 1.0 second.

**rest_500**
Request failed with status code 500.

**user_not_exists**
User doesn't exist.

**wrong_username**
Usernames can only contain letters, digits and @/./+/-/_.

**username_too_short**
Provided username is too short

**token_expired**
Signature has expired.

**error_decoding_signature**
Error decoding signature.

**network_error**
Network Error

