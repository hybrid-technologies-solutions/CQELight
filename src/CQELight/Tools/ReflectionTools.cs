using CQELight.Tools.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace CQELight.Tools
{
    /// <summary>
    /// Bunch of tool that are linked to reflection.
    /// </summary>
    public static class ReflectionTools
    {
        #region Static members
        /// <summary>
        /// User globally exclusion of Dlls.
        /// </summary>
        internal static IEnumerable<string> s_DLLBlackList = Enumerable.Empty<string>();
        /// <summary>
        /// User whitelist of DLLs. All ohters are blacklisted
        /// </summary>
        internal static IEnumerable<string> s_DLLsWhiteList = Enumerable.Empty<string>();
        /// <summary>
        /// All current types
        /// </summary>
        private static List<Type> s_AllTypes = new List<Type>();
        /// <summary>
        /// Intialization flag.
        /// </summary>
        private static bool s_Init;
        /// <summary>
        /// List of already treated assemblies.
        /// </summary>
        private static ConcurrentBag<string> s_LoadedAssemblies = new ConcurrentBag<string>();
        /// <summary>
        /// List of already treated assemblies DLLs.
        /// </summary>
        private static ConcurrentBag<string> s_LoadedAssembliesDLL = new ConcurrentBag<string>();

        /// <summary>
        /// List of DLL to not load for types.
        /// </summary>
        private static readonly string[] CONST_REJECTED_DLLS = new[] { "Microsoft", "System", "sqlite3" };
        /// <summary>
        /// Thread safety init object.
        /// </summary>
        private static object s_Lock = new object();

        #endregion

        #region Nested class

        /// <summary>
        /// Proxy class to load assembly by reflection without adding it to AppDomain in full Framework.
        /// </summary>
        private class Proxy : MarshalByRefObject
        {
            /// <summary>
            /// Retrieve an assembly from it's path.
            /// </summary>
            /// <param name="assemblyPath">Path to the assembly.</param>
            /// <returns>Assembly loaded.</returns>
            public Assembly? GetAssembly(string assemblyPath)
            {
                try
                {
                    if (File.Exists(assemblyPath))
                    {
#pragma warning disable S3885 // "Assembly.Load" should be used
                        return Assembly.LoadFile(assemblyPath);
#pragma warning restore S3885 // "Assembly.Load" should be used
                    }
                }
                catch
                {
                    //If file is not loadable, just return null
                }
                return null;
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Get all types from current app by looking to all associated DLLs
        /// </summary>
        /// <param name="rejectedDlls">Name of DLL to no inspect</param>
        /// <returns>Collection of all app types.</returns>
        public static IEnumerable<Type> GetAllTypes(params string[] rejectedDlls)
        {
            if (!s_Init)
            {
                lock (s_Lock)
                {
                    if (s_LoadedAssemblies == null)
                    {
                        s_LoadedAssemblies = new ConcurrentBag<string>();
                    }
                    if (s_AllTypes == null)
                    {
                        s_AllTypes = new List<Type>();
                    }
                }
                if (s_DLLBlackList.Any() || s_DLLsWhiteList.Any())
                {
                    if (s_DLLBlackList.Any() && !s_DLLsWhiteList.Any())
                    {
                        s_DLLBlackList = s_DLLBlackList.Concat(CONST_REJECTED_DLLS.Concat(rejectedDlls))
                            .ToList();
                    }
                    else
                    {
                        if (!s_DLLsWhiteList.Any(s => string.Equals(s, "CQELight", StringComparison.OrdinalIgnoreCase)))
                        {
                            s_DLLsWhiteList = s_DLLsWhiteList.Concat(new[] { "CQELight" }).ToList();
                        }
                    }
                }
                s_Init = true;
            }

            var allTypesBag = new ConcurrentBag<Type>();
            if (!string.IsNullOrWhiteSpace(AppDomain.CurrentDomain.BaseDirectory))
            {
                var assemblies = new DirectoryInfo(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)).GetFiles("*.dll", SearchOption.AllDirectories);
                if (assemblies.Length > 0)
                {
                    assemblies.DoForEach(file =>
                    {
                        if (IsDLLAllowed(file.Name) &&
                        !s_LoadedAssembliesDLL.Any(a => a == file.Name))
                        {
                            s_LoadedAssembliesDLL.Add(file.Name);
                            AssemblyName? assemblyName = null;
                            try
                            {
                                assemblyName = AssemblyName.GetAssemblyName(file.FullName);
                            }
                            catch (BadImageFormatException)
                            {
                                //No need to worry, it should be non managed DLL that will be ignored
                            }
                            if (assemblyName != null
                            && !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().FullName == assemblyName.FullName))
                            {
                                try
                                {
                                    Assembly.Load(assemblyName); //It loads assembly withing AppDomain.CurrentDomain, which is enough
                                }
                                catch
                                {
                                    //If assembly cannot be loaded, it shouldn't throw exception
                                }
                            }
                        }
                    }, true);
                }
            }
            var domainAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            if (domainAssemblies != null)
            {
                domainAssemblies.DoForEach(a =>
                {
                    if (IsDLLAllowed(a.GetName().Name)
                    && !s_LoadedAssemblies.Contains(a.GetName().Name))
                    {
                        s_LoadedAssemblies.Add(a.GetName().Name);
                        Type[] types = new Type[0];
                        try
                        {
                            types = a.GetTypes();
                        }
                        catch (ReflectionTypeLoadException e)
                        {
                            types = e.Types.WhereNotNull().ToArray();
                        }
                        catch
                        {
                            //Ignore all others exceptions
                        }
                        for (int i = 0; i < types.Length; i++)
                        {
                            allTypesBag.Add(types[i]);
                        }
                    }
                }, true);
            }

            if (allTypesBag.Count != 0)
            {
                lock (s_Lock)
                {
                    if (allTypesBag.Count != 0)
                    {
                        s_AllTypes.AddRange(allTypesBag);
                        s_AllTypes = s_AllTypes.Distinct(new TypeEqualityComparer()).ToList();
                    }
                }
            }
            return s_AllTypes;
        }
        #endregion

        #region Private static methods

        private static bool IsDLLAllowed(string dllName)
        {
            if (s_DLLsWhiteList.Any())
            {
                return s_DLLsWhiteList
                    .Any(d => dllName.StartsWith(d, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                return !s_DLLBlackList
                    .Any(d => dllName.StartsWith(d, StringComparison.OrdinalIgnoreCase));
            }
        }

        #endregion
    }
}
