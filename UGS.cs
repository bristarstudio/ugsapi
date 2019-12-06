using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class UGS : MonoBehaviour
{
    [System.Serializable]
    public class UGSServertime
    {
        public string now;
        public int tik;
    }

    [System.Serializable]
    public class UGSContainerMaintenance
    {
        [System.Serializable]
        public class UGSPayloadMaintenance
        {
            [System.Serializable]
            public class UGSValueMaintenance
            {
                public int duration;
                public int start_date;
            }
            public int type;
            public UGSValueMaintenance value;
        }
        public string uuid;
        public UGSPayloadMaintenance payload;
    }

    [System.Serializable]
    public class UGSResponseToken
    {
        public string token;
        public bool is_staff;
    }

    [System.Serializable]
    public class UGSResponseAchieve
    {
        public string detail;
        public string error;
    }

    [System.Serializable]
    public class UGSResponseError
    {
        public string detail;
        public string[] non_field_errors;
        public string email;
        public string username;
        public string password1;
    }

    bool _maintenanceChecked = false;

    public delegate void OnLoginSuccess();
    public delegate void OnLoginFailed(string error);
    public delegate void OnAchieveSuccess(string details);
    public delegate void OnAchieveFailed(string error);
    private OnLoginSuccess _loginSuccess = null;
    private OnLoginFailed _loginFailed = null;
    private OnAchieveSuccess _achieveSuccess = null;
    private OnAchieveFailed _achieveFailed = null;

    public string USERNAME;
    public string TOKEN;
    public DateTime ServerTime;

    private int ServerTimeTik;
    private DateTime ServertimeRequestTime;
    private DateTime MaintenanceStart;
    private DateTime MaintenanceEnd;


    private string _password;

    private delegate void WebRequestCallback(DownloadHandler response);

    public void Login(string iUsername, string iPassword, OnLoginSuccess iLoginSuccess, OnLoginFailed iLoginFailed)
    {
        USERNAME = iUsername;
        TOKEN = null;
        _loginSuccess = iLoginSuccess;
        _loginFailed = iLoginFailed;
        _password = iPassword;
        if (_maintenanceChecked)
        {
            DoLogin();
        }
        else
        {
            CheckMaintenance();
        }
    }

    public void Logout()
    {
        USERNAME = null;
        TOKEN = null;
    }

    public void Achieve(string uuid, int amount, OnAchieveSuccess iAchieveSuccess, OnAchieveFailed iAchieveFailed)
    {
        if (TOKEN == null)
        {
            iAchieveFailed("authorization_required");
        }
        else
        {
            if (isMaintenanceGoing)
            {
                _achieveFailed?.Invoke("maintenance_going");
            }
            else
            {
                _achieveSuccess = iAchieveSuccess;
                _achieveFailed = iAchieveFailed;
                WWWForm form = new WWWForm();
                form.AddField("uuid", uuid);
                form.AddField("amount", amount);
                StartCoroutine(PostRequest("https://ugs3experiment.universinet.org/scripts/bd14b1a4d7744a92af73bb8b70c2cf07/run/", form, AchieveDone));
            }
        }
    }

    private void AchieveDone(DownloadHandler response)
    {
        UGSResponseAchieve achieve = JsonUtility.FromJson<UGSResponseAchieve>(response.text);
        if (achieve.error == null)
        {
            _achieveSuccess?.Invoke(achieve.detail);
        }
        else
        {
            _achieveFailed?.Invoke(achieve.error);
        }
    }

    private void DoLogin()
    {
        WWWForm form = new WWWForm();
        form.AddField("username", USERNAME);
        form.AddField("password", _password);
        StartCoroutine(PostRequest("https://ugs3experiment.universinet.org/auth/token/obtain/", form, ObtainToken));
    }

    private void ObtainToken(DownloadHandler response)
    {
        UGSResponseToken tr = JsonUtility.FromJson<UGSResponseToken>(response.text);
        // Debug.Log("Token Obtained: " + tr.token + ", " + tr.is_staff);
        if (tr.is_staff)
        {
            _loginFailed?.Invoke("unable_to_login_as_staff");
        }
        else
        {
            TOKEN = tr.token;
            _loginSuccess?.Invoke();
        }
    }

    private void CheckMaintenance()
    {
        ServertimeRequestTime = DateTime.Now;
        StartCoroutine(GetRequest("https://ugs3experiment.universinet.org/servertime/", ServertimeLoaded));
    }

    private void ServertimeLoaded(DownloadHandler response)
    {
        UGSServertime ServertimeObj = JsonUtility.FromJson<UGSServertime>(response.text);
        ServerTimeTik = ServertimeObj.tik;
        StartCoroutine(GetRequest("https://ugs3experiment.universinet.org/containers/47103b5babcc4ed5b9c2b73bb704bd86/", MaintenanceLoaded));
    }

    private void UpdateServertime()
    {
        TimeSpan sinseRequest = DateTime.Now - ServertimeRequestTime;
        ServerTime = UnixTimeStampToDateTime(ServerTimeTik) + sinseRequest;
    }
    
    private bool isMaintenanceGoing
    {
        get
        {
            UpdateServertime();
            if (ServerTime > MaintenanceStart && ServerTime < MaintenanceEnd)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    private void MaintenanceLoaded(DownloadHandler response)
    {
        UGSContainerMaintenance mc = JsonUtility.FromJson<UGSContainerMaintenance>(response.text);
        MaintenanceStart = UnixTimeStampToDateTime(mc.payload.value.start_date);
        MaintenanceEnd = MaintenanceStart.AddSeconds(mc.payload.value.duration);
        // Debug.Log("Maintenance loaded. Duration: " + mc.payload.value.duration + ", StartDate: " + MaintenanceStart + ", EndDate: " + MaintenanceEnd);
        if (isMaintenanceGoing)
        {
            if (_loginFailed != null)
            {
                _loginFailed("maintenance_going");
                _loginFailed = null;
            }
            if (_achieveFailed != null)
            {
                _achieveFailed("maintenance_going");
                _achieveFailed = null;
            }
        }
        else
        {
            _maintenanceChecked = true;
            DoLogin();
        }
    }

    private IEnumerator GetRequest(string uri, WebRequestCallback callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            if (TOKEN != null)
            {
                webRequest.SetRequestHeader("Authorization", "JWT " + TOKEN);
            }
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                if (_loginFailed != null)
                {
                    _loginFailed("network_error");
                    _loginFailed = null;
                }
                if (_achieveFailed != null)
                {
                    _achieveFailed("network_error");
                    _achieveFailed = null;
                }
            }
            else
            {
                if (webRequest.isHttpError)
                {
                    ServerError(webRequest.downloadHandler.text);
                }
                else
                {
                    callback(webRequest.downloadHandler);
                }
            }
        }
    }
    private IEnumerator PostRequest(string uri, WWWForm form, WebRequestCallback callback)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Post(uri, form))
        {
            if (TOKEN != null)
            {
                webRequest.SetRequestHeader("Authorization", "JWT " + TOKEN);
            }
            yield return webRequest.SendWebRequest();

            if (webRequest.isNetworkError)
            {
                if (_loginFailed != null)
                {
                    _loginFailed("network_error");
                    _loginFailed = null;
                }
                if (_achieveFailed != null)
                {
                    _achieveFailed("network_error");
                    _achieveFailed = null;
                }
            }
            else
            {
                if (webRequest.isHttpError)
                {
                    ServerError(webRequest.downloadHandler.text);
                }
                else
                {
                    callback(webRequest.downloadHandler);
                }
            }
        }
    }

    private IEnumerator ReloginAfterThrottling(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        DoLogin();
    }

    private void ServerError(string response)
    {
        UGSResponseError re = JsonUtility.FromJson<UGSResponseError>(response);
        string errorMessage = "";
        if (re.detail != null)
        {
            errorMessage = re.detail;
        }
        if (re.non_field_errors.Length > 0)
        {
            errorMessage = re.non_field_errors[0];
        }
        if (re.email != null)
        {
            errorMessage = re.email;
        }
        if (re.username != null)
        {
            errorMessage = re.username;
        }
        if (re.password1 != null)
        {
            errorMessage = re.password1;
        }

        if (errorMessage.Contains("throttled"))
        {
            Regex regex = new Regex(@"available in (\d*\.?\d*) second");
            Match match = regex.Match(errorMessage);
            if (match.Groups.Count == 2)
            {
                int secondsToReload = System.Convert.ToInt32(System.Math.Ceiling(System.Convert.ToDecimal(match.Groups[1].Value)));
                if (secondsToReload > 0)
                {
                    // Debug.Log("Reloading After Throttling in " + secondsToReload + " seconds");
                    StartCoroutine(ReloginAfterThrottling(secondsToReload));
                }
            }
        }
        else
        {
            errorMessage = TransformServerError(errorMessage);
            if (_loginFailed != null)
            {
                _loginFailed(errorMessage);
                _loginFailed = null;
            }
            if (_achieveFailed != null)
            {
                _achieveFailed(errorMessage);
                _achieveFailed = null;
            }
        }
    }

    private string TransformServerError(string error)
    {
        switch (error)
        {
            case "Request was throttled. Expected available in 1.0 second.":
                return "rest_throttled";
            case "Unable to login with provided credentials.":
                return "wrong_credentials";
            case "This username is already taken. Please choose another.":
                return "rest_username_exists";
            case "Not found.":
                return "rest_not_found";
            case "Enter a valid email address.":
                return "rest_invalid_email";
            case "A user is already registered with this e-mail address.":
                return "rest_email_exists";
            case "The password is too similar to the username.":
                return "rest_password_similar_to_username";
            case "The password is too similar to the email.":
                return "rest_password_similar_to_email";
            case "Password must be a minimum of 6 characters.":
                return "rest_password_too_short";
            case "This password is too common.":
                return "rest_password_too_common";
            case "You do not have permission to perform this action.":
                return "rest_no_permission";
            case "Usernames can only contain letters, digits and @/./+/-/_.":
                return "wrong_username";
            case "Request failed with status code 500.":
                return "rest_500";
            case "This field may not be blank.":
                return "username_too_short";
            case "Request failed with status code 400":
                return "rest_500";
            case "User doesn\"t exist.":
                return "user_not_exists";
            case "Error decoding signature.":
            case "Signature has expired.":
                if (error == "Error decoding signature.")
                {
                    error = "error_decoding_signature";
                }
                else
                {
                    error = "token_expired";
                }
                return error;
            case "Network Error":
                return "network_error";
            default:
                return error;
        }
    }

    public static DateTime UnixTimeStampToDateTime(int unixTimeStamp, DateTimeKind dateTimeType = DateTimeKind.Unspecified)
    {
        // Unix timestamp is seconds sinse epoch
        DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        switch (dateTimeType)
        {
            case DateTimeKind.Local:
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
                break;
            case DateTimeKind.Utc:
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToUniversalTime();
                break;
            case DateTimeKind.Unspecified:
                dtDateTime = dtDateTime.AddSeconds(unixTimeStamp);
                break;
        }
        return dtDateTime;
    }
    public static int DateTimeToUnixTimeStamp(DateTime date)
    {
        return (int)(date - new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
    }
}
