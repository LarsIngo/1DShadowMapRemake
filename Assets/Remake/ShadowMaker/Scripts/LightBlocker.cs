using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ShadowMaker
{
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class LightBlocker : MonoBehaviour
    {
        private static List<LightBlocker> blockerList = new List<LightBlocker>();

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

        public Mesh GetMesh()
        {
            return this.gameObject.GetComponent<MeshFilter>().sharedMesh;
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

        public void GetEdges(List<Vector2> edges)
        {
            Vector3 v1 = new Vector3(-0.5f, -0.5f, 0.0f);
            Vector3 v2 = new Vector3(+0.5f, -0.5f, 0.0f);
            Vector3 v3 = new Vector3(+0.5f, +0.5f, 0.0f);
            Vector3 v4 = new Vector3(-0.5f, +0.5f, 0.0f);

            v1 = transform.localToWorldMatrix.MultiplyPoint(v1);
            v2 = transform.localToWorldMatrix.MultiplyPoint(v2);
            v3 = transform.localToWorldMatrix.MultiplyPoint(v3);
            v4 = transform.localToWorldMatrix.MultiplyPoint(v4);

            edges.Add(new Vector2(v1.x, v1.y));
            edges.Add(new Vector2(v2.x, v2.y));

            edges.Add(new Vector2(v2.x, v2.y));
            edges.Add(new Vector2(v3.x, v3.y));

            edges.Add(new Vector2(v3.x, v3.y));
            edges.Add(new Vector2(v4.x, v4.y));

            edges.Add(new Vector2(v4.x, v4.y));
            edges.Add(new Vector2(v1.x, v1.y));
        }
    }
}
