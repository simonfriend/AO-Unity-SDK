using UnityEngine;
using UnityEngine.UI;

namespace Permaverse.AO
{
    public class SendCommandButton : MonoBehaviour
    {
        public string commandToSend;
        private Button button;

        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(() => AOSManager.main.RunCommand(commandToSend));
        }
    }
}
