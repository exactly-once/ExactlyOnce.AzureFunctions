using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Primitives;

namespace ExactlyOnce.AzureFunctions
{
    public static class StringExtensions
    {
        public static Guid ToGuid(this string value)
        {
            var bytes = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(value));
            Array.Resize(ref bytes, 16);
            return new Guid(bytes);
        }

        public static Guid ToGuid(this StringValues value)
        {
            var bytes = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(value));
            Array.Resize(ref bytes, 16);
            return new Guid(bytes);
        }
    }
}