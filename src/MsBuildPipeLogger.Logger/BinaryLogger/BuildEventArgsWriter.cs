﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Framework.Profiler;

namespace Microsoft.Build.Logging
{
    /// <summary>
    /// Serializes BuildEventArgs-derived objects into a provided BinaryWriter.
    /// </summary>
    internal class BuildEventArgsWriter
    {
        private readonly BinaryWriter _binaryWriter;

        /// <summary>
        /// Initializes a new instance of BuildEventArgsWriter with a BinaryWriter.
        /// </summary>
        /// <param name="binaryWriter">A BinaryWriter to write the BuildEventArgs instances to.</param>
        public BuildEventArgsWriter(BinaryWriter binaryWriter)
        {
            _binaryWriter = binaryWriter;
        }

        /// <summary>
        /// Write a provided instance of BuildEventArgs to the BinaryWriter.
        /// </summary>
        public void Write(BuildEventArgs e)
        {
            string type = e.GetType().Name;

            // the cases are ordered by most used first for performance
            if (e is BuildMessageEventArgs && type != "ProjectImportedEventArgs" && type != "TargetSkippedEventArgs")
            {
                Write((BuildMessageEventArgs)e);
            }
            else if (e is TaskStartedEventArgs taskStartedEventArgs)
            {
                Write(taskStartedEventArgs);
            }
            else if (e is TaskFinishedEventArgs taskFinishedEventArgs)
            {
                Write(taskFinishedEventArgs);
            }
            else if (e is TargetStartedEventArgs targetStartedEventArgs)
            {
                Write(targetStartedEventArgs);
            }
            else if (e is TargetFinishedEventArgs targetFinishedEventArgs)
            {
                Write(targetFinishedEventArgs);
            }
            else if (e is BuildErrorEventArgs buildErrorEventArgs)
            {
                Write(buildErrorEventArgs);
            }
            else if (e is BuildWarningEventArgs buildWarningEventArgs)
            {
                Write(buildWarningEventArgs);
            }
            else if (e is ProjectStartedEventArgs projectStartedEventArgs)
            {
                Write(projectStartedEventArgs);
            }
            else if (e is ProjectFinishedEventArgs projectFinishedEventArgs)
            {
                Write(projectFinishedEventArgs);
            }
            else if (e is BuildStartedEventArgs buildStartedEventArgs)
            {
                Write(buildStartedEventArgs);
            }
            else if (e is BuildFinishedEventArgs buildFinishedEventArgs)
            {
                Write(buildFinishedEventArgs);
            }
            else if (e is ProjectEvaluationStartedEventArgs projectEvaluationStartedEventArgs)
            {
                Write(projectEvaluationStartedEventArgs);
            }
            else if (e is ProjectEvaluationFinishedEventArgs projectEvaluationFinishedEventArgs)
            {
                Write(projectEvaluationFinishedEventArgs);
            }

            // The following cases are due to the fact that StructuredLogger.dll
            // only references MSBuild 14.0 .dlls. The following BuildEventArgs types
            // were only introduced in MSBuild 15.3 so we can't refer to them statically.
            // To still provide a good experience to those who are using the BinaryLogger
            // from StructuredLogger.dll against MSBuild 15.3 or later we need to preserve
            // these new events, so use reflection to create our "equivalents" of those
            // and populate them to be binary identical to the originals. Then serialize
            // our copies so that it's impossible to tell what wrote these.
            else if (type == "ProjectEvaluationStartedEventArgs")
            {
                ProjectEvaluationStartedEventArgs evaluationStarted = new ProjectEvaluationStartedEventArgs(e.Message)
                {
                    BuildEventContext = e.BuildEventContext,
                    ProjectFile = Reflector.GetProjectFileFromEvaluationStarted(e)
                };
                Write(evaluationStarted);
            }
            else if (type == "ProjectEvaluationFinishedEventArgs")
            {
                ProjectEvaluationFinishedEventArgs evaluationFinished = new ProjectEvaluationFinishedEventArgs(e.Message)
                {
                    BuildEventContext = e.BuildEventContext,
                    ProjectFile = Reflector.GetProjectFileFromEvaluationFinished(e)
                };
                Write(evaluationFinished);
            }
            else if (type == "ProjectImportedEventArgs")
            {
                BuildMessageEventArgs message = e as BuildMessageEventArgs;
                ProjectImportedEventArgs projectImported = new ProjectImportedEventArgs(message.LineNumber, message.ColumnNumber, e.Message)
                {
                    BuildEventContext = e.BuildEventContext,
                    ProjectFile = message.ProjectFile,
                    ImportedProjectFile = Reflector.GetImportedProjectFile(e),
                    UnexpandedProject = Reflector.GetUnexpandedProject(e)
                };
                Write(projectImported);
            }
            else if (type == "TargetSkippedEventArgs")
            {
                BuildMessageEventArgs message = e as BuildMessageEventArgs;
                TargetSkippedEventArgs targetSkipped = new TargetSkippedEventArgs(e.Message)
                {
                    BuildEventContext = e.BuildEventContext,
                    ProjectFile = message.ProjectFile,
                    TargetName = Reflector.GetTargetNameFromTargetSkipped(e),
                    TargetFile = Reflector.GetTargetFileFromTargetSkipped(e),
                    ParentTarget = Reflector.GetParentTargetFromTargetSkipped(e),
                    BuildReason = Reflector.GetBuildReasonFromTargetSkipped(e)
                };
                Write(targetSkipped);
            }
            else
            {
                // convert all unrecognized objects to message
                // and just preserve the message
                BuildMessageEventArgs buildMessageEventArgs = new BuildMessageEventArgs(
                    e.Message,
                    e.HelpKeyword,
                    e.SenderName,
                    MessageImportance.Normal,
                    e.Timestamp)
                {
                    BuildEventContext = e.BuildEventContext ?? BuildEventContext.Invalid
                };
                Write(buildMessageEventArgs);
            }
        }

        public void WriteBlob(BinaryLogRecordKind kind, byte[] bytes)
        {
            Write(kind);
            Write(bytes.Length);
            Write(bytes);
        }

        private void Write(BuildStartedEventArgs e)
        {
            Write(BinaryLogRecordKind.BuildStarted);
            WriteBuildEventArgsFields(e);
            Write(e.BuildEnvironment);
        }

        private void Write(BuildFinishedEventArgs e)
        {
            Write(BinaryLogRecordKind.BuildFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
        }

        private void Write(ProjectEvaluationStartedEventArgs e)
        {
            Write(BinaryLogRecordKind.ProjectEvaluationStarted);
            WriteBuildEventArgsFields(e);
            Write(e.ProjectFile);
        }

        private void Write(ProjectEvaluationFinishedEventArgs e)
        {
            Write(BinaryLogRecordKind.ProjectEvaluationFinished);

            WriteBuildEventArgsFields(e);
            Write(e.ProjectFile);

            Write(e.ProfilerResult.HasValue);
            if (e.ProfilerResult.HasValue)
            {
                Write(e.ProfilerResult.Value.ProfiledLocations.Count);

                foreach (KeyValuePair<EvaluationLocation, ProfiledLocation> item in e.ProfilerResult.Value.ProfiledLocations)
                {
                    Write(item.Key);
                    Write(item.Value);
                }
            }
        }

        private void Write(ProjectStartedEventArgs e)
        {
            Write(BinaryLogRecordKind.ProjectStarted);
            WriteBuildEventArgsFields(e);

            if (e.ParentProjectBuildEventContext == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                Write(e.ParentProjectBuildEventContext);
            }

            WriteOptionalString(e.ProjectFile);

            Write(e.ProjectId);
            Write(e.TargetNames);
            WriteOptionalString(e.ToolsVersion);

            if (e.GlobalProperties == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                Write(e.GlobalProperties);
            }

            WriteProperties(e.Properties);

            WriteItems(e.Items);
        }

        private void Write(ProjectFinishedEventArgs e)
        {
            Write(BinaryLogRecordKind.ProjectFinished);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.ProjectFile);
            Write(e.Succeeded);
        }

        private void Write(TargetStartedEventArgs e)
        {
            Write(BinaryLogRecordKind.TargetStarted);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.TargetName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TargetFile);
            WriteOptionalString(e.ParentTarget);
            Write((int)Reflector.GetBuildReasonFromTargetStarted(e));
        }

        private void Write(TargetFinishedEventArgs e)
        {
            Write(BinaryLogRecordKind.TargetFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TargetFile);
            WriteOptionalString(e.TargetName);
            WriteItemList(e.TargetOutputs);
        }

        private void Write(TaskStartedEventArgs e)
        {
            Write(BinaryLogRecordKind.TaskStarted);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.TaskName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TaskFile);
        }

        private void Write(TaskFinishedEventArgs e)
        {
            Write(BinaryLogRecordKind.TaskFinished);
            WriteBuildEventArgsFields(e);
            Write(e.Succeeded);
            WriteOptionalString(e.TaskName);
            WriteOptionalString(e.ProjectFile);
            WriteOptionalString(e.TaskFile);
        }

        private void Write(BuildErrorEventArgs e)
        {
            Write(BinaryLogRecordKind.Error);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.Subcategory);
            WriteOptionalString(e.Code);
            WriteOptionalString(e.File);
            WriteOptionalString(e.ProjectFile);
            Write(e.LineNumber);
            Write(e.ColumnNumber);
            Write(e.EndLineNumber);
            Write(e.EndColumnNumber);
        }

        private void Write(BuildWarningEventArgs e)
        {
            Write(BinaryLogRecordKind.Warning);
            WriteBuildEventArgsFields(e);
            WriteOptionalString(e.Subcategory);
            WriteOptionalString(e.Code);
            WriteOptionalString(e.File);
            WriteOptionalString(e.ProjectFile);
            Write(e.LineNumber);
            Write(e.ColumnNumber);
            Write(e.EndLineNumber);
            Write(e.EndColumnNumber);
        }

        private void Write(BuildMessageEventArgs e)
        {
            if (e is CriticalBuildMessageEventArgs criticalBuildMessageEventArgs)
            {
                Write(criticalBuildMessageEventArgs);
                return;
            }

            if (e is TaskCommandLineEventArgs taskCommandLineEventArgs)
            {
                Write(taskCommandLineEventArgs);
                return;
            }

            if (e is ProjectImportedEventArgs projectImportedEventArgs)
            {
                Write(projectImportedEventArgs);
                return;
            }

            if (e is TargetSkippedEventArgs targetSkippedEventArgs)
            {
                Write(targetSkippedEventArgs);
                return;
            }

            Write(BinaryLogRecordKind.Message);
            WriteMessageFields(e);
        }

        private void Write(ProjectImportedEventArgs e)
        {
            Write(BinaryLogRecordKind.ProjectImported);
            WriteMessageFields(e);
            Write(e.ImportIgnored);
            WriteOptionalString(e.ImportedProjectFile);
            WriteOptionalString(e.UnexpandedProject);
        }

        private void Write(TargetSkippedEventArgs e)
        {
            Write(BinaryLogRecordKind.TargetSkipped);
            WriteMessageFields(e);
            WriteOptionalString(e.TargetFile);
            WriteOptionalString(e.TargetName);
            WriteOptionalString(e.ParentTarget);
            Write((int)e.BuildReason);
        }

        private void Write(CriticalBuildMessageEventArgs e)
        {
            Write(BinaryLogRecordKind.CriticalBuildMessage);
            WriteMessageFields(e);
        }

        private void Write(TaskCommandLineEventArgs e)
        {
            Write(BinaryLogRecordKind.TaskCommandLine);
            WriteMessageFields(e);
            WriteOptionalString(e.CommandLine);
            WriteOptionalString(e.TaskName);
        }

        private void WriteBuildEventArgsFields(BuildEventArgs e)
        {
            BuildEventArgsFieldFlags flags = GetBuildEventArgsFieldFlags(e);
            Write((int)flags);
            WriteBaseFields(e, flags);
        }

        private void WriteBaseFields(BuildEventArgs e, BuildEventArgsFieldFlags flags)
        {
            if ((flags & BuildEventArgsFieldFlags.Message) != 0)
            {
                Write(e.Message);
            }

            if ((flags & BuildEventArgsFieldFlags.BuildEventContext) != 0)
            {
                Write(e.BuildEventContext);
            }

            if ((flags & BuildEventArgsFieldFlags.ThreadId) != 0)
            {
                Write(e.ThreadId);
            }

            if ((flags & BuildEventArgsFieldFlags.HelpHeyword) != 0)
            {
                Write(e.HelpKeyword);
            }

            if ((flags & BuildEventArgsFieldFlags.SenderName) != 0)
            {
                Write(e.SenderName);
            }

            if ((flags & BuildEventArgsFieldFlags.Timestamp) != 0)
            {
                Write(e.Timestamp);
            }
        }

        private void WriteMessageFields(BuildMessageEventArgs e)
        {
            BuildEventArgsFieldFlags flags = GetBuildEventArgsFieldFlags(e);
            flags = GetMessageFlags(e, flags);

            Write((int)flags);

            WriteBaseFields(e, flags);

            if ((flags & BuildEventArgsFieldFlags.Subcategory) != 0)
            {
                Write(e.Subcategory);
            }

            if ((flags & BuildEventArgsFieldFlags.Code) != 0)
            {
                Write(e.Code);
            }

            if ((flags & BuildEventArgsFieldFlags.File) != 0)
            {
                Write(e.File);
            }

            if ((flags & BuildEventArgsFieldFlags.ProjectFile) != 0)
            {
                Write(e.ProjectFile);
            }

            if ((flags & BuildEventArgsFieldFlags.LineNumber) != 0)
            {
                Write(e.LineNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.ColumnNumber) != 0)
            {
                Write(e.ColumnNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.EndLineNumber) != 0)
            {
                Write(e.EndLineNumber);
            }

            if ((flags & BuildEventArgsFieldFlags.EndColumnNumber) != 0)
            {
                Write(e.EndColumnNumber);
            }

            Write((int)e.Importance);
        }

        private static BuildEventArgsFieldFlags GetMessageFlags(BuildMessageEventArgs e, BuildEventArgsFieldFlags flags)
        {
            if (e.Subcategory != null)
            {
                flags |= BuildEventArgsFieldFlags.Subcategory;
            }

            if (e.Code != null)
            {
                flags |= BuildEventArgsFieldFlags.Code;
            }

            if (e.File != null)
            {
                flags |= BuildEventArgsFieldFlags.File;
            }

            if (e.ProjectFile != null)
            {
                flags |= BuildEventArgsFieldFlags.ProjectFile;
            }

            if (e.LineNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.LineNumber;
            }

            if (e.ColumnNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.ColumnNumber;
            }

            if (e.EndLineNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.EndLineNumber;
            }

            if (e.EndColumnNumber != 0)
            {
                flags |= BuildEventArgsFieldFlags.EndColumnNumber;
            }

            return flags;
        }

        private static BuildEventArgsFieldFlags GetBuildEventArgsFieldFlags(BuildEventArgs e)
        {
            BuildEventArgsFieldFlags flags = BuildEventArgsFieldFlags.None;
            if (e.BuildEventContext != null)
            {
                flags |= BuildEventArgsFieldFlags.BuildEventContext;
            }

            if (e.HelpKeyword != null)
            {
                flags |= BuildEventArgsFieldFlags.HelpHeyword;
            }

            if (!string.IsNullOrEmpty(e.Message))
            {
                flags |= BuildEventArgsFieldFlags.Message;
            }

            // no need to waste space for the default sender name
            if (e.SenderName != null && e.SenderName != "MSBuild")
            {
                flags |= BuildEventArgsFieldFlags.SenderName;
            }

            if (e.ThreadId > 0)
            {
                flags |= BuildEventArgsFieldFlags.ThreadId;
            }

            if (e.Timestamp != default(DateTime))
            {
                flags |= BuildEventArgsFieldFlags.Timestamp;
            }

            return flags;
        }

        private void WriteItemList(IEnumerable items)
        {
            if (items is IEnumerable<ITaskItem> taskItems)
            {
                Write(taskItems.Count());

                foreach (ITaskItem item in taskItems)
                {
                    Write(item);
                }

                return;
            }

            Write(0);
        }

        private void WriteItems(IEnumerable items)
        {
            if (items == null)
            {
                Write(0);
                return;
            }

            DictionaryEntry[] entries = items.OfType<DictionaryEntry>()
                .Where(e => e.Key is string && e.Value is ITaskItem)
                .ToArray();
            Write(entries.Length);

            foreach (DictionaryEntry entry in entries)
            {
                string key = entry.Key as string;
                ITaskItem item = entry.Value as ITaskItem;
                Write(key);
                Write(item);
            }
        }

        private void Write(ITaskItem item)
        {
            Write(item.ItemSpec);
            IDictionary customMetadata = item.CloneCustomMetadata();
            Write(customMetadata.Count);

            foreach (string metadataName in customMetadata.Keys)
            {
                Write(metadataName);
                Write(item.GetMetadata(metadataName));
            }
        }

        private void WriteProperties(IEnumerable properties)
        {
            if (properties == null)
            {
                Write(0);
                return;
            }

            // there are no guarantees that the properties iterator won't change, so
            // take a snapshot and work with the readonly copy
            DictionaryEntry[] propertiesArray = properties.OfType<DictionaryEntry>().ToArray();

            Write(propertiesArray.Length);

            foreach (DictionaryEntry entry in propertiesArray)
            {
                if (entry.Key is string && entry.Value is string)
                {
                    Write((string)entry.Key);
                    Write((string)entry.Value);
                }
                else
                {
                    // to keep the count accurate
                    Write(string.Empty);
                    Write(string.Empty);
                }
            }
        }

        private void Write(BuildEventContext buildEventContext)
        {
            Write(buildEventContext.NodeId);
            Write(buildEventContext.ProjectContextId);
            Write(buildEventContext.TargetId);
            Write(buildEventContext.TaskId);
            Write(buildEventContext.SubmissionId);
            Write(buildEventContext.ProjectInstanceId);
            Write(Reflector.GetEvaluationId(buildEventContext));
        }

        private void Write<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> keyValuePairs)
        {
            if (keyValuePairs?.Any() == true)
            {
                Write(keyValuePairs.Count());
                foreach (KeyValuePair<TKey, TValue> kvp in keyValuePairs)
                {
                    Write(kvp.Key.ToString());
                    Write(kvp.Value.ToString());
                }
            }
            else
            {
                Write(false);
            }
        }

        private void Write(BinaryLogRecordKind kind)
        {
            Write((int)kind);
        }

        private void Write(int value)
        {
            Write7BitEncodedInt(_binaryWriter, value);
        }

        private void Write(long value)
        {
            _binaryWriter.Write(value);
        }

        private void Write7BitEncodedInt(BinaryWriter writer, int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                writer.Write((byte)(v | 0x80));
                v >>= 7;
            }
            writer.Write((byte)v);
        }

        private void Write(byte[] bytes)
        {
            _binaryWriter.Write(bytes);
        }

        private void Write(bool boolean)
        {
            _binaryWriter.Write(boolean);
        }

        private void Write(string text)
        {
            if (text != null)
            {
                _binaryWriter.Write(text);
            }
            else
            {
                _binaryWriter.Write(false);
            }
        }

        private void WriteOptionalString(string text)
        {
            if (text == null)
            {
                Write(false);
            }
            else
            {
                Write(true);
                Write(text);
            }
        }

        private void Write(DateTime timestamp)
        {
            _binaryWriter.Write(timestamp.Ticks);
            Write((int)timestamp.Kind);
        }

        private void Write(TimeSpan timeSpan)
        {
            _binaryWriter.Write(timeSpan.Ticks);
        }

        private void Write(EvaluationLocation item)
        {
            WriteOptionalString(item.ElementName);
            WriteOptionalString(item.ElementDescription);
            WriteOptionalString(item.EvaluationPassDescription);
            WriteOptionalString(item.File);
            Write((int)item.Kind);
            Write((int)item.EvaluationPass);

            Write(item.Line.HasValue);
            if (item.Line.HasValue)
            {
                Write(item.Line.Value);
            }

            Write(item.Id);
            Write(item.ParentId.HasValue);
            if (item.ParentId.HasValue)
            {
                Write(item.ParentId.Value);
            }
        }

        private void Write(ProfiledLocation e)
        {
            Write(e.NumberOfHits);
            Write(e.ExclusiveTime);
            Write(e.InclusiveTime);
        }
    }
}
