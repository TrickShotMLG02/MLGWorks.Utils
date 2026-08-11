using System;
using UnityEngine;

namespace MLGWorks.Utils.Helpers.Collections
{
    /// <summary>Provides shared serialization lifecycle behavior for collection implementations.</summary>
    [System.Serializable]
    public abstract class SerializableCollectionBase : ISerializableCollection, ISerializationCallbackReceiver
    {
        private bool runtimeStateInitialized;

        /// <summary>Raised when the collection contents change.</summary>
        public event Action Changed;

        /// <summary>Raised after the runtime representation is rebuilt.</summary>
        public event Action CollectionRebuilt;

        /// <summary>Gets the number of runtime entries.</summary>
        public abstract int Count { get; }

        /// <summary>Rebuilds runtime data from the serialized representation.</summary>
        public void Rebuild()
        {
            RebuildRuntimeState();
            runtimeStateInitialized = true;
            CollectionRebuilt?.Invoke();
        }

        /// <summary>Validates the serialized representation without changing runtime data.</summary>
        /// <returns>The validation result.</returns>
        public abstract CollectionValidationResult Validate();

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (runtimeStateInitialized)
            {
                SynchronizeSerializedData();
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            RebuildRuntimeState();
            runtimeStateInitialized = true;
        }

        /// <summary>Ensures the runtime adapter has been initialized.</summary>
        protected void EnsureRuntimeState()
        {
            if (!runtimeStateInitialized)
            {
                RebuildRuntimeState();
                runtimeStateInitialized = true;
            }
        }

        /// <summary>Notifies subscribers that collection contents changed.</summary>
        protected void NotifyChanged() => Changed?.Invoke();

        /// <summary>Rebuilds the implementation-specific runtime adapter.</summary>
        protected abstract void RebuildRuntimeState();

        /// <summary>Copies runtime changes back into Unity-serialized data.</summary>
        protected abstract void SynchronizeSerializedData();
    }
}
