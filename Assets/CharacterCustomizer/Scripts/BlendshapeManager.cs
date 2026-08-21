using System.Collections.Generic;
using UnityEngine;
using System;

namespace CC
{
    public class BlendshapeManager : MonoBehaviour
    {
        private List<String> blendshapeNames = new List<String>();

        private SkinnedMeshRenderer mesh;

        public void parseBlendshapes()
        {
            mesh = gameObject.GetComponent<SkinnedMeshRenderer>();

            if (mesh.sharedMesh != null)
            {
                for (int i = 0; i < mesh.sharedMesh.blendShapeCount; i++)
                {
                    blendshapeNames.Add(mesh.sharedMesh.GetBlendShapeName(i));
                }
            }
        }

        public void setBlendshape(string name, float value)
        {
            int index = blendshapeNames.FindIndex(t => t == name);
            if (index != -1) mesh.SetBlendShapeWeight(index, value * 100);
        }
    }
}