namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Describes the severity of a serialized collection validation issue.</summary>
    public enum CollectionValidationSeverity
    {
        /// <summary>The data cannot be used safely.</summary>
        Error,
        /// <summary>The data can be resolved by a configured policy.</summary>
        Warning,
        /// <summary>The data is valid and communicates intentional behavior.</summary>
        Information
    }

    /// <summary>Specifies how duplicate serialized dictionary keys are handled.</summary>
    public enum DuplicateKeyPolicy
    {
        /// <summary>Keeps the first value and ignores later duplicates.</summary>
        KeepFirst,
        /// <summary>Replaces the value with the last duplicate.</summary>
        KeepLast,
        /// <summary>Ignores duplicates and reports a validation issue.</summary>
        Reject
    }

    /// <summary>Identifies the kind of validation problem found in serialized data.</summary>
    public enum CollectionValidationIssueType
    {
        /// <summary>A required key or item is null.</summary>
        NullValue,
        /// <summary>A key occurs more than once.</summary>
        DuplicateKey,
        /// <summary>A collection entry is malformed.</summary>
        InvalidEntry
    }
}
