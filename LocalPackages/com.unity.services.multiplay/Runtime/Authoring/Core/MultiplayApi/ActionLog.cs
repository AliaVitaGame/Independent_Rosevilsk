using System;

namespace Unity.Services.Multiplay.Authoring.Core.MultiplayApi
{
    /// <summary>
    /// Action Log from the server. See https://services.docs.unity.com/multiplay-config/v1/#tag/Servers/operation/GetServerActions for details
    /// </summary>
    /// <param name="Id">ID of the action log</param>
    /// <param name="ServerID">ID of the associated server</param>
    /// <param name="ActionID">ID of the action logged</param>
    /// <param name="MachineID">ID of the associated machine</param>
    /// <param name="Message">Message with the action log</param>
    /// <param name="Date">Date of the action</param>
    /// <param name="Attachment">Attachment with the Action Log</param>
    public record ActionLog(long Id, long ServerID, long ActionID, long MachineID, string Message, DateTime Date, string Attachment = default);
}
