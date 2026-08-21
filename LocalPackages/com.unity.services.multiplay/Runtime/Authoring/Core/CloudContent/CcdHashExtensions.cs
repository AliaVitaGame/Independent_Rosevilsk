using System;
using System.IO;
using System.Security.Cryptography;

namespace Unity.Services.Multiplay.Authoring.Core.CloudContent
{
    static class CcdHashExtensions
    {
        /// <summary>
        /// CCD uses MD5 hash of content encoded as lowercase hex
        ///
        /// From their API docs:
        ///
        /// example: c76580fa26181bfa8d8952826bd334d5
        /// pattern: ^[a-fA-F0-9]+$
        ///
        /// The "content_hash" should be MD5sum hash value.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static string CcdHash(this Stream content)
        {
            using var md5 = MD5.Create();
            var hashBytes = md5.ComputeHash(content);
            content.Position = 0;
            return BitConverter
                .ToString(hashBytes)
                .Replace("-", "")
                .ToLowerInvariant();
        }
    }
}
