using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Framework;

namespace MsBuildPipeLogger
{
    internal static class ParameterParser
    {
        internal enum ParameterType
        {
            Handle,
            Name,
            Server
        }

        public static IPipeWriter GetPipeFromParameters(string parameters)
        {
            KeyValuePair<ParameterType, string>[] segments = ParseParameters(parameters);

            if (segments.Any(x => string.IsNullOrWhiteSpace(x.Value)))
            {
                throw new LoggerException($"Invalid or empty parameter value");
            }

            // Anonymous pipe
            if (segments[0].Key == ParameterType.Handle)
            {
                if (segments.Length > 1)
                {
                    throw new LoggerException("Handle can only be specified as a single parameter");
                }
                return new AnonymousPipeWriter(segments[0].Value);
            }

            // Named pipe
            if (segments[0].Key == ParameterType.Name)
            {
                if (segments.Length == 1)
                {
                    return new NamedPipeWriter(segments[0].Value);
                }
                if (segments[1].Key != ParameterType.Server)
                {
                    throw new LoggerException("Only server and name can be specified for a named pipe");
                }
                return new NamedPipeWriter(segments[1].Value, segments[0].Value);
            }
            if (segments.Length == 1 || segments[1].Key != ParameterType.Name)
            {
                throw new LoggerException("Pipe name must be specified for a named pipe");
            }
            return new NamedPipeWriter(segments[0].Value, segments[1].Value);
        }

        internal static KeyValuePair<ParameterType, string>[] ParseParameters(string parameters)
        {
            string[] segments = parameters.Split(';');
            if (segments.Length < 1 || segments.Length > 2)
            {
                throw new LoggerException("Unexpected number of parameters");
            }
            return segments.Select(x => ParseParameter(x)).ToArray();
        }

        private static KeyValuePair<ParameterType, string> ParseParameter(string parameter)
        {
            string[] parts = parameter.Trim().Trim('"').Split('=');

            // No parameter name specified
            if (parts.Length == 1)
            {
                return new KeyValuePair<ParameterType, string>(ParameterType.Handle, parts[0].Trim());
            }

            // Parse the parameter name
            if (!Enum.TryParse(parts[0].Trim(), true, out ParameterType parameterType))
            {
                throw new LoggerException($"Invalid parameter name {parts[0]}");
            }
            return new KeyValuePair<ParameterType, string>(parameterType, string.Join("=", parts.Skip(1)).Trim());
        }
    }
}
