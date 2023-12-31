using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using TMPro;
using UnityEngine.SceneManagement;

public class FireBaseManager : MonoBehaviour
{
    public static FireBaseManager instance;

    [Header("Firebase")]
    public FirebaseAuth auth;
    public FirebaseUser user;

    [Space(5f)]
    [Header("Login References")]
    [SerializeField]
    private TMP_InputField loginEmail;

    [SerializeField]
    private TMP_InputField loginPassword;

    [SerializeField]
    private TMP_Text loginOutputText;

    [Space(5f)]
    [Header("Register References")]
    [SerializeField]
    private TMP_InputField registerUsername;

    [SerializeField]
    private TMP_InputField registerEmail;

    [SerializeField]
    private TMP_InputField registerPassword;

    [SerializeField]
    private TMP_InputField registerConfirmPassword;

    [SerializeField]
    private TMP_Text registerOutputText;

    [Space(5f)]
    [Header("Password Reset References")]
    [SerializeField]
    private TMP_InputField resetEmail;

    [SerializeField]
    private TMP_Text resetOutputText;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(instance.gameObject);
            instance = this;
        }
    }

    private void Start()
    {
        StartCoroutine(CheckAndFixDependencies());
    }

    private IEnumerator CheckAndFixDependencies()
    {
        var checkAndFixDependenciesTask = FirebaseApp.CheckAndFixDependenciesAsync();

        yield return new WaitUntil(predicate: () => checkAndFixDependenciesTask.IsCompleted);

        var dependencyResult = checkAndFixDependenciesTask.Result;

        if (dependencyResult == DependencyStatus.Available)
        {
            InitializeFirebase();
        }
        else
        {
            UnityEngine.Debug.LogError(
                $"Could not resolve all Firebase dependencies: {dependencyResult}"
            );
        }
    }

    private void InitializeFirebase()
    {
        auth = FirebaseAuth.DefaultInstance;
        StartCoroutine(CheckAutoLogin());

        auth.StateChanged += AuthStateChanged;
        AuthStateChanged(this, null);
    }

    private IEnumerator CheckAutoLogin()
    {
        yield return new WaitForEndOfFrame();
        if (user != null)
        {
            var reloadUserTask = user.ReloadAsync();

            yield return new WaitUntil(predicate: () => reloadUserTask.IsCompleted);

            if (user.IsEmailVerified)
            {
                AutoLogin();
            }
            else
            {
                loginOutputText.text = "Por favor, verifica tu correo electrónico.";
            }
        }
        else
        {
            AuthUIManager.instance.BackToMainScreen();
        }
    }

    private void AutoLogin()
    {
        if (user != null)
        {
            GameManager.instance.ChangeScene(2);
        }
        else
        {
            AuthUIManager.instance.BackToMainScreen();
        }
    }

    private void AuthStateChanged(object sender, System.EventArgs eventArgs)
    {
        if (auth.CurrentUser != user)
        {
            bool signedIn = user != auth.CurrentUser && auth.CurrentUser != null;

            if (!signedIn && user != null)
            {
                UnityEngine.Debug.Log("Signed Out");
                GameManager.instance.ChangeScene(1);
                AuthUIManager.instance.BackToMainScreen();
            }

            user = auth.CurrentUser;

            if (signedIn)
            {
                UnityEngine.Debug.Log($"Signed In: {user.DisplayName}");
            }
        }
    }

    public void ClearOutputs()
    {
        loginOutputText.text = "";
        registerOutputText.text = "";
    }

    public void LoginButton()
    {
        StartCoroutine(LoginLogic(loginEmail.text, loginPassword.text));
    }

    public void RegisterButton()
    {
        StartCoroutine(
            RegisterLogic(
                registerUsername.text,
                registerEmail.text,
                registerPassword.text,
                registerConfirmPassword.text
            )
        );
    }

    private IEnumerator LoginLogic(string _email, string _password)
    {
        var loginTask = auth.SignInWithEmailAndPasswordAsync(_email, _password);

        yield return new WaitUntil(() => loginTask.IsCompleted);

        if (loginTask.Exception != null)
        {
            FirebaseException firebaseException = (FirebaseException)
                loginTask.Exception.GetBaseException();
            AuthError error = (AuthError)firebaseException.ErrorCode;
            string output;

            switch (error)
            {
                case AuthError.MissingEmail:
                    output = "Por favor ingrese su email.";
                    break;
                case AuthError.MissingPassword:
                    output = "Por favor ingrese su contraseña.";
                    break;
                case AuthError.InvalidEmail:
                    output = "Email inválido.";
                    break;
                case AuthError.WrongPassword:
                    output = "Contraseña inválida.";
                    break;
                case AuthError.UserNotFound:
                    output = "Usuario no encontrado.";
                    break;
                default:
                    output = "Correo y/o contraseña inválida.";
                    break;
            }

            loginOutputText.text = output;
        }
        else
        {
            if (user.IsEmailVerified)
            {
                SceneManager.LoadScene(2);
            }
            else
            {
                loginOutputText.text = "Por favor, verifica tu correo electrónico.";
            }
        }
    }

    private IEnumerator RegisterLogic(
        string _username,
        string _email,
        string _password,
        string _confirmPassword
    )
    {
        if (_username == "")
        {
            registerOutputText.text = "Por favor ingrese su nombre.";
        }
        else if (_username.Length > 15)
        {
            registerOutputText.text = "El nombre no debe tener más de 15 caracteres.";
        }
        else if (ContainsNumbers(_username))
        {
            registerOutputText.text = "El nombre no debe contener números.";
        }
        else if (_password != _confirmPassword)
        {
            registerOutputText.text = "Las contraseñas no coinciden.";
        }
        else
        {
            var registerTask = auth.CreateUserWithEmailAndPasswordAsync(_email, _password);

            yield return new WaitUntil(predicate: () => registerTask.IsCompleted);

            if (registerTask.Exception != null)
            {
                FirebaseException firebaseException = (FirebaseException)
                    registerTask.Exception.GetBaseException();
                AuthError error = (AuthError)firebaseException.ErrorCode;
                string output = "Error desconocido, intente nuevamente.";

                switch (error)
                {
                    case AuthError.InvalidEmail:
                        output = "Email inválido.";
                        break;
                    case AuthError.EmailAlreadyInUse:
                        output = "El email ingresado ya está en uso.";
                        break;
                    case AuthError.WeakPassword:
                        output = "Contraseña débil.";
                        break;
                    case AuthError.MissingEmail:
                        output = "Por favor ingrese su email.";
                        break;
                    case AuthError.MissingPassword:
                        output = "Por favor ingrese una contraseña.";
                        break;
                }
                registerOutputText.text = output;
            }
            else
            {
                UserProfile profile = new UserProfile { DisplayName = _username, };

                var defaultUserTask = user.UpdateUserProfileAsync(profile);

                yield return new WaitUntil(predicate: () => defaultUserTask.IsCompleted);

                if (defaultUserTask.Exception != null)
                {
                    user.DeleteAsync();
                    FirebaseException firebaseException = (FirebaseException)
                        defaultUserTask.Exception.GetBaseException();
                    AuthError error = (AuthError)firebaseException.ErrorCode;
                    string output = "Error desconocido, intente nuevamente.";

                    switch (error)
                    {
                        case AuthError.Cancelled:
                            output = "Operación cancelada.";
                            break;
                        case AuthError.SessionExpired:
                            output = "La sesión ha culminado.";
                            break;
                    }
                    registerOutputText.text = output;
                }
                else
                {
                    UnityEngine.Debug.Log(
                        $"Firebase User Created Succesfully: {user.DisplayName} ({user.UserId})"
                    );

                    // Envía el correo de verificación
                    var emailTask = user.SendEmailVerificationAsync();
                    yield return new WaitUntil(predicate: () => emailTask.IsCompleted);

                    if (emailTask.Exception != null)
                    {
                        // Maneja los errores
                    }
                    else
                    {
                        AuthUIManager.instance.EmailScreen(); // Dirige al usuario a la pantalla de verificación
                    }
                }
            }
        }
    }

    private bool ContainsNumbers(string input)
    {
        foreach (char c in input)
        {
            if (char.IsDigit(c))
            {
                return true;
            }
        }
        return false;
    }

    public void ResetPasswordButton()
    {
        StartCoroutine(ResetPasswordLogic(resetEmail.text));
    }

    private IEnumerator ResetPasswordLogic(string _email)
    {
        var resetTask = auth.SendPasswordResetEmailAsync(_email);

        yield return new WaitUntil(predicate: () => resetTask.IsCompleted);

        if (resetTask.Exception != null)
        {
            FirebaseException firebaseException = (FirebaseException)
                resetTask.Exception.GetBaseException();
            AuthError error = (AuthError)firebaseException.ErrorCode;
            string output = "Error desconocido, intente nuevamente.";

            switch (error)
            {
                case AuthError.InvalidEmail:
                    output = "Email inválido.";
                    break;
                case AuthError.UserNotFound:
                    output = "Usuario no encontrado.";
                    break;
            }
            resetOutputText.text = output;
        }
        else
        {
            resetOutputText.text = "Se ha enviado un correo para restablecer la contraseña.";
        }
    }

    public void SignOut()
    {
        auth.SignOut();
    }

    public string GetUsername()
    {
        return user != null ? user.DisplayName : "";
    }
}
