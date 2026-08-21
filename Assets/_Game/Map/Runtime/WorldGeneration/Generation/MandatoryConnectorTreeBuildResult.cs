using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace StarNight.Map.WorldGeneration.Generation
{
    public enum MandatoryConnectorTreeBuildStatus
    {
        Completed,
        InvalidInput
    }

    public sealed class MandatoryConnectorTreeBuildResult
    {
        private readonly IReadOnlyList<MandatoryConnectorTreeBuildError> errors;

        public MandatoryConnectorTreeBuildResult(MandatoryConnectorTreeBuildStatus status, MandatoryConnectorTree tree, MandatoryConnectorTreeDiagnostics diagnostics, IEnumerable<MandatoryConnectorTreeBuildError> errors)
        {
            if (status != MandatoryConnectorTreeBuildStatus.Completed && status != MandatoryConnectorTreeBuildStatus.InvalidInput) throw new ArgumentOutOfRangeException(nameof(status));
            if (errors == null) throw new ArgumentNullException(nameof(errors));
            var values = new List<MandatoryConnectorTreeBuildError>(errors);
            if (status == MandatoryConnectorTreeBuildStatus.Completed && (tree == null || diagnostics == null || values.Count != 0)) throw new ArgumentException("Completed result shape is invalid.");
            if (status == MandatoryConnectorTreeBuildStatus.InvalidInput && (tree != null || diagnostics != null || values.Count == 0)) throw new ArgumentException("Invalid result shape is invalid.");
            Status = status;
            Tree = tree;
            Diagnostics = diagnostics;
            this.errors = new ReadOnlyCollection<MandatoryConnectorTreeBuildError>(values);
        }

        public MandatoryConnectorTreeBuildStatus Status { get; }
        public MandatoryConnectorTree Tree { get; }
        public MandatoryConnectorTreeDiagnostics Diagnostics { get; }
        public IReadOnlyList<MandatoryConnectorTreeBuildError> Errors => errors;
        public bool Succeeded => Status == MandatoryConnectorTreeBuildStatus.Completed;
        public bool RetryRequired => false;
    }
}
