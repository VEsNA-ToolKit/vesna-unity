using System;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace script.environment
{
    
    [ExecuteInEditMode]
    public class SnapPointArtifact : Artifact
    {
        [SerializeField]
        public bool snapToSurface;

        protected override void Awake()
        {
            artifactType = ArtifactTypeEnum.SnapPoint;
            base.Awake();
        }

        public SnapPointArtifact(bool snapToSurface)
        {
            this.snapToSurface = snapToSurface;
        }
        
        private void Update()
        {
            // TODO: We could add a custom button in the inspector to trigger this action.
            // This would mean adding a custom editor script, though, which might be overkill for now.
            if (!snapToSurface) return;
            
            SnapToSurface();
            snapToSurface = false; // Reset the flag after snapping
        }
        
        private void SnapToSurface()
        {
            var     transformPosition = transform.position;
            var     rayDirection = Vector3.down;
            var     layerMask = LayerMask.GetMask("Default");
            
            // Cast a ray downward to find the surface below
            if (Physics.Raycast(transformPosition, rayDirection.normalized, out var hit, RayDistance, layerMask))
                transform.position = hit.point;
        }

        private const float RayDistance = 10f; // Maximum distance to check for a surface below
    }
}
