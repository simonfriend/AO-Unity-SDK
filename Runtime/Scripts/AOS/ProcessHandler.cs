using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace Permaverse.AO
{
    //!IMPORTANT: This script should be attached to a GameObject called "ProcessHandler" placed at the root of the scene
    public class ProcessHandler : MonoBehaviour
    {
        public static ProcessHandler main;
        public string CurrentAddress => AOConnectManager.main.CurrentAddress;
        public AOProcess CurrentProcess { get => currentProcess; set { currentProcess = value; OnCurrentProcessChanged(); } }
        private AOProcess currentProcess;

        public List<AOProcess> processes = new List<AOProcess>();
        public List<string> availableLuaFiles = new List<string>();

        public GameObject loadingPanel;
        public TMP_InputField inputFieldPid;

        public MessageHandler messageHandler;
        [Header("Settings")]
        public bool autoFetchProcesses = true;
        public bool loadLuaFiles;

        [Header("ProcessInfoPanel")]
        public ProcessInfoButton processInfoButtonPrefab;
        public List<ProcessInfoButton> processInfoButtons;
        public Transform processInfoButtonParent;
        public Button spawnNewProcessButton;
        public Button processSettingsButton;
        public TMP_InputField spawnNewProcessInputField;

        [Header("LuaSettings")]
        public LoadLuaElement loadLuaElementPrefab;
        public List<LoadLuaElement> loadLuaElements;
        public Transform loadLuaElementParent;

        private string spawningProcessName;
        private bool gotErrorOnce = false;

        [DllImport("__Internal")]
        private static extern void FetchProcessesJS(string address);

        [DllImport("__Internal")]
        private static extern void SpawnProcessJS(string name);

        public void Awake()
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

        public void Start()
        {
            if (gameObject.name != "ProcessHandler")
            {
                Debug.LogError("ProcessHandler should be attached to a GameObject called 'ProcessHandler' placed at the root of the scene");
            }

            spawnNewProcessButton?.onClick.AddListener(SpawnProcess);
            processSettingsButton?.onClick.AddListener(TryOpenProcessSettings);

            if (!Application.isEditor && Application.platform == RuntimePlatform.WebGLPlayer)
            {
                if (loadLuaFiles)
                {
                    StartCoroutine(LoadLuaFiles());
                }
            }

            AOConnectManager.main.OnCurrentAddressChange += OnCurrentAddressChanged;
        }

        private void OnCurrentAddressChanged()
        {
            if (autoFetchProcesses)
            {
                FetchProcesses(CurrentAddress);
            }
        }

        private void OnCurrentProcessChanged()
        {
            processSettingsButton.gameObject.SetActive(currentProcess != null);
        }

        public void LoadProcess(AOProcess p)
        {
            if (CurrentProcess != null)
            {
                ProcessInfoButton pib = processInfoButtons.Find((process) => process.process == CurrentProcess);
                if (pib != null)
                {
                    pib.ToggleHighlight(false);
                }
            }

            CurrentProcess = p;
        }

        private void TryOpenProcessSettings()
        {
            if (CurrentProcess != null)
            {
                processSettingsButton.GetComponent<Animator>().SetTrigger("Open");
            }
            else
            {
                AOConnectManager.main.OpenAlert("Select a process first!");
            }
        }

        public void FetchProcesses(string address)
        {
            FetchProcessesJS(address);
            loadingPanel.SetActive(true);
        }

        public void SpawnProcess()
        {
            if (processes.Find(p => p.name == inputFieldPid.text) == null)
            {
                spawningProcessName = inputFieldPid.text;
                SpawnProcessJS(spawningProcessName);
            }
            else
            {
                AOConnectManager.main.OpenAlert("Process with that name already exists!");
            }
        }

        private IEnumerator LoadLuaFiles()
        {
            foreach (string fileName in availableLuaFiles)
            {
                string url = Application.streamingAssetsPath + "/" + fileName + ".lua";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError("Error: " + request.error);
                    }
                    else
                    {
                        // Successfully loaded the file
                        string fileContent = request.downloadHandler.text;

                        LoadLuaElement lle = Instantiate(loadLuaElementPrefab, loadLuaElementParent);
                        lle.SetInfo(fileName, fileContent);
                        loadLuaElements.Add(lle);
                    }
                }
            }
        }

        public void LoadLua(string lua)
        {
            if (CurrentProcess != null)
            {
                LoadLua(CurrentProcess.id, lua);
            }
            else
            {
                Debug.LogError("Current process can't be null!!");
            }
        }

        public void LoadLua(string pid, string lua)
        {
            messageHandler.SendRequest(pid, new List<Tag> { new Tag("Action", "Eval") }, OnLoadLuaResult, lua);
        }

        private void OnLoadLuaResult(bool result, NodeCU nodeCU)
        {
            if (result)
            {
                Debug.Log("Lua loaded successfully!");
            }
            else
            {
                Debug.LogError("Error loading Lua!");
            }
        }

        public void UpdateProcesses(string jsonString)
        {
            loadingPanel.SetActive(false);
            if (jsonString == "Error")
            {
                if (gotErrorOnce)
                {
                    AOConnectManager.main.OpenAlert("Error in fetching processes");
                }
                else
                {
                    gotErrorOnce = true;
                    FetchProcesses(CurrentAddress);
                }
                return;
            }
            else if (jsonString == "[]")
            {
                AOConnectManager.main.OpenAlert("No processes found!");
                return;
            }

            gotErrorOnce = false;

            if (processInfoButtons != null && processInfoButtons.Count > 0)
            {
                foreach (var button in processInfoButtons)
                {
                    Destroy(button.gameObject);
                }
            }

            processInfoButtons = new List<ProcessInfoButton>();

            var processesNode = JSON.Parse(jsonString);
            processes = new List<AOProcess>();

            for (int i = 0; i < processesNode.Count; i++)
            {
                var process = processesNode[i];
                string processId = process["id"];
                var tagsNode = process["tags"];

                Dictionary<string, string> tags = new Dictionary<string, string>();
                for (int j = 0; j < tagsNode.Count; j++)
                {
                    var tag = tagsNode[j];
                    string tagName = tag["name"];
                    string tagValue = tag["value"];
                    tags.Add(tagName, tagValue);
                }

                AOProcess p = new AOProcess();
                p.id = processId;
                p.tags = tags;
                p.shortId = AOUtils.ShortenProcessID(p.id);

                if (p.tags.ContainsKey("Name"))
                {
                    p.name = tags["Name"];
                }
                else
                {
                    p.name = p.shortId;
                }

                processes.Add(p);

                ProcessInfoButton pib = Instantiate(processInfoButtonPrefab, processInfoButtonParent);
                pib.SetInfo(p);
                processInfoButtons.Add(pib);
            }
        }

        public void SpawnProcessCallback(string processId)
        {
            Debug.Log("New spawned process id: " + processId);

            AOProcess p = new AOProcess();
            p.id = processId;
            p.name = spawningProcessName;
            p.shortId = AOUtils.ShortenProcessID(p.id);

            ProcessInfoButton pib = Instantiate(processInfoButtonPrefab, processInfoButtonParent);
            pib.SetInfo(p);
            processInfoButtons.Add(pib);

            spawningProcessName = "";
        }

    }
}