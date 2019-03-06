using System;
using Microsoft.Build.Evaluation;
using System.Collections.Generic;
using System.Linq;

namespace MSBuild.SolutionSdk.Tasks.Sln
{
    static class SlnExtensions
    {
        public static string ToSolutionString(this Guid guid)
        {
            return guid.ToString("B").ToUpperInvariant();
        }
        /// <summary>
        /// Gets the value of the given property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default value to return in the case when the property has no value.</param>
        /// <returns>The value of the property if one exists, otherwise the default value specified.</returns>
        public static string GetPropertyValueOrDefault(this Project project, string name, string defaultValue)
        {
            string value = project.GetPropertyValue(name);

            // MSBuild always returns String.Empty if the property has no value
            return value == String.Empty ? defaultValue : value;
        }
        /// <summary>
        /// Gets the value of the property value in addition to the conditioned property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default values comma separated to return in the case when the property has no value.</param>
        /// <returns>The values of the property if one exists, otherwise the default value specified.</returns>
        public static IEnumerable<string> GetPossiblePropertyValuesOrDefault(this Project project, string name, string defaultValue)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string propertyValue = project.GetPropertyValue(name);

            // add the actual properties first
            if (!string.IsNullOrEmpty(propertyValue))
            {
                values.Add(propertyValue);
            }

            // filter those that were already in the Properties
            foreach (var conditionPropertyValue in project.GetConditionedPropertyValuesOrDefault(name, string.Empty))
            {
                values.Add(conditionPropertyValue);
            }

            return values.Any() ? values : (defaultValue?.Split(',') ?? Enumerable.Empty<string>());
        }
        /// <summary>
        /// Gets the value of the given conditioned property in this project.
        /// </summary>
        /// <param name="project">The <see cref="Project"/> to get the property value from.</param>
        /// <param name="name">The name of the property to get the value of.</param>
        /// <param name="defaultValue">A default values comma separated to return in the case when the property has no value.</param>
        /// <returns>The values of the property if one exists, otherwise the default value specified.</returns>
        public static IEnumerable<string> GetConditionedPropertyValuesOrDefault(this Project project, string name, string defaultValue)
        {
            if (!project.ConditionedProperties.ContainsKey(name))
            {
                return defaultValue.Split(',');
            }

            return project.ConditionedProperties[name];
        }
    }
}