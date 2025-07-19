using System.Reflection;
using System.Text.Json;
using FluentAI.Net.Attributes;

namespace FluentAI.Net.Utils
{
    /// <summary>
    /// Utility class for generating JSON schemas from C# types and methods
    /// </summary>
    public static class SchemaGenerator
    {
        public static (string Name, string Description, string Schema) SerializeStructuredOutput<T>()
        {
            Type type = typeof(T);
            var schemaObject = BuildSchemaForType(type, 0);
            string json = JsonSerializer.Serialize(schemaObject);
            return (type.Name, GetDescription(type), json);
        }

        public static (string Name, string Description, string Schema) SerializeMethod(MethodInfo method)
        {
            var schemaObject = BuildSchemaForMethod(method);
            string json = JsonSerializer.Serialize(schemaObject);
            return (method.Name, GetDescription(method), json);
        }

        private static Dictionary<string, object> BuildSchemaForMethod(MethodInfo method)
        {
            var parameters = method.GetParameters();

            // Only include parameters that are not defaulted
            var requiredParameters = parameters.Select(p => p.Name).ToArray();

            var parametersSchema = new Dictionary<string, object>();

            foreach (var param in parameters)
            {
                var clrType = param.ParameterType;
                var jsonType = MapClrTypeToJsonType(clrType);
                var parameterSchema = new Dictionary<string, object>()
                {
                    {"type", jsonType},
                    {"description", GetDescription(param) }
                };

                // If the parameter itself is an object, build its schema recursively
                if (jsonType == "object")
                {
                    var subSchema = BuildSchemaForType(clrType, 1);
                    parameterSchema["required"] = subSchema["required"];
                    parameterSchema["properties"] = subSchema["properties"];
                }
                else if (jsonType == "array")
                {
                    var elementType = clrType.GetElementType() ?? clrType.GetGenericArguments().First();
                    var subSchema = BuildSchemaForType(elementType, 1);
                    parameterSchema["items"] = subSchema;
                }

                parametersSchema[param.Name] = parameterSchema;
            }

            return new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", parametersSchema },
                { "required", requiredParameters },
                { "additionalProperties", false },
                { "strict", true }
            };
        }

        private static Dictionary<string, object>? BuildSchemaForType(Type type, int recursionDepth)
        {
            if (recursionDepth > 15)
            {
                return null;
            }

            var properties = type.GetProperties()
                .Where(prop => prop.GetMethod?.IsVirtual != true)
                .Where(prop => prop.SetMethod?.IsPublic == true)
                .ToList();

            // Only include non-nullable properties as "required"
            var requiredProperties = properties
                .Where(prop => Nullable.GetUnderlyingType(prop.PropertyType) == null)
                .Select(prop => prop.Name)
                .ToList();

            var propertiesSchema = new Dictionary<string, object>();
            foreach (var prop in properties)
            {
                var clrType = prop.PropertyType;
                var jsonType = MapClrTypeToJsonType(clrType);
                var propertySchema = new Dictionary<string, object>
                {
                    { "type", jsonType },
                    { "description", GetDescription(prop) }
                };

                var enumValues = GetEnumValues(prop);
                if (enumValues != null)
                {
                    propertySchema["enum"] = enumValues;
                }

                // If the property itself is an object, build its schema recursively
                if (jsonType == "object")
                {
                    var subSchema = BuildSchemaForType(clrType, recursionDepth + 1);
                    if(subSchema == null)
                    {
                        requiredProperties.Remove(prop.Name);
                        continue;
                    }
                    propertySchema["required"] = subSchema["required"];
                    propertySchema["properties"] = subSchema["properties"];
                }
                else if (jsonType == "array")
                {
                    // get the type of the array
                    var elementType = clrType.GetElementType() ?? clrType.GetGenericArguments().First();
                    var subSchema = BuildSchemaForType(elementType, recursionDepth + 1);
                    if (subSchema == null)
                    {
                        requiredProperties.Remove(prop.Name);
                        continue;
                    }
                    propertySchema["items"] = subSchema;
                }

                propertiesSchema[prop.Name] = propertySchema;
            }

            return new Dictionary<string, object>
            {
                { "type", "object" },
                { "required", requiredProperties },
                { "properties", propertiesSchema },
                { "additionalProperties", false },
                { "strict", true }
            };
        }

        private static string MapClrTypeToJsonType(Type type)
        {
            if (type == null)
                return "object";

            // Handle nullable types by getting the underlying type
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null)
                type = underlyingType;

            if (type == typeof(string))
                return "string";
            if (type == typeof(int) || type == typeof(long) || type == typeof(short))
                return "integer";
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
                return "number";
            if (type == typeof(bool))
                return "boolean";

            var genericListTypes = new Type[] { typeof(List<>), typeof(IList<>), typeof(IEnumerable<>) };
            bool isGenericList = type.GetInterfaces().Any(i => i.IsGenericType && genericListTypes.Contains(i.GetGenericTypeDefinition()));

            if (type.IsArray || isGenericList)
                return "array";

            // Fallback to object for complex types
            return "object";
        }

        private static Dictionary<ICustomAttributeProvider, DescriptionAttribute?> _descriptionCache = new Dictionary<ICustomAttributeProvider, DescriptionAttribute?>();

        private static DescriptionAttribute? GetDescriptionAttribute(ICustomAttributeProvider member)
        {
            if (_descriptionCache.TryGetValue(member, out var descriptionAttr))
                return descriptionAttr;

            descriptionAttr = member.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                    .FirstOrDefault() as DescriptionAttribute;
            _descriptionCache[member] = descriptionAttr;
            return descriptionAttr;
        }

        private static object[]? GetEnumValues(ICustomAttributeProvider member)
        {
            var descriptionAttr = GetDescriptionAttribute(member);
            
            return descriptionAttr?.EnumValues;
        }

        private static string GetDescription(MemberInfo member)
        {
            var descriptionAttr = GetDescriptionAttribute(member);

            // For methods, show a method-specific default description.
            if (member.MemberType == MemberTypes.Method)
                return descriptionAttr?.Description ?? $"Method {member.Name}";

            // For properties, show the property name and its CLR type.
            if (member is PropertyInfo prop)
                return descriptionAttr?.Description ?? $"{prop.Name} of type {MapClrTypeToJsonType(prop.PropertyType)}";

            return descriptionAttr?.Description ?? member.Name;
        }

        private static string GetDescription(Type type)
        {
            var descriptionAttr = GetDescriptionAttribute(type);
            return descriptionAttr?.Description ?? type.Name;
        }

        private static string GetDescription(ParameterInfo parameter)
        {
            var descriptionAttr = GetDescriptionAttribute(parameter);
            return descriptionAttr?.Description ?? $"{parameter.Name} of type {MapClrTypeToJsonType(parameter.ParameterType)}";
        }
    }
} 