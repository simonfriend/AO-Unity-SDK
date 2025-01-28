using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.IO;
using SimpleFileBrowser;
using System;

namespace Permaverse.AO
{
	public enum AOSState
	{
		LoggedOut = 0,
		Loading = 1,
		LoggedIn = 2
	}

	public class AOSManager : MonoBehaviour
	{
		public static AOSManager main { get; private set; }

		[Header("General")]
		public EditVariablePopup editVariablePopup;
		public bool userFriendlyLogs;
		public float loginTimeout;
		public bool useColorsInConsole;
		public int maxConsoleLines;
		public Dictionary<string, Action<string>> consoleListeners = new Dictionary<string, Action<string>>();

		[SerializeField] private AOSState state;
		public AOSState State
		{
			get { return state; }
			set { if (state != value) { state = value; OnStateChanged(); OnAOSStateChanged?.Invoke(state); } }
		}
		public static Action<AOSState> OnAOSStateChanged;

		[Header("Login UI")]
		public GameObject loginPanel;
		public GameObject loadingPanel;
		public TMP_InputField usernameInputField;
		public TMP_InputField walletLocationInputField;
		public Button walletFilePickerButton;
		public Button loginButton;
		public TMP_Text errorText;
		public GameObject loadableBlueprintTogglePrefab;
		public RectTransform loadableBlueprintParent;

		[Header("Loading UI")]
		public Button backLoginButton;
		public GameObject longLoadingText;

		[Header("Console UI")]
		public GameObject mainPanel;
		public GameObject console;
		public TMP_Text consoleText;
		public TMP_InputField consoleInputField;
		public ScrollRect scrollRect;
		public Button toggleConsoleButton;
		public Button clearConsoleButton;
		public Button logoutButton;
		public TMP_Text processIDText;

		[Header("UpdatePopupUI")]
		public GameObject updatePopup;
		public Button yesButton;
		public Button noButton;

		private InteractiveCMDShell shell;
		private List<string> newLines = new List<string>();

		private const string forceLogoutString = "FORCELOGOUT";
		private Coroutine waitAndStartCoroutine;
		private bool updating = false;
		private List<string> commandsHistory = new List<string>();
		private int commandsHistoryIndex = 0;

		private Dictionary<string, GameObject> loadableBlueprints = new Dictionary<string, GameObject>();

		[Header("Debug")]
		public bool debug;
		public bool logLines;
		[SerializeField] private List<string> lineBuffer = new List<string>();

		public string processID;

		public bool consoleOn = false;

		private bool messageSentFromInput = false;

		void Awake()
		{
			if (main == null)
			{
				main = this;
				DontDestroyOnLoad(gameObject);
			}
			else
			{
				Destroy(gameObject);
			}
		}

		void Start()
		{
			FileBrowser.SetFilters(true, new FileBrowser.Filter("Wallet", ".json"));

			loginButton.onClick.AddListener(() => Login());
			walletFilePickerButton.onClick.AddListener(() => OpenWalletBrowser());

			toggleConsoleButton.onClick.AddListener(() => ToggleConsole(!consoleOn));
			clearConsoleButton.onClick.AddListener(() => ClearShell());
			logoutButton.onClick.AddListener(() => Logout());
			backLoginButton.onClick.AddListener(() => Logout(false));

			yesButton.onClick.AddListener(() => { shell.RunCommand("y"); updatePopup.SetActive(false); ClearShell(); updating = true; StartCoroutine(WaitUpdateAndLogin()); });
			noButton.onClick.AddListener(() => { shell.RunCommand("n"); updatePopup.SetActive(false); waitAndStartCoroutine = StartCoroutine(WaitAndFinalizeLogin()); });

			StartShell();

			// SoundManager.main.PlayLoginAudio();

			GetLoadableBlueprints();
		}

		void Update()
		{
			if (shell != null)
			{
				GetRecentLines();
			}

			if (Input.GetKeyDown(KeyCode.Return))
			{
				RunInputFieldCommand();
			}

			if (Input.GetKeyDown(KeyCode.Escape))
			{
				Application.Quit();
			}

			if (Input.GetKeyDown(KeyCode.UpArrow))
			{
				if (commandsHistoryIndex >= 1)
				{
					commandsHistoryIndex--;
					consoleInputField.text = commandsHistory[commandsHistoryIndex];
				}
			}

			if (Input.GetKeyDown(KeyCode.DownArrow))
			{
				if (commandsHistoryIndex < commandsHistory.Count - 1)
				{
					commandsHistoryIndex++;
					consoleInputField.text = commandsHistory[commandsHistoryIndex];
				}
				else if (commandsHistoryIndex == commandsHistory.Count - 1)
				{
					commandsHistoryIndex++;
					consoleInputField.text = "";
				}
			}
		}

		private void OnDestroy()
		{
			StopShell();
		}

		public void StartShell()
		{
			if (shell == null)
			{
				shell = new InteractiveCMDShell();
			}
			else
			{
				RestartShell();
			}
		}

		public void RestartShell()
		{
			StopShell();
			StartShell();
		}

		public void StopShell()
		{
			if (shell != null)
			{
				shell.Stop();
				shell = null;
			}
		}

		public void ClearShell()
		{
			consoleText.text = "";
			lineBuffer.Clear();
			newLines.Clear();
		}

		public void Login()
		{
			errorText.text = "";
			State = AOSState.Loading;

			string loginString = "aos";

			if (!string.IsNullOrEmpty(usernameInputField.text))
			{
				loginString += " " + usernameInputField.text;
			}

			if (!string.IsNullOrEmpty(walletLocationInputField.text))
			{
				loginString += " --wallet \"" + walletLocationInputField.text + "\"";
			}

			Debug.Log("Logging with: " + loginString);
			shell.RunCommand(loginString);
		}

		public void Logout(bool removeBlueprintsSelection = true)
		{
			ToggleConsole(false);
			StopAllCoroutines();
			ClearShell();
			RestartShell();

			processID = "";
			processIDText.text = "";

			GetLoadableBlueprints(removeBlueprintsSelection);

			consoleListeners.Clear();

			State = AOSState.LoggedOut;
		}

		public void RunCommand(string command, Action<string> callback, string catchValue)
		{
			shell.RunCommand(command);

			if (consoleListeners.ContainsKey(catchValue)) //Updating with new callback
			{
				consoleListeners[catchValue] = callback;
			}
			else
			{
				consoleListeners.Add(catchValue, callback);
			}
		}

		public void RunCommand(string command)
		{
			shell.RunCommand(command);
		}

		protected void ToggleConsole(bool isVisible)
		{
			consoleOn = isVisible;
			console.SetActive(consoleOn);
			clearConsoleButton.gameObject.SetActive(consoleOn);

			if (isVisible)
			{
				StartCoroutine(ScrollDown());
			}
		}

		protected void LogLine(string line)
		{
			consoleText.text += line + "\n";
			TrimExcessLines();

			if (debug)
			{
				lineBuffer.Add(line);
			}
		}

		protected void TrimExcessLines()
		{
			string[] lines = consoleText.text.Split('\n');
			if (lines.Length > maxConsoleLines)
			{
				// Calculate how many lines to remove
				int linesToRemove = lines.Length - maxConsoleLines;
				string trimmedText = string.Join("\n", lines, linesToRemove, lines.Length - linesToRemove);
				consoleText.text = trimmedText;
			}
		}

		protected string EvaulateLine(string line)
		{
			string evaluatedLine = "";

			List<string> keysToRemove = new List<string>();

			if (consoleListeners != null && consoleListeners.Count > 0)
			{
				foreach (string catchValue in consoleListeners.Keys)
				{
					if (line.Contains(catchValue))
					{
						consoleListeners[catchValue]?.Invoke(line);
						keysToRemove.Add(catchValue);
					}
				}
			}

			foreach (string key in keysToRemove)
			{
				consoleListeners.Remove(key);
			}

			if (line.StartsWith("aos process: "))
			{
				processID = line.Replace("aos process:", "").Trim();
				processIDText.text = "ProcessID: " + processID;
				evaluatedLine = "Welcome " + processID;

				waitAndStartCoroutine = StartCoroutine(WaitAndFinalizeLogin());
			}
			else if (line.Contains("An Error occurred trying to boot AOS"))
			{
				Debug.LogError(line);
				errorText.text += line + "\n";
				evaluatedLine = forceLogoutString;
			}
			else if (line.Contains("New version") && line.Contains("available. Would you like to update [Y/n]?"))
			{
				if (waitAndStartCoroutine != null)
				{
					StopCoroutine(waitAndStartCoroutine);
				}
				else
				{
					Debug.LogError("Shouldn't be null coroutine");
				}

				updatePopup.SetActive(true);

				evaluatedLine = line;
			}
			else if (line.Contains("Updated"))
			{
				updating = false;
				evaluatedLine = line;
			}
			else
			{
				evaluatedLine = line;
			}

			if (!userFriendlyLogs) //Ignore formatting
			{
				evaluatedLine = line;
			}

			return evaluatedLine;
		}

		protected void RunInputFieldCommand()
		{
			if (!string.IsNullOrEmpty(consoleInputField.text))
			{
				messageSentFromInput = true;
				shell.RunCommand(consoleInputField.text);
				commandsHistory.Add(consoleInputField.text);
				commandsHistoryIndex = commandsHistory.Count;
				consoleInputField.text = "";
			}
		}

		protected void GetRecentLines()
		{
			shell.GetRecentLines(newLines);
			if (newLines.Count > 0)
			{
				foreach (string line in newLines)
				{
					if (logLines)
					{
						Debug.Log(line);
					}

					string convertedLine = ANSIConverter.ConvertANSIToTextMeshPro(line, useColorsInConsole);
					string evaluated = EvaulateLine(convertedLine);

					if (evaluated == forceLogoutString)
					{
						Logout();
						break;
					}

					if (!string.IsNullOrEmpty(evaluated))
					{
						LogLine(evaluated);
					}
				}

				newLines.Clear();

				if (messageSentFromInput)
				{
					messageSentFromInput = false;
					StartCoroutine(ScrollDown());
				}
			}
		}

		protected IEnumerator ScrollDown()
		{
			yield return null;
			scrollRect.ScrollToBottom();
		}

		protected void OnStateChanged()
		{
			switch (state)
			{
				case AOSState.LoggedOut:
					loginPanel.SetActive(true);
					loadingPanel.SetActive(false);
					mainPanel.SetActive(false);
					// SoundManager.main.PlayLoginAudio();
					break;
				case AOSState.Loading:
					loginPanel.SetActive(false);
					loadingPanel.SetActive(true);
					mainPanel.SetActive(false);
					longLoadingText.SetActive(false);
					break;
				case AOSState.LoggedIn:
					loginPanel.SetActive(false);
					loadingPanel.SetActive(false);
					mainPanel.SetActive(true);
					break;
			}
		}

		protected IEnumerator WaitAndFinalizeLogin()
		{
			string currentShellLine = ANSIConverter.ConvertANSIToTextMeshPro(shell.GetCurrentLine(), useColorsInConsole);
			float timeElapsed = 0;

			while (!currentShellLine.Contains(">")) //Looking for "aos>" because it means I'm logged in correctly
			{
				if (State != AOSState.Loading)
				{
					yield break;
				}

				currentShellLine = ANSIConverter.ConvertANSIToTextMeshPro(shell.GetCurrentLine(), useColorsInConsole);
				timeElapsed += Time.deltaTime;

				if (timeElapsed >= loginTimeout)
				{
					timeElapsed = 0;
					RunCommand("Test = \"Test\""); //To handle bug that when you log in instead of "aos>" there is "undefined"
					longLoadingText.SetActive(true);
				}

				yield return null;
			}

			Debug.Log(currentShellLine);

			yield return new WaitForSeconds(0.5f);

			State = AOSState.LoggedIn;

			if (loadableBlueprints != null && loadableBlueprints.Count > 0)
			{
				StartCoroutine(LoadBlueprints());
			}
		}

		protected IEnumerator WaitUpdateAndLogin()
		{
			while (updating)
			{
				yield return null;
			}

			yield return null;

			Login();
		}

		public void Refresh()
		{
			GetLoadableBlueprints();
		}

		public void GetLoadableBlueprints(bool removeBlueprintsSelection = false)
		{
			string directoryPath = Application.dataPath + "/StreamingAssets";

			List<string> oldSelected = new List<string>();

			if (loadableBlueprints != null && loadableBlueprints.Count > 0)
			{
				foreach (KeyValuePair<string, GameObject> b in loadableBlueprints)
				{
					Toggle lb = b.Value.GetComponentInChildren<Toggle>();
					Button button = b.Value.GetComponentInChildren<Button>();

					if (lb.isOn)
					{
						oldSelected.Add(b.Key);
					}

					Destroy(b.Value);
				}

				loadableBlueprints.Clear();
			}

			if (Directory.Exists(directoryPath))
			{
				string[] files = Directory.GetFiles(directoryPath);

				foreach (string file in files)
				{
					if (Path.GetExtension(file).Equals(".lua", System.StringComparison.OrdinalIgnoreCase))
					{
						string fileName = Path.GetFileName(file);

						GameObject lbGO = Instantiate(loadableBlueprintTogglePrefab, loadableBlueprintParent);
						Toggle lb = lbGO.GetComponentInChildren<Toggle>();

						lb.GetComponentInChildren<TMP_Text>(true).text = fileName;

						if (oldSelected.Contains(file) && !removeBlueprintsSelection)
						{
							lb.isOn = true;
						}

						Button button = lbGO.GetComponentInChildren<Button>();

						button.onClick.AddListener(() => DeleteFileAndRefresh(file));

						loadableBlueprints.Add(file, lbGO);
					}
				}
			}
		}

		protected void DeleteFileAndRefresh(string file)
		{
			File.Delete(file);
			GetLoadableBlueprints();
		}

		protected IEnumerator LoadBlueprints()
		{
			string directoryPath = Application.dataPath + "/StreamingAssets";

			if (Directory.Exists(directoryPath))
			{
				string[] files = Directory.GetFiles(directoryPath);

				foreach (KeyValuePair<string, GameObject> b in loadableBlueprints)
				{
					Toggle lb = b.Value.GetComponentInChildren<Toggle>();
					Button button = b.Value.GetComponentInChildren<Button>();

					if (lb.isOn)
					{
						string fileName = Path.GetFileName(b.Key);
						Debug.Log("Loading lua file found: " + fileName);

						shell.RunCommand(".load " + fileName);

						yield return new WaitForSeconds(0.5f);
					}
				}
			}
			else
			{
				Debug.LogError("Directory not found: " + directoryPath);
			}
		}

		protected void OpenWalletBrowser()
		{
			FileBrowser.SetDefaultFilter(".json");
			StartCoroutine(ShowLoadWalletDialogCoroutine());
		}

		protected IEnumerator ShowLoadWalletDialogCoroutine()
		{
			// Show a load file dialog and wait for a response from user
			// Load file/folder: file, Allow multiple selection: true
			// Initial path: default (Documents), Initial filename: empty
			// Title: "Load File", Submit button text: "Load"
			yield return FileBrowser.WaitForLoadDialog(FileBrowser.PickMode.Files, false, null, null, "Select Wallet", "Load");

			// Dialog is closed
			// Print whether the user has selected some files or cancelled the operation (FileBrowser.Success)
			if (FileBrowser.Success)
			{
				walletLocationInputField.text = FileBrowser.Result[0];
			}
			else
			{
				Debug.LogError("Nothing selected");
			}
		}
	}
}