﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//-----------------------------------------------------------------------
// </copyright>
// <summary>The result profiling an evaluation.</summary>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Microsoft.Build.Framework.Profiler
{
    /// <summary>
    /// Result of profiling an evaluation.
    /// </summary>
#if FEATURE_BINARY_SERIALIZATION
    [Serializable]
#endif
    public struct ProfilerResult
    {
        /// <nodoc/>
        public IReadOnlyDictionary<EvaluationLocation, ProfiledLocation> ProfiledLocations { get; }

        /// <nodoc/>
        public ProfilerResult(IDictionary<EvaluationLocation, ProfiledLocation> profiledLocations)
        {
            ProfiledLocations = new ReadOnlyDictionary<EvaluationLocation, ProfiledLocation>(profiledLocations);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is ProfilerResult))
            {
                return false;
            }

            ProfilerResult result = (ProfilerResult)obj;

            return (ProfiledLocations == result.ProfiledLocations)
                   || (ProfiledLocations.Count == result.ProfiledLocations.Count
                    && !ProfiledLocations.Except(result.ProfiledLocations).Any());
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return ProfiledLocations.Keys.Aggregate(0, (acum, location) => acum + location.GetHashCode());
        }
    }

    /// <summary>
    /// Result of timing the evaluation of a given element at a given location.
    /// </summary>
#if FEATURE_BINARY_SERIALIZATION
    [Serializable]
#endif
    public struct ProfiledLocation
    {
        /// <nodoc/>
        public TimeSpan InclusiveTime { get; }

        /// <nodoc/>
        public TimeSpan ExclusiveTime { get; }

        /// <nodoc/>
        public int NumberOfHits { get; }

        /// <nodoc/>
        public ProfiledLocation(TimeSpan inclusiveTime, TimeSpan exclusiveTime, int numberOfHits)
        {
            InclusiveTime = inclusiveTime;
            ExclusiveTime = exclusiveTime;
            NumberOfHits = numberOfHits;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (!(obj is ProfiledLocation))
            {
                return false;
            }

            ProfiledLocation location = (ProfiledLocation)obj;
            return InclusiveTime.Equals(location.InclusiveTime)
                   && ExclusiveTime.Equals(location.ExclusiveTime)
                   && NumberOfHits == location.NumberOfHits;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            int hashCode = -2131368567;
            hashCode = (hashCode * -1521134295) + base.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<TimeSpan>.Default.GetHashCode(InclusiveTime);
            hashCode = (hashCode * -1521134295) + EqualityComparer<TimeSpan>.Default.GetHashCode(ExclusiveTime);
            return (hashCode * -1521134295) + NumberOfHits.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"[{InclusiveTime} - {ExclusiveTime}]: {NumberOfHits} hits";
        }
    }
}
