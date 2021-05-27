using System;
using Weikio.PluginFramework.Abstractions;
using System.Linq;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class PluginExtensions
    {
        public static object Create(this Plugin plugin, IServiceProvider serviceProvider, params object[] parameters)
        {
            return ActivatorUtilities.CreateInstance(serviceProvider, plugin, parameters);
        }
        
        public static T Create<T>(this Plugin plugin, IServiceProvider serviceProvider, params object[] parameters) where T : class
        {
            var result = default(T);

            try
            {
                result = ActivatorUtilities.CreateInstance(serviceProvider, plugin, parameters) as T;
            }
            catch(Exception e)
            {
                var iVisualizationTypes = (from t in plugin.Assembly.GetExportedTypes()
                                           where !t.IsInterface && !t.IsAbstract && !t.IsEnum
                                           where typeof(T).IsAssignableFrom(t)
                                           select t).ToArray();
                var instantiatedTypes = iVisualizationTypes.Select(t =>
                {
                    var constructor = t.GetConstructors().OrderBy(c => c.GetParameters().Length).FirstOrDefault();
                    if (constructor == null)
                        throw new MissingMethodException("No public constructor defined for this object");
                    var ctorParams = new object[constructor.GetParameters().Length];
                    var cParams = constructor.GetParameters();
                    for (var i = 0; i < cParams.Length; i++)
                    {
                        ctorParams[i] = serviceProvider.GetService(cParams[i].ParameterType);
                    }
                    var instance = constructor.Invoke(ctorParams);
                    return instance;
                });

                if (instantiatedTypes != null)
                {
                    result = instantiatedTypes.First() as T;
                }

            }
            return result;
        }
    }
}
