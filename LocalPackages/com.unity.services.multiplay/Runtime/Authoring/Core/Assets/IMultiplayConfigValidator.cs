using System.Collections.Generic;

namespace Unity.Services.Multiplay.Authoring.Core.Assets
{
    interface IMultiplayConfigValidator
    {
        List<Error> Validate(MultiplayConfig config);

        public record Error(string Message);
    }
}
