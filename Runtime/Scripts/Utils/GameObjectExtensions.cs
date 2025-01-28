using UnityEngine;

namespace Permaverse.AO
{
    public static class GameObjectExtensions
    {
        public static Transform FindChildRecursively(this Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                {
                    return child;
                }

                // Recursively search the child's children
                Transform result = child.FindChildRecursively(childName);
                if (result != null)
                {
                    return result;
                }
            }

            // If the child with the specified name was not found
            return null;
        }
    }
}