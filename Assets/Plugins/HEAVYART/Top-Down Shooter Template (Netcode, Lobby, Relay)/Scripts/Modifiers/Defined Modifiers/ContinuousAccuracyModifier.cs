using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class ContinuousAccuracyModifier : ContinuousModifier
    {
        public float value;

        public ContinuousAccuracyModifier()
        {
            type = GetType().Name;
            tag = "cam";
        }

        protected override void SerializeModifier()
        {
            object[] outputData = new object[]
            {
            value,
            duration,
            tag
            };

            serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(outputData);
        }

        protected override ModifierBase DeserializeModifier(string inputData)
        {
            object[] data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(inputData);

            value = Convert.ToSingle(data[0]);
            duration = Convert.ToSingle(data[1]);
            tag = data[2].ToString();

            return this;
        }
    }
}
