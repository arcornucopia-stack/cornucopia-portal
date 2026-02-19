using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using EasyUI.Dialogs;
using Firebase.Database;
using System.IO;
using Cornucopia.Core.Models;

public class FirebaseAuthManager : MonoBehaviour
{
    // Firebase variable
    [Header("Firebase")]
    public DependencyStatus dependencyStatus;
    public FirebaseAuth auth;
    public FirebaseUser user;

    // Login Variables
    [Space]
    [Header("Login")]
    public InputField emailLoginField;
    public InputField passwordLoginField;

    // Registration Variables
    [Space]
    [Header("Registration")]
    public InputField nameRegisterField;
    public InputField emailRegisterField;
    public InputField passwordRegisterField;
    public InputField confirmPasswordRegisterField;

    private void Awake()
    {
        if (!Directory.Exists($"{Application.persistentDataPath}/Files"))
        {
            Directory.CreateDirectory($"{Application.persistentDataPath}/Files");
            Directory.CreateDirectory($"{Application.persistentDataPath}/Files/models");
        }
        if (PlayerPrefs.GetInt("login")==1)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
            return;
        }

        // Check that all of the necessary dependencies for firebase are present on the system
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            dependencyStatus = task.Result;

            if (dependencyStatus == DependencyStatus.Available)
            {
                InitializeFirebase();
            }
            else
            {
                Debug.LogError("Could not resolve all firebase dependencies: " + dependencyStatus);
            }
        });
    }

    void InitializeFirebase()
    {
        //Set the default instance object
        auth = FirebaseAuth.DefaultInstance;

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    // Track state changes of the auth object.
    void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                Debug.Log("Signed out " + user.UserId);
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                Debug.Log("Signed in " + user.UserId);
            }
        }
    }

    public void Login()
    {
        StartCoroutine(LoginAsync(emailLoginField.text, passwordLoginField.text));
    }

    public void ForgotPassword()
    {
        StartCoroutine(ForgotPasswordAsync(emailLoginField.text));
    }

    private IEnumerator ForgotPasswordAsync(string email)
    {
        if (auth == null)
        {
            DialogUI.Instance
                .SetTitle("Error")
                .SetMessage("Authentication service is not ready. Please try again.")
                .SetButtonColor(DialogButtonColor.Black)
                .Show();
            yield break;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            DialogUI.Instance
                .SetTitle("Error")
                .SetMessage("Please enter your email first.")
                .SetButtonColor(DialogButtonColor.Black)
                .Show();
            yield break;
        }

        var resetTask = auth.SendPasswordResetEmailAsync(email.Trim());
        yield return new WaitUntil(() => resetTask.IsCompleted);

        if (resetTask.Exception != null)
        {
            FirebaseException firebaseException = resetTask.Exception.GetBaseException() as FirebaseException;
            AuthError? authError = firebaseException != null ? (AuthError)firebaseException.ErrorCode : (AuthError?)null;
            Debug.LogWarning(firebaseException != null ? firebaseException.Message : "Password reset failed.");

            string failedMessage = "Could not send reset email.";
            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage = "Email is invalid.";
                    break;
                case AuthError.MissingEmail:
                    failedMessage = "Email is missing.";
                    break;
                case AuthError.UserNotFound:
                    failedMessage = "No account exists with this email.";
                    break;
            }

            DialogUI.Instance
                .SetTitle("Error")
                .SetMessage(failedMessage)
                .SetButtonColor(DialogButtonColor.Black)
                .Show();
        }
        else
        {
            DialogUI.Instance
                .SetTitle("Reset Email Sent")
                .SetMessage("Check your inbox for password reset instructions.")
                .SetButtonColor(DialogButtonColor.Black)
                .Show();
        }
    }

    private IEnumerator LoginAsync(string email, string password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(email, password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            FirebaseException firebaseException = loginTask.Exception.GetBaseException() as FirebaseException;
            AuthError? authError = firebaseException != null ? (AuthError)firebaseException.ErrorCode : (AuthError?)null;
            Debug.LogWarning(firebaseException != null ? firebaseException.Message : "Login failed.");


            string failedMessage = "Login Failed! Because ";

            switch (authError)
            {
                case AuthError.InvalidEmail:
                    failedMessage += "Email is invalid";
                    break;
                case AuthError.WrongPassword:
                    failedMessage += "Wrong Password";
                    break;
                case AuthError.MissingEmail:
                    failedMessage += "Email is missing";
                    break;
                case AuthError.MissingPassword:
                    failedMessage += "Password is missing";
                    break;
                case AuthError.UserNotFound:
                    failedMessage += "No account exists with this email";
                    break;
                default:
                    failedMessage = "Login Failed";
                    break;
            }
            DialogUI.Instance
        .SetTitle("Error")
        .SetMessage(failedMessage)
        .SetButtonColor(DialogButtonColor.Black)
        .OnClose(() => Debug.Log("Closed 1"))
        .Show();
            Debug.Log(failedMessage);
        }
        else
        {
            user = loginTask.Result;

            Debug.LogFormat("{0} You Are Successfully Logged In", user.DisplayName);
            PlayerPrefs.SetString("userEmail", email);
            PlayerPrefs.SetString("userName", user.DisplayName);
            PlayerPrefs.SetString("userId", user.UserId);
            PlayerPrefs.SetInt("login", 1);
            References.userName = user.DisplayName;
            UnityEngine.SceneManagement.SceneManager.LoadScene("Home");
        }
    }

    public void Register()
    {
        StartCoroutine(RegisterAsync(nameRegisterField.text, emailRegisterField.text, passwordRegisterField.text, confirmPasswordRegisterField.text));
    }

    private IEnumerator RegisterAsync(string name, string email, string password, string confirmPassword)
    {
        if (name == "")
        {
            Debug.LogError("User Name is empty");
            DialogUI.Instance
       .SetTitle("Error")
       .SetMessage("User Name is empty")
       .SetButtonColor(DialogButtonColor.Black)
       .OnClose(() => Debug.Log("Closed 1"))
       .Show();
        }
        else if (email == "")
        {
            Debug.LogError("Email field is empty");
            DialogUI.Instance
      .SetTitle("Error")
      .SetMessage("Email field is empty")
      .SetButtonColor(DialogButtonColor.Black)
      .OnClose(() => Debug.Log("Closed 1"))
      .Show();
        }
        else if (passwordRegisterField.text != confirmPasswordRegisterField.text)
        {
            Debug.LogError("Password does not match");
            DialogUI.Instance
      .SetTitle("Error")
      .SetMessage("Password does not match")
      .SetButtonColor(DialogButtonColor.Black)
      .OnClose(() => Debug.Log("Closed 1"))
      .Show();
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(email, password);

            yield return new WaitUntil(() => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                Debug.LogError(registerTask.Exception);

                FirebaseException firebaseException = registerTask.Exception.GetBaseException() as FirebaseException;
                AuthError authError = (AuthError)firebaseException.ErrorCode;

                string failedMessage = "Registration Failed! Becuase ";
                switch (authError)
                {
                    case AuthError.InvalidEmail:
                        failedMessage += "Email is invalid";
                        break;
                    case AuthError.WrongPassword:
                        failedMessage += "Wrong Password";
                        break;
                    case AuthError.MissingEmail:
                        failedMessage += "Email is missing";
                        break;
                    case AuthError.MissingPassword:
                        failedMessage += "Password is missing";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        failedMessage += "Email already in use";
                        break;
                    default:
                        failedMessage = "Registration Failed";
                        break;
                }
                DialogUI.Instance
        .SetTitle("Error")
        .SetMessage(failedMessage)
        .SetButtonColor(DialogButtonColor.Black)
        .OnClose(() => Debug.Log("Closed 1"))
        .Show();
                
                Debug.Log(failedMessage);
            }
            else
            {
                // Get The User After Registration Success
                user = registerTask.Result;

                UserProfile userProfile = new UserProfile { DisplayName = name };

                var updateProfileTask = user.UpdateUserProfileAsync(userProfile);

                yield return new WaitUntil(() => updateProfileTask.IsCompleted);

                if (updateProfileTask.Exception != null)
                {
                    // Delete the user if user update failed
                    user.DeleteAsync();

                    Debug.LogError(updateProfileTask.Exception);

                    FirebaseException firebaseException = updateProfileTask.Exception.GetBaseException() as FirebaseException;
                    AuthError authError = (AuthError)firebaseException.ErrorCode;


                    string failedMessage = "Profile update Failed! Becuase ";
                    switch (authError)
                    {
                        case AuthError.InvalidEmail:
                            failedMessage += "Email is invalid";
                            break;
                        case AuthError.WrongPassword:
                            failedMessage += "Wrong Password";
                            break;
                        case AuthError.MissingEmail:
                            failedMessage += "Email is missing";
                            break;
                        case AuthError.MissingPassword:
                            failedMessage += "Password is missing";
                            break;
                        default:
                            failedMessage = "Profile update Failed";
                            break;
                    }
                    DialogUI.Instance
        .SetTitle("Error")
        .SetMessage(failedMessage)
        .SetButtonColor(DialogButtonColor.Black)
        .OnClose(() => Debug.Log("Closed 1"))
        .Show();
                   
                    Debug.Log(failedMessage);

                }
                else
                {
                    Debug.Log("Registration Sucessful Welcome " + user.DisplayName);
                    DatabaseReference reference = FirebaseDatabase.DefaultInstance.RootReference;
                    LegacyUserDetails m = new LegacyUserDetails(name,email, user.UserId);                   
                    string json = JsonUtility.ToJson(m);
                    Debug.Log(json + "  "+ email);
                    var task = reference.Child("cornucopia").Child("users").Child(user.UserId).SetRawJsonValueAsync(json);
                    yield return new WaitUntil(() => task.IsCompleted);
                    LegacyUserData d = new LegacyUserData(0, 0, 0);
                    string json2 = JsonUtility.ToJson(d);
                    Debug.Log(json2 );
                    var task2 = reference.Child("cornucopia").Child("users").Child(user.UserId).Child("userData").SetRawJsonValueAsync(json2);
                    yield return new WaitUntil(() => task2.IsCompleted);
                    if (task.Exception == null && task2.Exception==null)
                    {
                        
                        LoginUIManager.Instance.OpenLoginPanel();
                    }
                    else
                        Debug.Log(task.Exception);
                }
            }
        }
    }
}
