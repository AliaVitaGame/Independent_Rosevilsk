using System.Security.Cryptography;
using System.Text;

namespace SampleProjectAV.CustomUpmManager.Editor
{
    public static class StableHash
    {
        public static string Sha1(string value)
        {
            using (var sha1 = SHA1.Create())
            {
                var bytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(value ?? string.Empty));
                var builder = new StringBuilder(bytes.Length * 2);

                foreach (var b in bytes)
                    builder.Append(b.ToString("x2"));

                return builder.ToString();
            }
        }
    }
}
