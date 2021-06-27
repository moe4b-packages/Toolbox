using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

using System.Reflection;

#if UNITY_EDITOR
/// <summary>
/// Set script symbol defines (#UNITY_EDITOR, #UNITY_ANDROID, ...) as assembly attributes
/// </summary>
[InitializeOnLoad]
public static class AssemblySymbolDefine
{
    public static List<BuildTargetGroup> Targets { get; private set; }

    static bool IsObsolete(BuildTargetGroup target)
    {
        var type = typeof(BuildTargetGroup);

        var name = target.ToString();

        var attributes = type.GetField(name).GetCustomAttributes(typeof(ObsoleteAttribute), false);

        if (attributes == null) return false;
        if (attributes.Length == 0) return false;

        return true;
    }

    static AssemblySymbolDefine()
    {
        Targets = new List<BuildTargetGroup>();

        foreach (BuildTargetGroup target in Enum.GetValues(typeof(BuildTargetGroup)))
        {
            if (target == BuildTargetGroup.Unknown) continue;
            if (IsObsolete(target)) continue;

            Targets.Add(target);
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var attribute in assembly.GetCustomAttributes<AssemblySymbolDefineAttribute>())
                Apply(attribute);
    }

    static void Apply(AssemblySymbolDefineAttribute attribute)
    {
        foreach (var target in Targets)
        {
            var symbols = GetSymbols(target);

            if (attribute.Obsolete)
            {
                if (symbols.Contains(attribute.ID) == false) continue;
                symbols.Remove(attribute.ID);
            }
            else
            {
                if (symbols.Contains(attribute.ID)) continue;
                symbols.Add(attribute.ID);
            }

            SetSymbols(target, symbols);
        }
    }

    #region Symbols
    static HashSet<string> GetSymbols(BuildTargetGroup target)
    {
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(target).Trim();

        var hash = new HashSet<string>();

        foreach (var item in defines.Split(';', ' '))
        {
            if (item == null || item == string.Empty) continue;

            hash.Add(item);
        }

        return hash;
    }

    static void SetSymbols(BuildTargetGroup target, HashSet<string> set)
    {
        var defines = set.Count == 0 ? "" : set.Aggregate((a, b) => $"{a};{b}");

        PlayerSettings.SetScriptingDefineSymbolsForGroup(target, defines);
    }
    #endregion
}
#endif

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = true)]
public class AssemblySymbolDefineAttribute : Attribute
{
    public string ID { get; protected set; }

    public bool Obsolete { get; set; }

    public AssemblySymbolDefineAttribute(string ID)
    {
        this.ID = ID;
    }
}