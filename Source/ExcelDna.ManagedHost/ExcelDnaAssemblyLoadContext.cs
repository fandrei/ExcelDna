﻿#if NETCOREAPP
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace ExcelDna.ManagedHost
{
    public class ExcelDnaAssemblyLoadContext : AssemblyLoadContext
    {
        readonly string _basePath;
        readonly AssemblyDependencyResolver _resolver;


        public ExcelDnaAssemblyLoadContext(string basePath) 
            : base($"ExcelDnaAssemblyLoadContext_{Path.GetFileNameWithoutExtension(basePath)}", isCollectible: true)
        {
            _basePath = basePath;
            _resolver = new AssemblyDependencyResolver(basePath);

#if DEBUG
            this.Resolving += ExcelDnaAssemblyLoadContext_Resolving;
            this.ResolvingUnmanagedDll += ExcelDnaAssemblyLoadContext_ResolvingUnmanagedDll;
#endif
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            // CONSIDER: Should we consider priorities for packed vs local files?

            // First try the regular load path
            string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            // Finally we try the AssemblyManager
            return AssemblyManager.AssemblyResolve(assemblyName);
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            string libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }

#if DEBUG
        // NOTE: The resolving events whould not be used if we are handling Load
        //       Added here for extra debugging
        Assembly ExcelDnaAssemblyLoadContext_Resolving(AssemblyLoadContext arg1, AssemblyName arg2)
        {
            Debug.Print($"Resolving event in {arg1.Name} for {arg2}");
            return null;
        }

        IntPtr ExcelDnaAssemblyLoadContext_ResolvingUnmanagedDll(Assembly arg1, string arg2)
        {
            Debug.Print($"ResolvingUnmanagedDll event from assembly {arg1.FullName} for {arg2}");
            return IntPtr.Zero;
        }
#endif

        internal Assembly LoadFromAssemblyBytes(byte[] assemblyBytes, byte[] pdbBytes)
        {
            if (pdbBytes == null)
            {
                return LoadFromStream(new MemoryStream(assemblyBytes));
            }
            else
            {
                return LoadFromStream(new MemoryStream(assemblyBytes), new MemoryStream(pdbBytes));
            }
        }
    }
}
#endif
