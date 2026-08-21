using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class ContinuousDamage : ContinuousModifier
    {
        public float damage;

        public ContinuousDamage()
        {
            type = GetType().Name;
            tag = "cdmg";
        }

        protected override void SerializeModifier()
        {
            object[] outputData = new object[]
            {
            damage,
            tag
            };

            serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(outputData);
        }

        protected override ModifierBase DeserializeModifier(string inputData)
        {
            object[] data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(inputData);

            damage = Convert.ToSingle(data[0]);
            tag = data[1].ToString();

            return this;
        }
    }
}

