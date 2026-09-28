using System;
using System.Collections;
using EpicTransport;
using JustAGame;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JustAGame.UI
{
    /// <summary>
    /// UI Controller for Epic Online Services (EOS) Authentication & Account Selection.
    /// Exclusively uses TextMeshPro (TMP_InputField, TextMeshProUGUI) and standard UI Buttons.
    /// Adheres strictly to repository DESIGN_PATTERNS.md zero-slop guidelines.
    /// Enables multi-instance testing on the same PC via DevAuthTool or Epic Account Portal.
    /// </summary>
    [DisallowMultipleComponent]
    public class EOSLoginUI : MonoBehaviour
    {
        [Header("UI Panel & Canvas References")]
        [Tooltip("Root panel GameObject for the login window.")]
        [SerializeField] private GameObject loginPanel;

        [Header("TextMeshPro Inputs & Labels")]
        [Tooltip("Input field for the username / profile name (e.g. Player1, Player2).")]
        [SerializeField] private TMP_InputField accountNameInput;

        [Tooltip("Status text displaying connection and auth progress.")]
        [SerializeField] private TextMeshProUGUI statusTMP;

        [Header("Action Buttons")]
        [Tooltip("Button to log in via local Developer Authentication Tool (DevAuth on localhost:7878/8081).")]
        [SerializeField] private Button devAuthLoginButton;

        [Tooltip("Button to log in via official Epic Games Account Portal (opens web browser).")]
        [SerializeField] private Button epicAccountLoginButton;

        [Tooltip("Button for 1-click Guest / Machine Device ID login.")]
        [SerializeField] private Button deviceIdLoginButton;

        [Header("Configuration")]
        [SerializeField] private uint devAuthPort = 7878;
        [SerializeField] private string defaultAccountName = "Player1";

        private bool _isLoggedIn = false;

        private void Awake()
        {
            try
            {
                EnsureUIBindings();

                if (accountNameInput.IsNotNull() && string.IsNullOrEmpty(accountNameInput.text))
                {
                    // Auto-assign default profile name: Player1 for Editor, defaultAccountName / Player2 for Standalone build
                    accountNameInput.text = Application.isEditor ? "Player1" : (!string.IsNullOrEmpty(defaultAccountName) ? defaultAccountName : "Player2");
                }
                else
                {
                    // Field already populated or null
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in Awake: {ex.Message}");
            }
        }

        private void OnEnable()
        {
            try
            {
                if (devAuthLoginButton.IsNotNull())
                {
                    devAuthLoginButton.onClick.AddListener(HandleDevAuthLogin);
                }
                else
                {
                    // DevAuth button not assigned
                }

                if (epicAccountLoginButton.IsNotNull())
                {
                    epicAccountLoginButton.onClick.AddListener(HandleEpicAccountLogin);
                }
                else
                {
                    // Epic account button not assigned
                }

                if (deviceIdLoginButton.IsNotNull())
                {
                    deviceIdLoginButton.onClick.AddListener(HandleDeviceIdLogin);
                }
                else
                {
                    // Device ID button not assigned
                }

                EOSSDKComponent.OnLoginFailed += HandleLoginFailed;
                EOSSDKComponent.OnLoginSuccess += HandleLoginSuccess;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in OnEnable: {ex.Message}");
            }
        }

        private void OnDisable()
        {
            try
            {
                EOSSDKComponent.OnLoginFailed -= HandleLoginFailed;
                EOSSDKComponent.OnLoginSuccess -= HandleLoginSuccess;

                if (devAuthLoginButton.IsNotNull())
                {
                    devAuthLoginButton.onClick.RemoveListener(HandleDevAuthLogin);
                }
                else
                {
                    // Button unbind idle
                }

                if (epicAccountLoginButton.IsNotNull())
                {
                    epicAccountLoginButton.onClick.RemoveListener(HandleEpicAccountLogin);
                }
                else
                {
                    // Button unbind idle
                }

                if (deviceIdLoginButton.IsNotNull())
                {
                    deviceIdLoginButton.onClick.RemoveListener(HandleDeviceIdLogin);
                }
                else
                {
                    // Button unbind idle
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in OnDisable: {ex.Message}");
            }
        }

        private void Update()
        {
            try
            {
                if (EOSSDKComponent.Initialized && !_isLoggedIn)
                {
                    _isLoggedIn = true;
                    string puid = EOSSDKComponent.LocalUserProductIdString;
                    if (statusTMP.IsNotNull())
                    {
                        statusTMP.text = $"<color=#80FF80>[OK] Logged In: {puid}</color>";
                    }
                    else
                    {
                        // Status text null
                    }

                    StartCoroutine(HideLoginPanelAfterDelay(1.2f));
                }
                else
                {
                    // Still connecting or already marked logged in
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in Update: {ex.Message}");
            }
        }

        private Coroutine _loginTimeoutCoroutine = null;

        private void StartLoginTimeout()
        {
            try
            {
                if (_loginTimeoutCoroutine.IsNotNull())
                {
                    StopCoroutine(_loginTimeoutCoroutine);
                }
                else
                {
                    // No existing coroutine
                }

                _loginTimeoutCoroutine = StartCoroutine(LoginTimeoutRoutine(14.0f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in StartLoginTimeout: {ex.Message}");
            }
        }

        private void StopLoginTimeout()
        {
            try
            {
                if (_loginTimeoutCoroutine.IsNotNull())
                {
                    StopCoroutine(_loginTimeoutCoroutine);
                    _loginTimeoutCoroutine = null;
                }
                else
                {
                    // Coroutine not active
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in StopLoginTimeout: {ex.Message}");
            }
        }

        private IEnumerator LoginTimeoutRoutine(float timeoutSeconds)
        {
            yield return new WaitForSecondsRealtime(timeoutSeconds);

            if (!_isLoggedIn && !EOSSDKComponent.Initialized)
            {
                UpdateStatus("<color=#FFCC00>Login timed out. DevAuth or Epic Account did not respond. Check DevAuthTool or retry.</color>");
                SetButtonsInteractable(true);
            }
            else
            {
                // Already authenticated
            }
        }

        public void HandleDevAuthLogin()
        {
            try
            {
                string credName = accountNameInput.IsNotNull() && !string.IsNullOrEmpty(accountNameInput.text)
                    ? accountNameInput.text.Trim()
                    : "Player1";

                UpdateStatus($"Connecting to DevAuth ({credName} on port {devAuthPort})...");
                SetButtonsInteractable(false);
                StartLoginTimeout();

                EOSSDKComponent.LoginWithDevAuth(credName, devAuthPort);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in HandleDevAuthLogin: {ex.Message}");
                StopLoginTimeout();
                UpdateStatus($"<color=red>Login Error: {ex.Message}</color>");
                SetButtonsInteractable(true);
            }
        }

        public void HandleEpicAccountLogin()
        {
            try
            {
                UpdateStatus("Opening Epic Games Account Portal in browser...");
                SetButtonsInteractable(false);
                StartLoginTimeout();

                EOSSDKComponent.LoginWithEpicAccount();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in HandleEpicAccountLogin: {ex.Message}");
                StopLoginTimeout();
                UpdateStatus($"<color=red>Login Error: {ex.Message}</color>");
                SetButtonsInteractable(true);
            }
        }

        public void HandleDeviceIdLogin()
        {
            try
            {
                string displayName = accountNameInput.IsNotNull() && !string.IsNullOrEmpty(accountNameInput.text)
                    ? accountNameInput.text.Trim()
                    : "User";

                UpdateStatus($"Logging in with Device ID ({displayName})...");
                SetButtonsInteractable(false);
                StartLoginTimeout();

                EOSSDKComponent.LoginWithDeviceId(displayName);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in HandleDeviceIdLogin: {ex.Message}");
                StopLoginTimeout();
                UpdateStatus($"<color=red>Login Error: {ex.Message}</color>");
                SetButtonsInteractable(true);
            }
        }

        private void UpdateStatus(string message)
        {
            try
            {
                if (statusTMP.IsNotNull())
                {
                    statusTMP.text = message;
                }
                else
                {
                    Debug.Log($"[EOSLoginUI] Status: {message}");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in UpdateStatus: {ex.Message}");
            }
        }

        private void HandleLoginFailed(Epic.OnlineServices.Result result)
        {
            try
            {
                StopLoginTimeout();

                string hint;
                switch (result)
                {
                    case Epic.OnlineServices.Result.InvalidRequest:
                        hint = "InvalidRequest (Client ID unlinked or Permissions not configured in Epic Account Services)";
                        break;
                    case Epic.OnlineServices.Result.UnexpectedError:
                        hint = "UnexpectedError (Client Policy lacks account permission, or DevAuth credential needs refresh)";
                        break;
                    default:
                        hint = result.ToString();
                        break;
                }

                UpdateStatus($"<color=#FF5555>Login Failed: {hint}</color>");
                SetButtonsInteractable(true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in HandleLoginFailed: {ex.Message}");
            }
        }

        private void HandleLoginSuccess()
        {
            try
            {
                StopLoginTimeout();
                _isLoggedIn = true;
                string puid = EOSSDKComponent.LocalUserProductIdString;
                UpdateStatus($"<color=#80FF80>[OK] Logged In: {puid}</color>");
                StartCoroutine(HideLoginPanelAfterDelay(1.2f));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in HandleLoginSuccess: {ex.Message}");
            }
        }

        private void SetButtonsInteractable(bool interactable)
        {
            try
            {
                if (devAuthLoginButton.IsNotNull()) devAuthLoginButton.interactable = interactable;
                else { }

                if (epicAccountLoginButton.IsNotNull()) epicAccountLoginButton.interactable = interactable;
                else { }

                if (deviceIdLoginButton.IsNotNull()) deviceIdLoginButton.interactable = interactable;
                else { }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in SetButtonsInteractable: {ex.Message}");
            }
        }

        private IEnumerator HideLoginPanelAfterDelay(float delaySeconds)
        {
            yield return new WaitForSecondsRealtime(delaySeconds);

            if (loginPanel.IsNotNull())
            {
                loginPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void EnsureUIBindings()
        {
            try
            {
                if (loginPanel.IsNull())
                {
                    loginPanel = gameObject;
                }
                else
                {
                    // Login panel assigned
                }

                if (accountNameInput.IsNull())
                {
                    accountNameInput = this.GetInChildrenOrNull<TMP_InputField>();
                }
                else
                {
                    // Input already assigned
                }

                if (statusTMP.IsNull())
                {
                    statusTMP = this.GetInChildrenOrNull<TextMeshProUGUI>();
                }
                else
                {
                    // Status TMP already assigned
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EOSLoginUI] Exception in EnsureUIBindings: {ex.Message}");
            }
        }
    }
}
