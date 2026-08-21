using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class InstantDamage : InstantModifier
    {
        public float damage;

        public InstantDamage()
        {
            type = GetType().Name;
        }

        protected override void SerializeModifier()
        {
            object[] outputData = new object[] { damage };

            serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(outputData);
        }

        protected override ModifierBase DeserializeModifier(string inputData)
        {
            object[] data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(inputData);

            damage = Convert.ToSingle(data[0]);

            return this;
        }
    }
}


