using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HEAVYART.TopDownShooter.Netcode
{
    public static class CommandLineHelper
    {
        public static bool TryGetArgumentValue(string argumentName, out string argumentValue)
        {
            string[] commandLineArguments = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < commandLineArguments.Length; i++)
            {
                if (commandLineArguments[i] == "-" + argumentName && commandLineArguments.Length > i + 1)
                {
                    argumentValue = commandLineArguments[i + 1];
                    return true;
                }
            }

            argumentValue = string.Empty;
            return false;
        }
    }
}
