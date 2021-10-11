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
using MB;
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
    public static List<BuildTargetGroup> Targets { get; }

    static bool IsObsolete(BuildTargetGroup target)
    {
        var type = typeof(BuildTargetGroup);

        var name = target.ToString();

        var attributes = type.GetField(name).GetCustomAttributes(typeof(ObsoleteAttribute), false);

        return attributes.Length > 0;
    }

    static AssemblySymbolDefine()
    {
        var range = Enum.GetValues(typeof(BuildTargetGroup));
        
        Targets = new List<BuildTargetGroup>(range.Length);

        foreach (BuildTargetGroup target in range)
        {
            if (target == BuildTargetGroup.Unknown) continue;
            if (IsObsolete(target)) continue;

            Targets.Add(target);
        }

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var attribute in assembly.GetCustomAttributes<AssemblySymbolDefineAttribute>())
            {
                Apply(attribute);
            }
        }
    }

    static void Apply(AssemblySymbolDefineAttribute attribute)
    {
        foreach (var target in Targets)
        {
            using (ScriptingDefineSymbols.Lease(target, out var set))
            {
                if (attribute.Obsolete)
                    set.Remove(attribute.ID);
                else
                    set.Add(attribute.ID);
            }
        }
    }
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