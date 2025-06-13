using GrokCLI.Core.Models;

namespace GrokCLI.Core.Abstractions
{
    /// <summary>
    /// Interface that all function implementations must implement
    /// </summary>
    public interface IFunction
    {
        /// <summary>
        /// Gets the function definition including name, description, and parameters
        /// </summary>
        FunctionDefinition GetDefinition();

        /// <summary>
        /// Executes the function with the provided arguments
        /// </summary>
        /// <param name="arguments">JSON string containing the function arguments</param>
        /// <returns>JSON string containing the function result</returns>
        Task<string> ExecuteAsync(string arguments);
    }

    /// <summary>
    /// Attribute to mark and configure function classes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class FunctionAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; }
        public bool IsEnabled { get; set; } = true;

        public FunctionAttribute(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    /// <summary>
    /// Attribute to mark and configure function parameters
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FunctionParameterAttribute : Attribute
    {
        public string Description { get; }
        public bool IsRequired { get; set; } = false;
        public string[]? EnumValues { get; set; }

        public FunctionParameterAttribute(string description)
        {
            Description = description;
        }
    }
}
