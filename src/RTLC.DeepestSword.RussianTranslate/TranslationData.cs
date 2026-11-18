using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RTLC.DeepestSword.RussianTranslate
{
    internal static class TranslationData
    {
        [DataContract]
        private sealed class Entry
        {
            [DataMember]
            public string key = string.Empty;
            [DataMember]
            public string value = string.Empty;
        }

        [DataContract]
        private sealed class TranslationTable
        {
            [DataMember]
            public Entry[] exact = Array.Empty<Entry>();
            [DataMember]
            public Entry[] keyNames = Array.Empty<Entry>();
            [DataMember]
            public Entry[] fragments = Array.Empty<Entry>();
        }

        public static readonly Dictionary<string, string> ExactStrings =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static readonly Dictionary<string, string> KeyNames =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static readonly Dictionary<string, string> Fragments =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private static bool _initialized;

        public static void Initialize(TextAsset translationsAsset)
        {
            if (translationsAsset == null)
            {
                throw new ArgumentNullException(nameof(translationsAsset));
            }

            using var stream = new MemoryStream(translationsAsset.bytes, false);
            var serializer = new DataContractJsonSerializer(typeof(TranslationTable));
            var table = (TranslationTable)serializer.ReadObject(stream);
            if (table == null || table.exact == null || table.keyNames == null || table.fragments == null ||
                table.keyNames.Length == 0 || table.fragments.Length == 0)
            {
                throw new InvalidOperationException("The translation table in rtlc.bundle is incomplete.");
            }

            AddEntries(ExactStrings, table.exact, "exact");
            AddEntries(KeyNames, table.keyNames, "keyNames");
            AddEntries(Fragments, table.fragments, "fragments");

            if (ExactStrings.Count == 0)
            {
                throw new InvalidOperationException("The translation table contains no exact string mappings.");
            }

            _initialized = true;
            Plugin.Log.LogInfo(
                $"Loaded translation data: exact={ExactStrings.Count}, keys={KeyNames.Count}, fragments={Fragments.Count}.");
        }

        private static void AddEntries(Dictionary<string, string> target, Entry[] entries, string section)
        {
            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || entry.value == null)
                {
                    throw new InvalidOperationException($"Invalid entry in translation table section '{section}'.");
                }

                if (target.ContainsKey(entry.key))
                {
                    throw new InvalidOperationException(
                        $"Duplicate key '{entry.key}' in translation table section '{section}'.");
                }

                target.Add(entry.key, entry.value);
            }
        }

        public static string Translate(string input)
        {
            if (!_initialized || string.IsNullOrEmpty(input) || !Plugin.EnabledConfig.Value)
            {
                return input;
            }

            if (ExactStrings.TryGetValue(input, out var exact))
            {
                return exact;
            }

            if (input.IndexOf('\r') >= 0)
            {
                var normalized = input.Replace("\r\n", "\n");
                if (ExactStrings.TryGetValue(normalized, out var normMatch))
                {
                    return normMatch;
                }
            }

            return input;
        }

        public static string TranslateFragments(string input)
        {
            if (!_initialized || string.IsNullOrEmpty(input) || !Plugin.EnabledConfig.Value)
            {
                return input;
            }

            foreach (var pair in Fragments)
            {
                input = input.Replace(pair.Key, pair.Value);
            }

            return input;
        }

        public static string TranslateKeyName(string keyText)
        {
            if (!_initialized || string.IsNullOrEmpty(keyText) || !Plugin.EnabledConfig.Value)
            {
                return keyText;
            }

            return KeyNames.TryGetValue(keyText, out var translated) ? translated : keyText;
        }
    }
}
