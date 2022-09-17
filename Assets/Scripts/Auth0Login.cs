using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;


public class Auth0Login : MonoBehaviour
{
    private static string AUTH0_AUTH_HOST = "";
    private static string AUTH0_AUDIENCE = "";
    private static string AUTH0_CLIENT_ID = "";


    private string accessToken;

    private class OauthDeviceCodeResponse
    {
        public string verification_uri_complete;
        public string user_code;
        public string device_code;
    }

    private class OauthAccessTokenResponse
    {
        public string access_token;
    }

    private class OauthAccessTokenErrorResponse
    {
        public string error;
        public string error_description;
    }

    private class OauthUserInfoResponse
    {
        public string sub;
        public string name;
        public string nickname;
        public string picture;
        public string updated_at;
    }

    [SerializeField]
    private TextMeshProUGUI deviceCodeDisplay;
    [SerializeField]
    private TextMeshProUGUI userInfoDisplay;

    public void onClick()
    {
        StartCoroutine(LoginHandler());
    }

    private IEnumerator LoginHandler()
    {
        yield return GenerateOneTimeDeviceCode();
    }

    // デバイス認証用URLの発行
    private IEnumerator GenerateOneTimeDeviceCode()
    {
        WWWForm generateOneTimeDeviceCodeForm = new WWWForm();
        generateOneTimeDeviceCodeForm.AddField("client_id", AUTH0_CLIENT_ID);
        generateOneTimeDeviceCodeForm.AddField("scope", "openid profile email");
        generateOneTimeDeviceCodeForm.AddField("audience", AUTH0_AUDIENCE);
        using (UnityWebRequest request = UnityWebRequest.Post(AUTH0_AUTH_HOST + "/oauth/device/code", generateOneTimeDeviceCodeForm))
        {
            yield return request.SendWebRequest();
            if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError) {
                Debug.Log(request.error);
                yield return null;
            }
            else
            {
                OauthDeviceCodeResponse response = JsonUtility.FromJson<OauthDeviceCodeResponse>(request.downloadHandler.text);
                Application.OpenURL(response.verification_uri_complete);
                deviceCodeDisplay.text = response.user_code;
                yield return GetAccessToken(response.device_code);
            }
        }
    }

    // デバイス認証後に取得できるアクセストークンの取得
    // ユーザーが認証するまで無限ループします。。
    private IEnumerator GetAccessToken(string deviceCode)
    {
        WWWForm getAccessTokenForOneTimeDeviceCodeForm = new WWWForm();
        getAccessTokenForOneTimeDeviceCodeForm.AddField("client_id", AUTH0_CLIENT_ID);
        getAccessTokenForOneTimeDeviceCodeForm.AddField("grant_type", "urn:ietf:params:oauth:grant-type:device_code");
        getAccessTokenForOneTimeDeviceCodeForm.AddField("device_code", deviceCode);
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Post(AUTH0_AUTH_HOST + "/oauth/token", getAccessTokenForOneTimeDeviceCodeForm))
            {
                yield return request.SendWebRequest();
                if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError) {
                    OauthAccessTokenErrorResponse errorResponse = JsonUtility.FromJson<OauthAccessTokenErrorResponse>(request.downloadHandler.text);
                    Debug.Log(errorResponse.error);
                    Debug.Log(errorResponse.error_description);
                    if (errorResponse.error != "authorization_pending")
                    {
                        Debug.Log("Authorization is canceled.");
                        break;
                    }
                    int WAIT_SECONDS = 5;
                    Debug.Log("Retry GetAccessToken request " + WAIT_SECONDS + " seconds later...");
                    yield return new WaitForSeconds(WAIT_SECONDS);
                }
                else
                {
                    OauthAccessTokenResponse response = JsonUtility.FromJson<OauthAccessTokenResponse>(request.downloadHandler.text);
                    this.accessToken = response.access_token;
                    yield return GetUserInfo(this.accessToken);
                    break;
                }
            }
        }
    }

    // アクセストークンを用いてUserInfoを取得
    public IEnumerator GetUserInfo(string accessToken)
    {
        using (UnityWebRequest request = UnityWebRequest.Get(AUTH0_AUTH_HOST + "/userinfo"))
        {
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
            yield return request.SendWebRequest();
            if(request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.ConnectionError) {
                Debug.Log(request.error);
            }
            else
            {
                //Dictionary<string, object> response = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                OauthUserInfoResponse response = JsonUtility.FromJson<OauthUserInfoResponse>(request.downloadHandler.text);
                string result = "sub: " + response.sub + "\n"
                        + "name: " + response.name + "\n"
                        + "nickname: " + response.nickname + "\n"
                        + "picture: " + response.picture + "\n"
                        + "updated_at: " + response.updated_at;
                userInfoDisplay.text = result;
            }
        }
    }
}

