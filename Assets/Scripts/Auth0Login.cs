using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using MiniJSON;
using TMPro;

public class Auth0Login : MonoBehaviour
{
    private static string AUTH0_AUTH_HOST = "";
    private static string AUTH0_AUDIENCE = "";
    private static string AUTH0_CLIENT_ID = "";

    private string accessToken;
    private object userInfo;

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
                Dictionary<string, object> response = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                Application.OpenURL(response["verification_uri_complete"] as string);
                deviceCodeDisplay.text = response["user_code"] as string;
                yield return GetAccessTokenForOneTimeDeviceCode(response["device_code"] as string);
            }
        }
    }

    // デバイス認証後に取得できるアクセストークンの取得
    // ユーザーが認証するまで無限ループします。。
    private IEnumerator GetAccessTokenForOneTimeDeviceCode(string deviceCode)
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
                    Debug.Log(request.error);
                    int WAIT_SECONDS = 5;
                    Debug.Log("Retry request " + WAIT_SECONDS + " seconds later...");
                    yield return new WaitForSeconds(WAIT_SECONDS);
                }
                else
                {
                    Dictionary<string, object> response = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                    Debug.Log("access_token: " + response["access_token"]);
                    this.accessToken = response["access_token"] as string;
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
                Dictionary<string, object> response = Json.Deserialize(request.downloadHandler.text) as Dictionary<string, object>;
                string result = "sub: " + response["sub"] + "\n"
                        + "name: " + response["name"] + "\n"
                        + "nickname: " + response["nickname"] + "\n"
                        + "picture: " + response["picture"] + "\n"
                        + "updated_at: " + response["updated_at"];
                Debug.Log(result);
                this.userInfo = response;
                userInfoDisplay.text = result;
            }
        }
    }
}

