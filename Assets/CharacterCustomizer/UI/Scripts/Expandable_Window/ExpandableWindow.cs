using UnityEngine;

namespace CC
{
    public class ExpandableWindow : MonoBehaviour
    {
        public float ExpandedHeight;
        public float ClosedHeight;
        public bool Expanded;
        private float TargetHeight;
        private float CurrentHeight;
        private RectTransform rT;

        private void Start()
        {
            rT = gameObject.GetComponent<RectTransform>();
            TargetHeight = CurrentHeight = Expanded ? ExpandedHeight : ClosedHeight;
            rT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, CurrentHeight);
        }

        public void setExpandedState()
        {
            Expanded = !Expanded;
            TargetHeight = Expanded ? ExpandedHeight : ClosedHeight;
        }

        private void Update()
        {
            if (Mathf.Abs(CurrentHeight - TargetHeight) > 1)
            {
                CurrentHeight = Mathf.Lerp(CurrentHeight, TargetHeight, Time.deltaTime * 5);
                rT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, CurrentHeight);
            }
        }
    }
}