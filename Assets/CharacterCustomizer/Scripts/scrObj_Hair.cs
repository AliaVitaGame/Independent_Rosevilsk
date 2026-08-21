using System.Collections.Generic;
using UnityEngine;

namespace CC
{
    [CreateAssetMenu(fileName = "Hair", menuName = "ScriptableObjects/Hair")]
    public class scrObj_Hair : ScriptableObject
    {
        [System.Serializable]
        public struct Hairstyle
        {
            public GameObject Mesh;
            public string Name;
            public Texture2D ShadowMap;
            public bool AddCopyPoseScript;
            public Sprite Icon;
        }

        public List<Hairstyle> Hairstyles = new List<Hairstyle>();
        public string ShadowMapProperty;
        public string TintProperty;
    }
}