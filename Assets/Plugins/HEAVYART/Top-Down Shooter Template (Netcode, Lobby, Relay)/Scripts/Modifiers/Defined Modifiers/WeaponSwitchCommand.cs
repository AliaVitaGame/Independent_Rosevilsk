using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    [Serializable]
    public class WeaponSwitchCommand : InstantModifier
    {
        public WeaponType weapon;

        public WeaponSwitchCommand()
        {
            type = GetType().Name;
        }

        protected override void SerializeModifier()
        {
            object[] outputData = new object[] { (int)weapon };

            serializedData = Newtonsoft.Json.JsonConvert.SerializeObject(outputData);
        }

        protected override ModifierBase DeserializeModifier(string inputData)
        {
            object[] data = Newtonsoft.Json.JsonConvert.DeserializeObject<object[]>(inputData);

            weapon = (WeaponType)Convert.ToInt32(data[0]);

            return this;
        }
    }
}
