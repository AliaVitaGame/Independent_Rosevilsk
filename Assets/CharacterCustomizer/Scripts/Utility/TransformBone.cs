using UnityEngine;

namespace CC
{
    public class TransformBone : MonoBehaviour
    {
        public Vector3 offset = new Vector3(0f, 0f, 0f);

        public enum mode
        {
            Height, FootRotation, BallRotation
        }

        public enum axis
        {
            X, Y, Z
        }

        public axis Axis;
        public mode Mode;

        public void SetOffset(FootOffset footOffset)
        {
            float value;
            switch (Mode)
            {
                case mode.Height:
                    {
                        value = footOffset.HeightOffset;

                        break;
                    }
                case mode.FootRotation:
                    {
                        value = footOffset.FootRotation;
                        break;
                    }
                case mode.BallRotation:
                    {
                        value = footOffset.BallRotation;
                        break;
                    }
                default:
                    {
                        value = 0f;
                        break;
                    }
            }

            switch (Axis)
            {
                case axis.X:
                    {
                        offset = new Vector3(value, 0f, 0f);
                        break;
                    }
                case axis.Y:
                    {
                        offset = new Vector3(0f, value, 0f);
                        break;
                    }
                case axis.Z:
                    {
                        offset = new Vector3(0f, 0f, value);
                        break;
                    }
            }
        }

        private void LateUpdate()
        {
            switch (Mode)
            {
                case mode.Height:
                    {
                        transform.position = transform.position + offset / 100;
                        break;
                    }
                case mode.FootRotation:
                    {
                        transform.eulerAngles = transform.eulerAngles + offset;
                        break;
                    }
                case mode.BallRotation:
                    {
                        transform.eulerAngles = transform.eulerAngles + offset;
                        break;
                    }
            }
        }
    }
}