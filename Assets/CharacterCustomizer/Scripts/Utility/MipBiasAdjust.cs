using UnityEngine;

namespace CC
{
    public class MipBiasAdjust : MonoBehaviour
    {
        public float[] MipBias;
        public Texture[] Textures;

        public void Start()
        {
            for (int i = 0; i < Textures.Length; i++)
            {
                Textures[i].mipMapBias = MipBias[i];
            }
        }
    }
}