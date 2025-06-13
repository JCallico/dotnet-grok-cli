using GrokCLI.Core.Models;
using Newtonsoft.Json;
using System.Reflection;

namespace GrokCLI.Core.Abstractions
{
    /// <summary>
    /// Base class for function implementations that provides automatic parameter discovery
    /// </summary>
    public abstract class FunctionBase : IFunction
    {
        public abstract Task<string> ExecuteAsync(string arguments);

        public virtual FunctionDefinition GetDefinition()
        {
            var functionAttribute = GetType().GetCustomAttribute<FunctionAttribute>();
            if (functionAttribute == null)
                throw new InvalidOperationException($"Function class {GetType().Name} must have a FunctionAttribute");

            var parameters = GetParametersFromType();
            
            return new FunctionDefinition
            {
                Name = functionAttribute.Name,
                Description = functionAttribute.Description,
                Parameters = parameters
            };
        }

        /// <summary>
        /// Automatically discovers parameters from the function's argument class using reflection
        /// Override this method if you need custom parameter definition logic
        /// </summary>
        protected virtual FunctionParameters GetParametersFromType()
        {
            var parameters = new FunctionParameters
            {
                Type = "object",
                Properties = new Dictionary<string, FunctionProperty>(),
                Required = new List<string>()
            };

            // Look for a nested class that represents the arguments
            var argumentsType = FindArgumentsType();
            if (argumentsType == null)
                return parameters;

            var properties = argumentsType.GetProperties();
            foreach (var property in properties)
            {
                var paramAttribute = property.GetCustomAttribute<FunctionParameterAttribute>();
                if (paramAttribute == null) continue;

                var propertyName = GetJsonPropertyName(property);
                var functionProperty = new FunctionProperty
                {
                    Type = GetJsonType(property.PropertyType),
                    Description = paramAttribute.Description
                };

                if (paramAttribute.EnumValues != null && paramAttribute.EnumValues.Length > 0)
                {
                    functionProperty.Enum = paramAttribute.EnumValues.ToList();
                }

                parameters.Properties[propertyName] = functionProperty;

                if (paramAttribute.IsRequired)
                {
                    parameters.Required.Add(propertyName);
                }
            }

            return parameters;
        }

        private Type? FindArgumentsType()
        {
            // Look for nested classes ending with "Args" or "Arguments"
            var nestedTypes = GetType().GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public);
            return nestedTypes.FirstOrDefault(t => 
                t.Name.EndsWith("Args", StringComparison.OrdinalIgnoreCase) ||
                t.Name.EndsWith("Arguments", StringComparison.OrdinalIgnoreCase));
        }

        private string GetJsonPropertyName(PropertyInfo property)
        {
            var jsonProperty = property.GetCustomAttribute<JsonPropertyAttribute>();
            return jsonProperty?.PropertyName ?? property.Name.ToLowerInvariant();
        }

        private string GetJsonType(Type propertyType)
        {
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
            
            if (underlyingType == typeof(string))
                return "string";
            if (underlyingType == typeof(int) || underlyingType == typeof(long) ||
                underlyingType == typeof(double) || underlyingType == typeof(float) ||
                underlyingType == typeof(decimal))
                return "number";
            if (underlyingType == typeof(bool))
                return "boolean";
            if (propertyType.IsArray || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>)))
                return "array";
            
            return "object";
        }
    }
}
