using System;
using System.Collections.Generic;
using System.Linq;
using Mcma.Core;

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public static class CollectionExtensions
    {
        public static void Filter<T>(this IList<T> collection, IDictionary<string, string> filterValues)
        {
            // if we don't have filters to apply, leave the collection as-is
            if (filterValues == null || !filterValues.Any())
                return;
                
            // convert dictionary of property names to dictionary of PropertyInfos
            var propertyValues =
                filterValues
                    .Select(kvp =>
                        new
                        {
                            Property = typeof(T).GetProperties().FirstOrDefault(p => p.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)),
                            PropertyTextValue = kvp.Value
                        }
                    )
                    .Where(x => x.Property != null)
                    .ToList();

            for (var i = collection.Count - 1; i >= 0; i--)
            {
                var curItem = collection[i];
                
                // if any of the properties of the object don't match the filter values, remove it
                if (propertyValues.Any(x => x.Property.GetValue(curItem)?.ToString() != x.PropertyTextValue))
                    collection.RemoveAt(i);
            }
        }
    }
}
