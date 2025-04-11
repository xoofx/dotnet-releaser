// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>Location for different elements tracked by the evaluation profiler.</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace Microsoft.Build.Framework.Profiler
{
    /// <summary>
    /// Evaluation main phases used by the profiler.
    /// </summary>
    /// <remarks>
    /// Order matters since the profiler pretty printer orders profiled items from top to bottom using
    /// the pass they belong to.
    /// </remarks>
    public enum EvaluationPass : byte
    {
        TotalEvaluation = 0,
        TotalGlobbing = 1,
        InitialProperties = 2,
        Properties = 3,
        ItemDefinitionGroups = 4,
        Items = 5,
        LazyItems = 6,
        UsingTasks = 7,
        Targets = 8
    }

    /// <summary>
    /// The kind of the evaluated location being tracked.
    /// </summary>
    public enum EvaluationLocationKind : byte
    {
        Element = 0,
        Condition = 1,
        Glob = 2
    }

    /// <summary>
    /// Represents a location for different evaluation elements tracked by the EvaluationProfiler.
    /// </summary>
#if FEATURE_BINARY_SERIALIZATION
    [Serializable]
#endif
    public struct EvaluationLocation
    {
        /// <summary>
        /// Default descriptions for locations that are used in case a description is not provided.
        /// </summary>
        private static readonly Dictionary<EvaluationPass, string> PassDefaultDescription =
            new Dictionary<EvaluationPass, string>
            {
                { EvaluationPass.TotalEvaluation, "Total evaluation" },
                { EvaluationPass.TotalGlobbing, "Total evaluation for globbing" },
                { EvaluationPass.InitialProperties, "Initial properties (pass 0)" },
                { EvaluationPass.Properties, "Properties (pass 1)" },
                { EvaluationPass.ItemDefinitionGroups, "Item definition groups (pass 2)" },
                { EvaluationPass.Items, "Items (pass 3)" },
                { EvaluationPass.LazyItems, "Lazy items (pass 3.1)" },
                { EvaluationPass.UsingTasks, "Using tasks (pass 4)" },
                { EvaluationPass.Targets, "Targets (pass 5)" },
            };

        /// <nodoc/>
        public long Id { get; }

        /// <nodoc/>
        public long? ParentId { get; }

        /// <nodoc/>
        public EvaluationPass EvaluationPass { get; }

        /// <nodoc/>
        public string EvaluationPassDescription { get; }

        /// <nodoc/>
        public string File { get; }

        /// <nodoc/>
        public int? Line { get; }

        /// <nodoc/>
        public string ElementName { get; }

        /// <nodoc/>
        public string ElementDescription { get; }

        /// <nodoc/>
        public EvaluationLocationKind Kind { get; }

        /// <nodoc/>
        public bool IsEvaluationPass => File == null;

        /// <nodoc/>
        public static EvaluationLocation CreateLocationForCondition(
            long? parentId,
            EvaluationPass evaluationPass,
            string evaluationDescription,
            string file,
            int? line,
            string condition)
        {
            return new EvaluationLocation(parentId, evaluationPass, evaluationDescription, file, line, "Condition", condition, kind: EvaluationLocationKind.Condition);
        }

        /// <nodoc/>
        public static EvaluationLocation CreateLocationForGlob(
            long? parentId,
            EvaluationPass evaluationPass,
            string evaluationDescription,
            string file,
            int? line,
            string globDescription)
        {
            return new EvaluationLocation(parentId, evaluationPass, evaluationDescription, file, line, "Glob", globDescription, kind: EvaluationLocationKind.Glob);
        }

        /// <nodoc/>
        public static EvaluationLocation CreateLocationForAggregatedGlob()
        {
            return new EvaluationLocation(
                EvaluationPass.TotalGlobbing,
                PassDefaultDescription[EvaluationPass.TotalGlobbing],
                file: null,
                line: null,
                elementName: null,
                elementDescription: null,
                kind: EvaluationLocationKind.Glob);
        }

        /// <summary>
        /// Constructs a generic evaluation location.
        /// </summary>
        /// <remarks>
        /// Used by serialization/deserialization purposes.
        /// </remarks>
        public EvaluationLocation(
            long id,
            long? parentId,
            EvaluationPass evaluationPass,
            string evaluationPassDescription,
            string file,
            int? line,
            string elementName,
            string elementDescription,
            EvaluationLocationKind kind)
        {
            Id = id;
            ParentId = parentId == EmptyLocation.Id ? null : parentId; // The empty location doesn't count as a parent id, since it's just a dummy starting point
            EvaluationPass = evaluationPass;
            EvaluationPassDescription = evaluationPassDescription;
            File = file;
            Line = line;
            ElementName = elementName;
            ElementDescription = elementDescription;
            Kind = kind;
        }

        /// <summary>
        /// Constructs a generic evaluation location based on a (possibly null) parent Id.
        /// </summary>
        /// <remarks>
        /// A unique Id gets assigned automatically
        /// Used by serialization/deserialization purposes.
        /// </remarks>
        public EvaluationLocation(long? parentId, EvaluationPass evaluationPass, string evaluationPassDescription, string file, int? line, string elementName, string elementDescription, EvaluationLocationKind kind)
            : this(EvaluationIdProvider.GetNextId(), parentId, evaluationPass, evaluationPassDescription, file, line, elementName, elementDescription, kind)
        {
        }

        /// <summary>
        /// Constructs a generic evaluation location with no parent.
        /// </summary>
        /// <remarks>
        /// A unique Id gets assigned automatically
        /// Used by serialization/deserialization purposes.
        /// </remarks>
        public EvaluationLocation(EvaluationPass evaluationPass, string evaluationPassDescription, string file, int? line, string elementName, string elementDescription, EvaluationLocationKind kind)
            : this(null, evaluationPass, evaluationPassDescription, file, line, elementName, elementDescription, kind)
        {
        }

        /// <summary>
        /// An empty location, used as the starting instance.
        /// </summary>
        public static EvaluationLocation EmptyLocation { get; } = CreateEmptyLocation();

        /// <nodoc/>
        public EvaluationLocation WithEvaluationPass(EvaluationPass evaluationPass, string passDescription = null)
        {
            return new EvaluationLocation(
                Id,
                evaluationPass,
                passDescription ?? PassDefaultDescription[evaluationPass],
                File,
                Line,
                ElementName,
                ElementDescription,
                Kind);
        }

        /// <nodoc/>
        public EvaluationLocation WithParentId(long? parentId)
        {
            // Simple optimization. If the new parent id is the same as the current one, then we just return this
            if (parentId == ParentId)
            {
                return this;
            }

            return new EvaluationLocation(
                Id,
                parentId,
                EvaluationPass,
                EvaluationPassDescription,
                File,
                Line,
                ElementName,
                ElementDescription,
                Kind);
        }

        /// <nodoc/>
        public EvaluationLocation WithFile(string file)
        {
            return new EvaluationLocation(Id, EvaluationPass, EvaluationPassDescription, file, null, null, null, Kind);
        }

        /// <nodoc/>
        public EvaluationLocation WithFileLineAndCondition(string file, int? line, string condition)
        {
            return CreateLocationForCondition(Id, EvaluationPass, EvaluationPassDescription, file, line, condition);
        }

        /// <nodoc/>
        public EvaluationLocation WithGlob(string globDescription)
        {
            return CreateLocationForGlob(Id, EvaluationPass, EvaluationPassDescription, File, Line, globDescription);
        }

        /// <nodoc/>
        public override bool Equals(object obj)
        {
            if (obj is EvaluationLocation evaluationLocation)
            {
                EvaluationLocation other = evaluationLocation;
                return
                    Id == other.Id
                    && ParentId == other.ParentId
                    && EvaluationPass == other.EvaluationPass
                    && EvaluationPassDescription == other.EvaluationPassDescription
                    && string.Equals(File, other.File, StringComparison.OrdinalIgnoreCase)
                    && Line == other.Line
                    && ElementName == other.ElementName
                    && ElementDescription == other.ElementDescription
                    && Kind == other.Kind;
            }
            return false;
        }

        /// <nodoc/>
        public override string ToString()
        {
            return
                $"{Id}\t{ParentId?.ToString() ?? string.Empty}\t{EvaluationPassDescription ?? string.Empty}\t{File ?? string.Empty}\t{Line?.ToString() ?? string.Empty}\t{ElementName ?? string.Empty}\tDescription:{ElementDescription}\t{EvaluationPassDescription}";
        }

        /// <nodoc/>
        public override int GetHashCode()
        {
            int hashCode = 1198539463;
            hashCode = (hashCode * -1521134295) + base.GetHashCode();
            hashCode = (hashCode * -1521134295) + Id.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<long?>.Default.GetHashCode(ParentId);
            hashCode = (hashCode * -1521134295) + EvaluationPass.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(EvaluationPassDescription);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(File);
            hashCode = (hashCode * -1521134295) + EqualityComparer<int?>.Default.GetHashCode(Line);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ElementName);
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(ElementDescription);
            return (hashCode * -1521134295) + Kind.GetHashCode();
        }

        private static EvaluationLocation CreateEmptyLocation()
        {
            return new EvaluationLocation(
                EvaluationIdProvider.GetNextId(),
                null,
                default(EvaluationPass),
                null,
                null,
                null,
                null,
                null,
                default(EvaluationLocationKind));
        }
    }
}
