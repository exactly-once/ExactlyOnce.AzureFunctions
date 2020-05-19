using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace ExactlyOnce.AzureFunctions
{
    public static class HelperExtensions
    {
        public static Guid ToGuid(this string value)
        {
            using MD5 md5 = MD5.Create();
            var hash = md5.ComputeHash(Encoding.Default.GetBytes(value));
            
            return new Guid(hash);
        }
    }
}