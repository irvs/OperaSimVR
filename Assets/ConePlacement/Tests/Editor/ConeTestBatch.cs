using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace OperaSimVR.ConePlacement.Tests
{
    /// <summary>Also runs these NUnit assertions directly when TestRunner discovery stalls under Box64.</summary>
    public static class ConeTestBatch
    {
        [Serializable] sealed class Entry { public string name; public bool passed; public string failure; }
        [Serializable] sealed class Report { public int passed; public int failed; public List<Entry> cases = new List<Entry>(); }
        public static void Run()
        {
            var report = new Report();
            foreach (Type type in new[] { typeof(ConeLayoutTests), typeof(ConeControllerTests) })
            {
                foreach (MethodInfo method in type.GetMethods().OrderBy(m => m.Name))
                {
                    TestCaseAttribute[] cases = method.GetCustomAttributes(typeof(TestCaseAttribute), false).Cast<TestCaseAttribute>().ToArray();
                    if (cases.Length == 0 && !method.IsDefined(typeof(TestAttribute), false)) continue;
                    object[][] arguments = cases.Length == 0 ? new[] { Array.Empty<object>() } : cases.Select(c => c.Arguments).ToArray();
                    for (int i = 0; i < arguments.Length; i++)
                    {
                        var entry = new Entry { name = type.Name + "." + method.Name + "[" + i + "]" };
                        object fixture = Activator.CreateInstance(type);
                        try
                        {
                            Debug.Log("[Cone tests] " + entry.name);
                            foreach (MethodInfo setup in type.GetMethods().Where(m => m.IsDefined(typeof(SetUpAttribute), false))) setup.Invoke(fixture, null);
                            method.Invoke(fixture, arguments[i]);
                            entry.passed = true; report.passed++;
                        }
                        catch (Exception e)
                        { entry.failure = (e is TargetInvocationException ? e.InnerException : e).ToString(); report.failed++; Debug.LogWarning(entry.failure); }
                        finally
                        {
                            foreach (MethodInfo teardown in type.GetMethods().Where(m => m.IsDefined(typeof(TearDownAttribute), false))) teardown.Invoke(fixture, null);
                        }
                        report.cases.Add(entry);
                    }
                }
            }
            Directory.CreateDirectory("Logs/cone-placement-demo");
            File.WriteAllText("Logs/cone-placement-demo/assertions.json", JsonUtility.ToJson(report, true));
            Debug.Log("[Cone tests] Passed: " + report.passed + "; failed: " + report.failed);
            EditorApplication.Exit(report.failed == 0 ? 0 : 1);
        }
    }
}
