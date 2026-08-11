using System;
using System.Collections.Generic;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Describes one problem found in serialized collection data.</summary>
    public readonly struct CollectionValidationIssue
    {
        /// <summary>Gets the issue category.</summary>
        public CollectionValidationIssueType Type { get; }

        /// <summary>Gets the serialized entry index associated with the issue.</summary>
        public int Index { get; }

        /// <summary>Gets the severity of the issue.</summary>
        public CollectionValidationSeverity Severity { get; }

        /// <summary>Gets a human-readable explanation of the issue.</summary>
        public string Message { get; }

        /// <summary>Creates a validation issue.</summary>
        /// <param name="type">The issue category.</param>
        /// <param name="index">The affected serialized entry index.</param>
        /// <param name="message">The explanation shown to the user.</param>
        public CollectionValidationIssue(
            CollectionValidationIssueType type,
            int index,
            string message,
            CollectionValidationSeverity severity = CollectionValidationSeverity.Error)
        {
            Type = type;
            Index = index;
            Message = message ?? throw new ArgumentNullException(nameof(message));
            Severity = severity;
        }
    }

    /// <summary>Contains validation issues found in a serializable collection.</summary>
    public sealed class CollectionValidationResult
    {
        private readonly List<CollectionValidationIssue> issues;

        /// <summary>Gets whether the collection contains no errors.</summary>
        public bool IsValid => !HasErrors;

        /// <summary>Gets whether the collection contains one or more errors.</summary>
        public bool HasErrors => HasSeverity(CollectionValidationSeverity.Error);

        /// <summary>Gets whether the collection contains one or more warnings.</summary>
        public bool HasWarnings => HasSeverity(CollectionValidationSeverity.Warning);

        /// <summary>Gets whether the collection contains informational issues.</summary>
        public bool HasInformation => HasSeverity(CollectionValidationSeverity.Information);

        /// <summary>Gets the reported issues in serialized order.</summary>
        public IReadOnlyList<CollectionValidationIssue> Issues => issues;

        internal CollectionValidationResult(List<CollectionValidationIssue> issues)
        {
            this.issues = issues ?? throw new ArgumentNullException(nameof(issues));
        }

        private bool HasSeverity(CollectionValidationSeverity severity)
        {
            for (int index = 0; index < issues.Count; index++)
            {
                if (issues[index].Severity == severity)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
