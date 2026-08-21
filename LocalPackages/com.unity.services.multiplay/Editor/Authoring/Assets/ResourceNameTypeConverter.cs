using System;
using System.IO;
using Unity.Services.Multiplay.Authoring.Core.Assets;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Core;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Core.Events;
using Unity.Services.Multiplay.Authoring.YamlDotNet.Serialization;

namespace Unity.Services.Multiplay.Authoring.Editor.Assets
{
    sealed class ResourceNameTypeConverter : IYamlTypeConverter
    {
        public bool Accepts(Type type)
        {
            return typeof(IResourceName).IsAssignableFrom(type);
        }

        public object ReadYaml(IParser parser, Type type)
        {
            var scalar = parser.Current as Scalar;
            parser.MoveNext();

            if (scalar == null)
            {
                throw new InvalidDataException("Failed to retrieve scalar value.");
            }

            var inst = Activator.CreateInstance(type);
            type.GetProperty(nameof(IResourceName.Name))?.SetValue(inst, scalar.Value);

            return inst;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type)
        {
            var name = (IResourceName)value;
            emitter.Emit(new Scalar(null, name.Name));
        }
    }
}
