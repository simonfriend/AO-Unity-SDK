using UnityEngine;

namespace Permaverse.AO
{
    public class LoadingRotator : MonoBehaviour
    {
        public float speed;
        private float rotZ = 0;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            rotZ += speed * Time.deltaTime;
            transform.eulerAngles = new Vector3(0f, 0f, rotZ);
        }
    }
}