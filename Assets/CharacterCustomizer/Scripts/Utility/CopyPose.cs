using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace CC
{
    public class CopyPose : MonoBehaviour
    {
        private Transform[] SourceHierarchy;
        private Transform[] TargetHierarchy;
        public List<Transform> SourceBones = new List<Transform>();
        public List<Transform> TargetBones = new List<Transform>();

        private void Start()
        {
            //Get bone hierarchies
            foreach (Transform child in transform.parent)
            {
                if (child.gameObject.name == "root")
                {
                    SourceHierarchy = child.GetComponentsInChildren<Transform>();
                }
            }
            foreach (Transform child in transform)
            {
                if (child.gameObject.name == "root")
                {
                    TargetHierarchy = child.GetComponentsInChildren<Transform>();
                }
            }

            //Only copy bones that are found in both hierarchies, also ensures order is the same
            foreach (Transform child in SourceHierarchy)
            {
                Transform targetBone = TargetHierarchy.FirstOrDefault(t => t.name == child.name);
                if (targetBone != null)
                {
                    SourceBones.Add(child);
                    TargetBones.Add(targetBone);
                }
            }
        }

        private void Update()
        {
            //Copy bone transform
            for (int i = 0; i < SourceBones.Count; i++)
            {
                TargetBones[i].localPosition = SourceBones[i].localPosition;
                TargetBones[i].localRotation = SourceBones[i].localRotation;
                TargetBones[i].localScale = SourceBones[i].localScale;
            }
        }
    }
}