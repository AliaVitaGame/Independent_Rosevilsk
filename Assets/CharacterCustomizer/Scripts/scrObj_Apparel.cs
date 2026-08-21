using System.Collections.Generic;
using UnityEngine;

namespace CC
{
    [CreateAssetMenu(fileName = "Apparel", menuName = "ScriptableObjects/Apparel")]
    public class scrObj_Apparel : ScriptableObject
    {
        [System.Serializable]
        public struct Apparel
        {
            public GameObject Mesh;
            public string Name;
            public string DisplayName;
            public bool AddCopyPoseScript;
            public Texture2D Mask;
            public FootOffset FootOffset;
            public List<CC_Apparel_Material_Collection> Materials;
            public Sprite Icon;
        }

        public List<Apparel> Items = new List<Apparel>();
        public string MaskProperty;
        public string Label;
    }
}