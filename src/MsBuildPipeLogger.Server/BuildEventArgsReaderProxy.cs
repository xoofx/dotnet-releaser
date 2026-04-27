using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    internal class BuildEventArgsReaderProxy
    {
        private const BindingFlags InstanceMemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string BuildEventArgsReaderTypeName = "Microsoft.Build.Logging.BuildEventArgsReader";

        private readonly Func<BuildEventArgs> _read;

        public BuildEventArgsReaderProxy(BinaryReader reader)
        {
            Type buildEventArgsReader = GetBuildEventArgsReaderType();
            object argsReader = CreateBuildEventArgsReader(buildEventArgsReader, reader);
            MethodInfo readMethod = buildEventArgsReader.GetMethod(
                "Read",
                InstanceMemberFlags,
                null,
                Type.EmptyTypes,
                null);
            if (readMethod == null || readMethod.ReturnType != typeof(BuildEventArgs))
            {
                throw new MissingMethodException(BuildEventArgsReaderTypeName, "Read()");
            }

            _read = (Func<BuildEventArgs>)readMethod.CreateDelegate(typeof(Func<BuildEventArgs>), argsReader);
        }

        public BuildEventArgs Read() => _read();

        private static Type GetBuildEventArgsReaderType()
        {
            Assembly msBuildAssembly = typeof(BinaryLogger).GetTypeInfo().Assembly;
            Type buildEventArgsReader = msBuildAssembly.GetType(BuildEventArgsReaderTypeName);
            if (buildEventArgsReader == null)
            {
                throw new TypeLoadException(
                    $"Could not load type '{BuildEventArgsReaderTypeName}' from assembly '{msBuildAssembly.FullName}'. " +
                    "The MSBuild binary logger implementation may have changed.");
            }

            return buildEventArgsReader;
        }

        private static object CreateBuildEventArgsReader(Type buildEventArgsReader, BinaryReader reader)
        {
            ConstructorInfo readerConstructor = buildEventArgsReader.GetConstructor(
                InstanceMemberFlags,
                null,
                new[] { typeof(BinaryReader), typeof(int) },
                null);
            if (readerConstructor != null)
            {
                return readerConstructor.Invoke(new object[] { reader, GetBinaryLoggerFileFormatVersion() });
            }

            readerConstructor = buildEventArgsReader.GetConstructor(
                InstanceMemberFlags,
                null,
                new[] { typeof(BinaryReader) },
                null);
            if (readerConstructor != null)
            {
                return readerConstructor.Invoke(new object[] { reader });
            }

            throw new MissingMethodException(
                BuildEventArgsReaderTypeName,
                ".ctor(System.IO.BinaryReader) or .ctor(System.IO.BinaryReader, System.Int32)");
        }

        private static int GetBinaryLoggerFileFormatVersion()
        {
            FieldInfo fileFormatVersionField = typeof(BinaryLogger).GetField(
                "FileFormatVersion",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (fileFormatVersionField == null)
            {
                throw new MissingFieldException(typeof(BinaryLogger).FullName, "FileFormatVersion");
            }

            object fileFormatVersion = fileFormatVersionField.GetValue(null);
            if (!(fileFormatVersion is int))
            {
                throw new InvalidOperationException(
                    $"Field '{typeof(BinaryLogger).FullName}.FileFormatVersion' must be an integer.");
            }

            return (int)fileFormatVersion;
        }
    }
}
