namespace ShadowMaker
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// Component which blocks the light of a light emitter.
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightBlocker : MonoBehaviour
    {
        /// <summary>
        /// Static list of all the active blockers in the scene.
        /// </summary>
        private static List<LightBlocker> blockerList = new List<LightBlocker>();

        /// <summary>
        /// The light blocker mesh.
        /// </summary>
        private ShadowMaker.Core.LightBlockerMesh lightBlockerMesh = null;

        /// <summary>
        /// Gets the list of active blockers.
        /// </summary>
        /// <returns>The list of active blockers.</returns>
        public static List<LightBlocker> GetActiveBlockerList()
        {
            return LightBlocker.blockerList;
        }

        /// <summary>
        /// Gets the render mesh.
        /// </summary>
        /// <returns>The render mesh.</returns>
        public Mesh GetRenderMesh()
        {
            return this.gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        /// <summary>
        /// Gets the blocker mesh (used when rendering shadow map).
        /// The blocker is generated if it does not exist or if the render mesh is changed.
        /// </summary>
        /// <returns>The blocker mesh.</returns>
        public Mesh GetBlockerMesh()
        {
            // If blocker never has been generated.
            if (this.lightBlockerMesh == null)
            {
                this.lightBlockerMesh = new Core.LightBlockerMesh(this.GetRenderMesh());
            }

            // If render mesh has been changed, blocker mesh needs to be updated.
            if (this.GetRenderMesh() != this.lightBlockerMesh.GetBlockerMesh())
            {
                this.lightBlockerMesh.UpdateBlockerMesh(this.GetRenderMesh());
            }

            return this.lightBlockerMesh.GetBlockerMesh();
        }

        /// <summary>
        /// Adds a blocker to the active blocker list.
        /// </summary>
        /// <param name="blocker">The blocker to add.</param>
        private static void AddBlocker(LightBlocker blocker)
        {
            Debug.Assert(blocker.gameObject.activeInHierarchy, typeof(LightBlocker).FullName + ".AddBlocker: Blocker is not active.");
            Debug.Assert(!LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".AddBlocker: Already in list.");
            LightBlocker.blockerList.Add(blocker);
        }

        /// <summary>
        /// Removes a blocker from the active blocker list.
        /// </summary>
        /// <param name="blocker">The blocker to remove.</param>
        private static void RemoveBlocker(LightBlocker blocker)
        {
            Debug.Assert(!blocker.gameObject.activeInHierarchy, typeof(LightBlocker).FullName + ".RemoveBlocker: Blocker is active.");
            Debug.Assert(LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".RemoveBlocker: Not in list.");
            LightBlocker.blockerList.Remove(blocker);
        }

        /// <summary>
        /// Unity method called when game object is enabled.
        /// </summary>
        private void OnEnable()
        {
            LightBlocker.AddBlocker(this);
        }

        /// <summary>
        /// Unity method called when game object is disabled.
        /// </summary>
        private void OnDisable()
        {
            LightBlocker.RemoveBlocker(this);
        }
    }
}
