using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public class ModifierBase : INetworkSerializable
    {
        //Modifier class name
        protected string type;

        //JSON
        protected string serializedData;

        //Required to serialize custom types (INetworkSerializable method)
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            //Call overriden version of method in inherited class
            SerializeModifier();

            serializer.SerializeValue(ref type);
            serializer.SerializeValue(ref serializedData);
        }

        public ModifierBase UnpackModifier()
        {
            //Create new object of type and fill it with deserizlized JSON data
            //DeserializeModifier calls overridden version of method in inherited class
            string typeName = $"{typeof(ModifierBase).Namespace}.{type}";
            return (Activator.CreateInstance(Type.GetType(typeName)) as ModifierBase).DeserializeModifier(serializedData);
        }

        //Override in inherited class
        protected virtual void SerializeModifier() { }

        //Override in inherited class
        protected virtual ModifierBase DeserializeModifier(string inputData) { return null; }

    }

    public class ModifierContainerBase : ScriptableObject
    {
        //Override in inherited class
        public virtual ModifierBase GetConfig() { throw new NotImplementedException(); }
    };
}
