using System;
using System.Collections.Generic;
using Unity.Services.Multiplay.Authoring.Core.Builds;
using Error = Unity.Services.Multiplay.Authoring.Core.Assets.IMultiplayConfigValidator.Error;

namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    class MultiplayConfigValidator : IMultiplayConfigValidator
    {
        IFileReader m_FileReader;

        public MultiplayConfigValidator(IFileReader reader)
        {
            m_FileReader = reader ?? throw new ArgumentNullException(nameof(reader));
        }

        public List<Error> Validate(MultiplayConfig config)
        {
            var errors = new List<Error>();

            if (config.Version != "1.0")
            {
                errors.Add(new Error("version must be 1.0"));
            }

            foreach (var buildDefinition in config.Builds)
            {
                var buildPath = buildDefinition.Value.BuildPath;
                if (!m_FileReader.DirectoryExists(buildPath))
                {
                    errors.Add(new Error($"BuildDefinition[{buildDefinition.Key}] buildPath[{buildPath}] does not exist!"));
                }
            }

            foreach (var(name, buildConfig) in config.BuildConfigurations)
            {
                if (buildConfig.BinaryPath.StartsWith("/"))
                {
                    errors.Add(new Error($"BuildConfiguration[{name}] binaryPath should not start with '/'!"));
                }
            }

            return errors;
        }
    }
}
