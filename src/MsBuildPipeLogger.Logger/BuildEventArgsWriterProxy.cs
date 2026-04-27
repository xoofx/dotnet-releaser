using System;
using System.IO;
using System.Reflection;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace MsBuildPipeLogger
{
    internal class BuildEventArgsWriterProxy
    {
        private const BindingFlags InstanceMemberFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const string BuildEventArgsWriterTypeName = "Microsoft.Build.Logging.BuildEventArgsWriter";

        private readonly Action<BuildEventArgs> _write;

        public BuildEventArgsWriterProxy(BinaryWriter writer)
        {
            Type buildEventArgsWriter = GetBuildEventArgsWriterType();
            ConstructorInfo writerConstructor = buildEventArgsWriter.GetConstructor(
                InstanceMemberFlags,
                null,
                new[] { typeof(BinaryWriter) },
                null);
            if (writerConstructor == null)
            {
                throw new MissingMethodException(BuildEventArgsWriterTypeName, ".ctor(System.IO.BinaryWriter)");
            }

            object argsWriter = writerConstructor.Invoke(new object[] { writer });

            MethodInfo writeMethod = buildEventArgsWriter.GetMethod(
                "Write",
                InstanceMemberFlags,
                null,
                new[] { typeof(BuildEventArgs) },
                null);
            if (writeMethod == null || writeMethod.ReturnType != typeof(void))
            {
                throw new MissingMethodException(
                    BuildEventArgsWriterTypeName,
                    "Write(Microsoft.Build.Framework.BuildEventArgs)");
            }

            _write = (Action<BuildEventArgs>)writeMethod.CreateDelegate(typeof(Action<BuildEventArgs>), argsWriter);
        }

        public void Write(BuildEventArgs e) => _write(e);

        private static Type GetBuildEventArgsWriterType()
        {
            Assembly msBuildAssembly = typeof(BinaryLogger).GetTypeInfo().Assembly;
            Type buildEventArgsWriter = msBuildAssembly.GetType(BuildEventArgsWriterTypeName);
            if (buildEventArgsWriter == null)
            {
                throw new TypeLoadException(
                    $"Could not load type '{BuildEventArgsWriterTypeName}' from assembly '{msBuildAssembly.FullName}'. " +
                    "The MSBuild binary logger implementation may have changed.");
            }

            return buildEventArgsWriter;
        }
    }
}
