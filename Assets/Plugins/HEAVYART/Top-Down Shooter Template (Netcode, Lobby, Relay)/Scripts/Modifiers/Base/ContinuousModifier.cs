using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class ContinuousModifier : ModifierBase
    {
        public float duration;

        //Could be word or any other alias. The shorter, the better. 
        //Used to group modifiers with the same effect into one single modifier with longer activity time.
        public string tag { get; protected set; }
    }
}
