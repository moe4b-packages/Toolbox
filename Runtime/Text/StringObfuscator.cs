using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MB
{
    public class StringObfuscator
    {
        public void SetMethods(EncryptDelegate encrypt, DecryptDelegate decrypt)
        {
            Encrypt = encrypt;
            Decrypt = decrypt;
        }

        public EncryptDelegate Encrypt { get; set; } = DefaultEncryptMethod;
        public delegate string EncryptDelegate(string text);
        public static string DefaultEncryptMethod(string text) => text;

        public DecryptDelegate Decrypt { get; set; } = DefaultDecryptMethod;
        public delegate string DecryptDelegate(string text);
        public static string DefaultDecryptMethod(string text) => text;
    }
}