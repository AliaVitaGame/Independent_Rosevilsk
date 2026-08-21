using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class InstantHeal : InstantModifier
    {
        public float health;

        public InstantHeal()
        {
            type = GetType().Name;
        }

        protected override void SerializeModifier()
        {
            object[] outputData = new object[] { health };

            serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(outputData);
        }

        protected override ModifierBase DeserializeModifier(string inputData)
        {
            object[] data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(inputData);

            health = Convert.ToSingle(data[0]);

            return this;
        }
    }
}
