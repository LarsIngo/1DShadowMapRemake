using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaker
{
    using ShadowMaker.Core;

    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightBlocker : MonoBehaviour
    {
        private static List<LightBlocker> blockerList = new List<LightBlocker>();

        private LightBlockerMesh lightBlockerMesh = null;

        public static List<LightBlocker> GetActiveBlockerList()
        {
            return LightBlocker.blockerList;
        }

        private static void AddBlocker(LightBlocker blocker)
        {
            Debug.Assert(!LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".AddBlocker: Already in list.");
            LightBlocker.blockerList.Add(blocker);
        }
        
        private static void RemoveBlocker(LightBlocker blocker)
        {
            Debug.Assert(LightBlocker.blockerList.Contains(blocker), typeof(LightBlocker).FullName + ".RemoveBlocker: Not in list.");
            LightBlocker.blockerList.Remove(blocker);
        }

        public Mesh GetRenderMesh()
        {
            return this.gameObject.GetComponent<MeshFilter>().sharedMesh;
        }

        public Mesh GetBlockerMesh()
        {
            // If blocker never has been generated.
            if (this.lightBlockerMesh == null)
            {
                this.lightBlockerMesh = new LightBlockerMesh(this.GetRenderMesh());
            }

            // If render mesh has been changed, blocker mesh needs to be updated.
            if (this.GetRenderMesh() != this.lightBlockerMesh.GetBlockerMesh())
            {
                this.lightBlockerMesh.UpdateBlockerMesh(this.GetRenderMesh());
            }

            return this.lightBlockerMesh.GetBlockerMesh();
        }

        private void Awake()
        {
        }

        private void OnEnable()
        {
            LightBlocker.AddBlocker(this);
        }

        private void OnDisable()
        {
            LightBlocker.RemoveBlocker(this);
        }

        private void OnDestroy()
        {
        }
    }
}
