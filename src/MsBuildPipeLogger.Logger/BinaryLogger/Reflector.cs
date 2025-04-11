using System;
using System.Reflection;
using Microsoft.Build.Framework;
using MsBuildPipeLogger;

namespace Microsoft.Build.Logging
{
    /// <summary>
    /// This class accesses the properties on event args that were added in MSBuild 15.3.
    /// As the StructuredLogger.dll references MSBuild 14.0 and gracefully degrades
    /// when used with MSBuild 14.0, we have to use Reflection to dynamically
    /// retrieve the values if present and gracefully degrade if we're running with
    /// an earlier MSBuild.
    /// </summary>
    internal class Reflector
    {
        private static Func<BuildEventArgs, string> projectFileFromEvaluationStarted;
        private static Func<BuildEventArgs, string> projectFileFromEvaluationFinished;
        private static Func<BuildEventArgs, string> unexpandedProjectGetter;
        private static Func<BuildEventArgs, string> importedProjectFileGetter;
        private static Func<BuildEventContext, int> evaluationIdGetter;
        private static Func<BuildEventArgs, string> targetNameFromTargetSkipped;
        private static Func<BuildEventArgs, string> targetFileFromTargetSkipped;
        private static Func<BuildEventArgs, string> parentTargetFromTargetSkipped;
        private static Func<BuildEventArgs, TargetBuiltReason> buildReasonFromTargetSkipped;

        internal static string GetProjectFileFromEvaluationStarted(BuildEventArgs e)
        {
            if (projectFileFromEvaluationStarted == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("ProjectFile").GetGetMethod();
                projectFileFromEvaluationStarted = b => method.Invoke(b, null) as string;
            }

            return projectFileFromEvaluationStarted(e);
        }

        internal static string GetProjectFileFromEvaluationFinished(BuildEventArgs e)
        {
            if (projectFileFromEvaluationFinished == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("ProjectFile").GetGetMethod();
                projectFileFromEvaluationFinished = b => method.Invoke(b, null) as string;
            }

            return projectFileFromEvaluationFinished(e);
        }

        internal static string GetTargetNameFromTargetSkipped(BuildEventArgs e)
        {
            if (targetNameFromTargetSkipped == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("TargetName").GetGetMethod();
                targetNameFromTargetSkipped = b => method.Invoke(b, null) as string;
            }

            return targetNameFromTargetSkipped(e);
        }

        internal static string GetTargetFileFromTargetSkipped(BuildEventArgs e)
        {
            if (targetFileFromTargetSkipped == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("TargetFile").GetGetMethod();
                targetFileFromTargetSkipped = b => method.Invoke(b, null) as string;
            }

            return targetFileFromTargetSkipped(e);
        }

        internal static string GetParentTargetFromTargetSkipped(BuildEventArgs e)
        {
            if (parentTargetFromTargetSkipped == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("ParentTarget").GetGetMethod();
                parentTargetFromTargetSkipped = b => method.Invoke(b, null) as string;
            }

            return parentTargetFromTargetSkipped(e);
        }

        internal static TargetBuiltReason GetBuildReasonFromTargetStarted(BuildEventArgs e)
        {
            Type type = e.GetType();
            PropertyInfo property = type.GetProperty("BuildReason");
            if (property == null)
            {
                return TargetBuiltReason.None;
            }

            MethodInfo method = property.GetGetMethod();
            return (TargetBuiltReason)method.Invoke(e, null);
        }

        internal static TargetBuiltReason GetBuildReasonFromTargetSkipped(BuildEventArgs e)
        {
            if (buildReasonFromTargetSkipped == null)
            {
                Type type = e.GetType();
                PropertyInfo property = type.GetProperty("BuildReason");
                if (property == null)
                {
                    return TargetBuiltReason.None;
                }

                MethodInfo method = property.GetGetMethod();
                buildReasonFromTargetSkipped = b => (TargetBuiltReason)method.Invoke(b, null);
            }

            return buildReasonFromTargetSkipped(e);
        }

        internal static string GetUnexpandedProject(BuildEventArgs e)
        {
            if (unexpandedProjectGetter == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("UnexpandedProject").GetGetMethod();
                unexpandedProjectGetter = b => method.Invoke(b, null) as string;
            }

            return unexpandedProjectGetter(e);
        }

        internal static string GetImportedProjectFile(BuildEventArgs e)
        {
            if (importedProjectFileGetter == null)
            {
                Type type = e.GetType();
                MethodInfo method = type.GetProperty("ImportedProjectFile").GetGetMethod();
                importedProjectFileGetter = b => method.Invoke(b, null) as string;
            }

            return importedProjectFileGetter(e);
        }

        internal static int GetEvaluationId(BuildEventContext buildEventContext)
        {
            if (buildEventContext == null)
            {
                return -1;
            }

            if (evaluationIdGetter == null)
            {
                Type type = buildEventContext.GetType();
                FieldInfo field = type.GetField("_evaluationId"/*, BindingFlags.Instance | BindingFlags.NonPublic*/);
                if (field != null)
                {
                    evaluationIdGetter = b => (int)field.GetValue(b);
                }
                else
                {
                    evaluationIdGetter = b => b.ProjectContextId <= 0 ? -b.ProjectContextId : -1;
                }
            }

            return evaluationIdGetter(buildEventContext);
        }
    }
}
