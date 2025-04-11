﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Build.Framework
{
    /// <summary>
    /// Arguments for the project evaluation started event.
    /// </summary>
#if FEATURE_BINARY_SERIALIZATION
    [Serializable]
#endif
    public class ProjectEvaluationStartedEventArgs : BuildStatusEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the ProjectEvaluationStartedEventArgs class.
        /// </summary>
        public ProjectEvaluationStartedEventArgs()
        {
        }

        /// <summary>
        /// Initializes a new instance of the ProjectEvaluationStartedEventArgs class.
        /// </summary>
        public ProjectEvaluationStartedEventArgs(string message, params object[] messageArgs)
            : base(message, null, null, DateTime.UtcNow, messageArgs)
        {
        }

        /// <summary>
        /// Gets or sets the full path of the project that started evaluation.
        /// </summary>
        public string ProjectFile { get; set; }
    }
}