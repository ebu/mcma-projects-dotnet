using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Mcma.Client;
using Mcma.Core;

namespace Mcma.Azure.WorkflowService.ApiHandler
{
    internal static class ResourceHelper
    {
        private static async Task<McmaResource> CreateResourceAsync<T>(ResourceManager resourceManager, object resource) where T : McmaResource
            => await resourceManager.CreateAsync((T)resource);

        private static async Task<McmaResource> ResolveAsync<T>(ResourceManager resourceManager, string resourceId) where T : McmaResource
            => await resourceManager.ResolveAsync<T>(resourceId);

        private static async Task<McmaResource> UpdateAsync<T>(ResourceManager resourceManager, object resource) where T : McmaResource
            => await resourceManager.UpdateAsync<T>((T)resource);

        private static GenericMethodInvoker CreateMethodInvoker { get; } = new GenericMethodInvoker(typeof(ResourceRoutes), nameof(CreateResourceAsync));
        
        private static GenericMethodInvoker ResolveMethodInvoker { get; } = new GenericMethodInvoker(typeof(ResourceRoutes), nameof(CreateResourceAsync));
        
        private static GenericMethodInvoker UpdateMethodInvoker { get; } = new GenericMethodInvoker(typeof(ResourceRoutes), nameof(CreateResourceAsync));

        public static Task<McmaResource> CreateResourceAsync(this ResourceManager resourceManager, Type resourceType, object resource)
            => CreateMethodInvoker.Invoke<Task<McmaResource>>(resourceType, null, new object[] { resourceManager, resource });

        public static Task<McmaResource> ResolveResourceAsync(this ResourceManager resourceManager, Type resourceType, string resourceId)
            => ResolveMethodInvoker.Invoke<Task<McmaResource>>(resourceType, null, new object[] { resourceManager, resourceId });

        public static Task<McmaResource> UpdateResourceAsync(this ResourceManager resourceManager, Type resourceType, object resource)
            => UpdateMethodInvoker.Invoke<Task<McmaResource>>(resourceType, null, new object[] { resourceManager, resource });
            
        private class GenericMethodInvoker
        {
            public GenericMethodInvoker(Type type, string methodName)
            {
                GenericMethod = type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            }

            private MethodInfo GenericMethod { get; }

            private Dictionary<Type, MethodInfo> Methods { get; }

            public T Invoke<T>(Type type, object instance, object[] args)
            {
                if (!Methods.ContainsKey(type))
                    Methods[type] = GenericMethod.MakeGenericMethod(type);
                
                return (T)Methods[type].Invoke(instance, args);
            }
        }
    }
}
