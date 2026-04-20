using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class TestSource : MonoBehaviour {
    private static readonly int LosFadePropertyId = Shader.PropertyToID("_Fade");
    private static readonly int LosOriginPropertyId = Shader.PropertyToID("_Origin");

    public float ViewRadius = 10f;
    [Range(0, 360)]
    public float ViewAngle = 360f;
    public LayerMask LosBlockers;
    public float MeshResolution = 1f;
    public float EdgeDstThreshold = 0.5f;
    public int MaxSubdivideDepth = 24;
    public float MinSegmentAngleDegrees = 0.02f;
    public Material MeshMaterial;
    public bool ShowMirrorSegmentGizmos = true;
    public float MirrorSegmentGizmoRadius = 0.1f;
    public bool GenerateReflectionMeshes = true;
    [Min(1)]
    public int MaxReflectionBounces = 4;

    private Mesh primaryLosMesh;
    private MeshFilter primaryLosMeshFilter;
    private MeshRenderer primaryLosRenderer;
    private MaterialPropertyBlock losFadePropertyBlock;
    private readonly List<MeshFilter> reflectionMeshPool = new List<MeshFilter>();
    private TestMirror[] mirrorsForFrame = System.Array.Empty<TestMirror>();
    private readonly List<ViewCastInfo> primaryBoundary = new List<ViewCastInfo>();
    private readonly List<List<ViewCastInfo>> boundaryCastBySegment = new List<List<ViewCastInfo>>();
    private readonly List<MirrorHitSegment> mirrorHitSegments = new List<MirrorHitSegment>();
    private readonly List<MirrorHitSegment> mirrorSegmentBatch = new List<MirrorHitSegment>();
    private readonly List<MirrorHitSegment> mirrorSegmentMergeBuffer = new List<MirrorHitSegment>();
    private readonly List<Vector3> reflectionMirrorScratch = new List<Vector3>();
    private readonly List<Vector3> reflectionRimScratch = new List<Vector3>();
    private Vector2 losPrimaryOriginWorldXY;
    private readonly List<Vector2> losReflectionOriginWorldXY = new List<Vector2>();

    private struct MirrorSample {
        public float mirrorT;
        public Vector3 point;
        public Vector3 incomingDir;
    }

    private void Awake() {
        MeshFilter losMf = CreateMeshChild("LosMesh", "LOS", startInactive: false);
        primaryLosMeshFilter = losMf;
        primaryLosMesh = losMf.sharedMesh;
        primaryLosRenderer = losMf.GetComponent<MeshRenderer>();
    }

    public void SetLosMaterialFade(float fade) {
        if (MeshMaterial == null) {
            return;
        }
        if (losFadePropertyBlock == null) {
            losFadePropertyBlock = new MaterialPropertyBlock();
        }
        if (primaryLosRenderer != null) {
            losFadePropertyBlock.Clear();
            losFadePropertyBlock.SetFloat(LosFadePropertyId, fade);
            losFadePropertyBlock.SetVector(LosOriginPropertyId, new Vector4(losPrimaryOriginWorldXY.x, losPrimaryOriginWorldXY.y, 0f, 0f));
            primaryLosRenderer.SetPropertyBlock(losFadePropertyBlock);
        }
        for (int i = 0; i < reflectionMeshPool.Count; i++) {
            MeshFilter mf = reflectionMeshPool[i];
            if (mf == null || !mf.gameObject.activeSelf) {
                continue;
            }
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null) {
                continue;
            }
            Vector2 origin = i < losReflectionOriginWorldXY.Count ? losReflectionOriginWorldXY[i] : Vector2.zero;
            losFadePropertyBlock.Clear();
            losFadePropertyBlock.SetFloat(LosFadePropertyId, fade);
            losFadePropertyBlock.SetVector(LosOriginPropertyId, new Vector4(origin.x, origin.y, 0f, 0f));
            mr.SetPropertyBlock(losFadePropertyBlock);
        }
    }

    private void OnEnable() {
        SignalMeshFieldManager.Instance?.RegisterSource(this);
        losPrimaryOriginWorldXY = new Vector2(transform.position.x, transform.position.y);
        ApplyCurrentGlobalLosFade();
    }

    private void OnDisable() {
        SignalMeshFieldManager.Instance?.UnregisterSource(this);
    }

    public int CountMeshesContainingWorldPoint(Vector3 worldPoint) {
        int count = 0;
        if (primaryLosMeshFilter != null && primaryLosMesh != null && MeshContainsPointXY(primaryLosMesh, primaryLosMeshFilter.transform, worldPoint)) {
            count++;
        }
        for (int i = 0; i < reflectionMeshPool.Count; i++) {
            MeshFilter mf = reflectionMeshPool[i];
            if (mf == null || !mf.gameObject.activeSelf) {
                continue;
            }
            Mesh m = mf.sharedMesh;
            if (m == null || m.triangles == null || m.triangles.Length < 3) {
                continue;
            }
            if (MeshContainsPointXY(m, mf.transform, worldPoint)) {
                count++;
            }
        }
        return count;
    }

    public bool IsWorldPointInsideField(Vector3 worldPoint) {
        return CountMeshesContainingWorldPoint(worldPoint) > 0;
    }

    private static bool MeshContainsPointXY(Mesh mesh, Transform meshTransform, Vector3 worldPoint) {
        Vector3 lp = meshTransform.InverseTransformPoint(worldPoint);
        Vector2 p = new Vector2(lp.x, lp.y);
        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;
        if (verts == null || tris == null || tris.Length < 3) {
            return false;
        }
        for (int ti = 0; ti < tris.Length; ti += 3) {
            int i0 = tris[ti];
            int i1 = tris[ti + 1];
            int i2 = tris[ti + 2];
            if ((uint)i0 >= (uint)verts.Length || (uint)i1 >= (uint)verts.Length || (uint)i2 >= (uint)verts.Length) {
                continue;
            }
            Vector2 a = new Vector2(verts[i0].x, verts[i0].y);
            Vector2 b = new Vector2(verts[i1].x, verts[i1].y);
            Vector2 c = new Vector2(verts[i2].x, verts[i2].y);
            if (PointInTriangleXY(p, a, b, c)) {
                return true;
            }
        }
        return false;
    }

    private static bool PointInTriangleXY(Vector2 p, Vector2 a, Vector2 b, Vector2 c) {
        const float eps = 1e-6f;
        float Cross2(Vector2 u, Vector2 v) {
            return u.x * v.y - u.y * v.x;
        }
        float c1 = Cross2(b - a, p - a);
        float c2 = Cross2(c - b, p - b);
        float c3 = Cross2(a - c, p - c);
        bool hasNeg = (c1 < -eps) || (c2 < -eps) || (c3 < -eps);
        bool hasPos = (c1 > eps) || (c2 > eps) || (c3 > eps);
        return !(hasNeg && hasPos);
    }

    private MeshFilter CreateMeshChild(string gameObjectName, string meshName, bool startInactive) {
        var go = new GameObject(gameObjectName);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        MeshFilter mf = go.AddComponent<MeshFilter>();
        var mr = go.AddComponent<MeshRenderer>();
        if (MeshMaterial != null) {
            mr.sharedMaterial = MeshMaterial;
        }
        mf.sharedMesh = new Mesh { name = meshName };
        if (startInactive) {
            go.SetActive(false);
        }
        return mf;
    }

    private static void UploadLosMesh(Mesh mesh, Vector3[] vertices, int[] triangles) {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    private bool ShouldSubdivideLosCast(ViewCastInfo a, ViewCastInfo b, bool compareMirrorHitChannel) {
        if (a.hit != b.hit) {
            return true;
        }
        if (a.hit && b.hit && a.hitObjectInstanceId != b.hitObjectInstanceId) {
            return true;
        }
        if (compareMirrorHitChannel && a.hit && b.hit && a.mirrorHit != b.mirrorHit) {
            return true;
        }
        if (a.hit && b.hit && Mathf.Abs(a.dst - b.dst) > EdgeDstThreshold) {
            return true;
        }
        return false;
    }

    private void LateUpdate() {
        if (GameManager.Instance != null && GameManager.Instance.isTransitioning) {
            return;
        }
        DrawFieldOfView();
    }

    private void OnDrawGizmos() {
        if (!ShowMirrorSegmentGizmos || mirrorHitSegments.Count == 0) {
            return;
        }
        for (int si = 0; si < mirrorHitSegments.Count; si++) {
            MirrorHitSegment seg = mirrorHitSegments[si];
            Gizmos.color = seg.gizmoColor;
            List<MirrorSample> samples = seg.samples;
            for (int i = 0; i < samples.Count; i++) {
                Gizmos.DrawSphere(samples[i].point, MirrorSegmentGizmoRadius);
            }
        }
    }

    private TestMirror FindMirrorByGameObjectInstanceId(int instanceId) {
        if (mirrorsForFrame != null) {
            for (int i = 0; i < mirrorsForFrame.Length; i++) {
                TestMirror m = mirrorsForFrame[i];
                if (m != null && m.gameObject.GetInstanceID() == instanceId) {
                    return m;
                }
            }
        }
        TestMirror[] found = UnityEngine.Object.FindObjectsByType<TestMirror>(FindObjectsSortMode.None);
        for (int i = 0; i < found.Length; i++) {
            if (found[i] != null && found[i].gameObject.GetInstanceID() == instanceId) {
                return found[i];
            }
        }
        return null;
    }

    private static Vector3 IncomingRayAtMirrorT(float tQuery, List<MirrorSample> samples) {
        int n = samples.Count;
        if (n == 0) {
            return Vector3.zero;
        }
        if (n == 1) {
            return samples[0].incomingDir;
        }
        float tMinVal = samples[0].mirrorT;
        float tMaxVal = samples[n - 1].mirrorT;
        if (tQuery <= tMinVal) {
            return samples[0].incomingDir;
        }
        if (tQuery >= tMaxVal) {
            return samples[n - 1].incomingDir;
        }
        for (int i = 0; i < n - 1; i++) {
            float ta = samples[i].mirrorT;
            float tb = samples[i + 1].mirrorT;
            if (tQuery >= ta && tQuery <= tb) {
                float denom = tb - ta;
                if (denom < 1e-10f) {
                    return samples[i].incomingDir;
                }
                float u = (tQuery - ta) / denom;
                return Vector3.Lerp(samples[i].incomingDir, samples[i + 1].incomingDir, u).normalized;
            }
        }
        return samples[n - 1].incomingDir;
    }

    private static Vector3 IncomingDirForBoundarySample(ViewCastInfo c, Vector3 eyeWorld) {
        if (c.incomingDir.sqrMagnitude > 1e-18f) {
            return c.incomingDir.normalized;
        }
        Vector3 w = c.point - eyeWorld;
        return w.sqrMagnitude > 1e-18f ? w.normalized : Vector3.right;
    }

    private static void BuildMirrorHitSegments(List<ViewCastInfo> samples, Vector3 eyeWorld, List<MirrorHitSegment> outSegments, int hueBaseIndex = 0) {
        outSegments.Clear();
        int n = samples.Count;
        int idx = 0;
        while (idx < n) {
            ViewCastInfo s = samples[idx];
            if (!s.hit || !s.mirrorHit) {
                idx++;
                continue;
            }
            int mirrorId = s.hitObjectInstanceId;
            int start = idx;
            idx++;
            while (idx < n && samples[idx].hit && samples[idx].mirrorHit && samples[idx].hitObjectInstanceId == mirrorId) {
                idx++;
            }
            int end = idx - 1;
            int count = end - start + 1;
            var sampleList = new List<MirrorSample>(count);
            for (int bi = start; bi <= end; bi++) {
                ViewCastInfo c = samples[bi];
                Vector3 incoming = IncomingDirForBoundarySample(c, eyeWorld);
                sampleList.Add(new MirrorSample { mirrorT = c.mirrorT, point = c.point, incomingDir = incoming });
            }
            if (sampleList.Count >= 2 && sampleList[0].mirrorT > sampleList[sampleList.Count - 1].mirrorT) {
                sampleList.Reverse();
            }
            outSegments.Add(MirrorHitSegment.Create(mirrorId, sampleList));
        }
        for (int si = 0; si < outSegments.Count; si++) {
            MirrorHitSegment seg = outSegments[si];
            seg.gizmoColor = Color.HSVToRGB(((hueBaseIndex + si) * 0.21f) % 1f, 0.75f, 1f);
            outSegments[si] = seg;
        }
    }

    private bool ShouldSubdivide(ViewCastInfo a, ViewCastInfo b) {
        return ShouldSubdivideLosCast(a, b, compareMirrorHitChannel: false);
    }

    private void RefineSegment(ViewCastInfo left, ViewCastInfo right, List<ViewCastInfo> boundary, int depth) {
        float deltaDeg = Mathf.Abs(right.angle - left.angle);
        if (depth >= MaxSubdivideDepth || !ShouldSubdivide(left, right) || deltaDeg < MinSegmentAngleDegrees) {
            boundary.Add(right);
            return;
        }
        float midAngle = (left.angle + right.angle) * 0.5f;
        ViewCastInfo mid = ViewCast(midAngle);
        RefineSegment(left, mid, boundary, depth + 1);
        RefineSegment(mid, right, boundary, depth + 1);
    }

    private void DrawFieldOfView() {
        mirrorsForFrame = Object.FindObjectsByType<TestMirror>(FindObjectsSortMode.None);
        losReflectionOriginWorldXY.Clear();
        losPrimaryOriginWorldXY = new Vector2(transform.position.x, transform.position.y);
        int stepCount = Mathf.RoundToInt(ViewAngle * MeshResolution);
        if (stepCount < 1) {
            stepCount = 1;
        }
        float stepAngleSize = ViewAngle / stepCount;
        var coarseCasts = new ViewCastInfo[stepCount + 1];
        for (int i = 0; i <= stepCount; i++) {
            float angle = transform.eulerAngles.z - ViewAngle / 2f + stepAngleSize * i;
            coarseCasts[i] = ViewCast(angle);
        }

        primaryBoundary.Clear();
        primaryBoundary.Add(coarseCasts[0]);
        for (int i = 0; i < stepCount; i++) {
            RefineSegment(coarseCasts[i], coarseCasts[i + 1], primaryBoundary, 0);
        }

        mirrorHitSegments.Clear();
        BuildMirrorHitSegments(primaryBoundary, transform.position, mirrorHitSegments, hueBaseIndex: 0);

        int vertexCount = primaryBoundary.Count + 1;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[(vertexCount - 2) * 3];

        vertices[0] = Vector3.zero;
        for (int vi = 0; vi < vertexCount - 1; vi++) {
            vertices[vi + 1] = transform.InverseTransformPoint(primaryBoundary[vi].point);

            if (vi < vertexCount - 2) {
                int ti = vi * 3;
                triangles[ti] = 0;
                triangles[ti + 1] = vi + 2;
                triangles[ti + 2] = vi + 1;
            }
        }

        UploadLosMesh(primaryLosMesh, vertices, triangles);

        int reflectionSlotsUsed = 0;
        if (GenerateReflectionMeshes) {
            reflectionSlotsUsed = RunReflectionBounceLoop();
        }
        FinalizeReflectionPool(reflectionSlotsUsed);
        ApplyCurrentGlobalLosFade();
    }

    private void ApplyCurrentGlobalLosFade() {
        float f = SignalMeshFieldManager.Instance != null ? SignalMeshFieldManager.CurrentLosFade : 1f;
        SetLosMaterialFade(f);
    }

    private void EnsureBoundaryCastListCount(int count) {
        while (boundaryCastBySegment.Count < count) {
            boundaryCastBySegment.Add(new List<ViewCastInfo>());
        }
        while (boundaryCastBySegment.Count > count) {
            int last = boundaryCastBySegment.Count - 1;
            boundaryCastBySegment[last].Clear();
            boundaryCastBySegment.RemoveAt(last);
        }
    }

    private int RunReflectionBounceLoop() {
        Vector3 eye = transform.position;
        int poolSlot = 0;
        int hueBase = mirrorHitSegments.Count;
        int bounce = 0;
        while (bounce < MaxReflectionBounces && mirrorHitSegments.Count > 0) {
            EnsureBoundaryCastListCount(mirrorHitSegments.Count);
            for (int si = 0; si < mirrorHitSegments.Count; si++) {
                List<ViewCastInfo> rimList = boundaryCastBySegment[si];
                rimList.Clear();
                poolSlot = AppendReflectionMeshForSegment(mirrorHitSegments[si], eye, rimList, poolSlot);
            }
            bounce++;
            if (bounce >= MaxReflectionBounces) {
                break;
            }
            mirrorSegmentMergeBuffer.Clear();
            int hueOffset = 0;
            for (int li = 0; li < boundaryCastBySegment.Count; li++) {
                List<ViewCastInfo> L = boundaryCastBySegment[li];
                if (L.Count == 0) {
                    continue;
                }
                BuildMirrorHitSegments(L, eye, mirrorSegmentBatch, hueBaseIndex: hueBase + hueOffset);
                hueOffset += mirrorSegmentBatch.Count;
                mirrorSegmentMergeBuffer.AddRange(mirrorSegmentBatch);
            }
            mirrorHitSegments.Clear();
            mirrorHitSegments.AddRange(mirrorSegmentMergeBuffer);
            hueBase += hueOffset;
            if (mirrorHitSegments.Count == 0) {
                break;
            }
        }
        return poolSlot;
    }

    private int AppendReflectionMeshForSegment(MirrorHitSegment seg, Vector3 eye, List<ViewCastInfo> rimCastInfos, int poolSlot) {
        List<MirrorSample> samples = seg.samples;
        if (samples.Count == 0) {
            return poolSlot;
        }
        TestMirror mirror = FindMirrorByGameObjectInstanceId(seg.mirrorObjectInstanceId);
        if (mirror == null) {
            return poolSlot;
        }
        Vector3 worldA = mirror.WorldA;
        Vector3 worldB = mirror.WorldB;
        Vector3 segVec = worldB - worldA;
        float segLen = segVec.magnitude;
        if (segLen < 1e-6f) {
            return poolSlot;
        }
        Vector3 nBase = mirror.WorldNormalXy;
        int excludeId = seg.mirrorObjectInstanceId;
        reflectionMirrorScratch.Clear();
        reflectionRimScratch.Clear();
        rimCastInfos.Clear();
        Vector3 d0 = ReflectedDirection(samples[0].incomingDir, nBase);
        ViewCastInfo h0 = CastReflectionRayHit(samples[0].point, d0, excludeId);
        reflectionMirrorScratch.Add(samples[0].point);
        reflectionRimScratch.Add(h0.point);
        rimCastInfos.Add(h0);
        if (samples.Count >= 2) {
            for (int ei = 0; ei < samples.Count - 1; ei++) {
                RefineReflectionMirrorEdge(samples[ei], samples[ei + 1], samples, worldA, segVec, eye, nBase, excludeId, reflectionMirrorScratch, reflectionRimScratch, rimCastInfos, 0);
            }
        }
        if (reflectionMirrorScratch.Count < 2) {
            while (reflectionMirrorScratch.Count < 2) {
                int last = reflectionMirrorScratch.Count - 1;
                reflectionMirrorScratch.Add(last >= 0 ? reflectionMirrorScratch[last] : samples[0].point);
                ViewCastInfo hPad = CastReflectionRayHit(samples[0].point, ReflectedDirection(samples[0].incomingDir, nBase), excludeId);
                reflectionRimScratch.Add(last >= 0 ? reflectionRimScratch[last] : hPad.point);
                if (rimCastInfos.Count == 0) {
                    rimCastInfos.Add(hPad);
                }
            }
        }
        while (losReflectionOriginWorldXY.Count <= poolSlot) {
            losReflectionOriginWorldXY.Add(Vector2.zero);
        }
        Vector3 mirrorMidWorld = (worldA + worldB) * 0.5f;
        losReflectionOriginWorldXY[poolSlot] = new Vector2(mirrorMidWorld.x, mirrorMidWorld.y);
        MeshFilter mf = EnsureReflectionMeshSlot(poolSlot);
        WriteReflectionQuadStripMesh(mf.sharedMesh, reflectionMirrorScratch, reflectionRimScratch);
        mf.gameObject.SetActive(true);
        return poolSlot + 1;
    }

    private MeshFilter EnsureReflectionMeshSlot(int index) {
        while (reflectionMeshPool.Count <= index) {
            int n = reflectionMeshPool.Count;
            MeshFilter mf = CreateMeshChild("ReflectionSeg_" + n, "ReflectionPool_" + n, startInactive: true);
            reflectionMeshPool.Add(mf);
        }
        return reflectionMeshPool[index];
    }

    private void FinalizeReflectionPool(int activeCount) {
        for (int i = 0; i < reflectionMeshPool.Count; i++) {
            reflectionMeshPool[i].gameObject.SetActive(i < activeCount);
        }
    }

    private void WriteReflectionQuadStripMesh(Mesh rm, List<Vector3> mirrorEdgeWorld, List<Vector3> rimWorld) {
        int k = mirrorEdgeWorld.Count;
        if (k != rimWorld.Count || k < 2) {
            return;
        }
        int vc = 2 * k;
        var vertices = new Vector3[vc];
        for (int i = 0; i < k; i++) {
            vertices[2 * i] = transform.InverseTransformPoint(mirrorEdgeWorld[i]);
            vertices[2 * i + 1] = transform.InverseTransformPoint(rimWorld[i]);
        }
        var triangles = new int[(k - 1) * 6];
        for (int i = 0; i < k - 1; i++) {
            int v0 = 2 * i;
            int r0 = 2 * i + 1;
            int v1 = 2 * i + 2;
            int r1 = 2 * i + 3;
            int b = i * 6;
            Vector3 m0 = vertices[v0];
            Vector3 r0v = vertices[r0];
            Vector3 m1 = vertices[v1];
            Vector3 cr = Vector3.Cross(r0v - m0, m1 - m0);
            bool flipForMinusZ = cr.z > 0f;
            if (!flipForMinusZ) {
                triangles[b] = v0;
                triangles[b + 1] = r1;
                triangles[b + 2] = v1;
                triangles[b + 3] = v0;
                triangles[b + 4] = r0;
                triangles[b + 5] = r1;
            } else {
                triangles[b] = v0;
                triangles[b + 1] = v1;
                triangles[b + 2] = r1;
                triangles[b + 3] = v0;
                triangles[b + 4] = r1;
                triangles[b + 5] = r0;
            }
        }
        UploadLosMesh(rm, vertices, triangles);
    }

    private void RefineReflectionMirrorEdge(MirrorSample left, MirrorSample right, List<MirrorSample> keySamples, Vector3 worldA, Vector3 segVec, Vector3 eye, Vector3 nBase, int excludeId, List<Vector3> outMirror, List<Vector3> outRim, List<ViewCastInfo> rimCastInfos, int depth) {
        ViewCastInfo hLeft = CastReflectionRayHit(left.point, ReflectedDirection(left.incomingDir, nBase), excludeId);
        ViewCastInfo hRight = CastReflectionRayHit(right.point, ReflectedDirection(right.incomingDir, nBase), excludeId);
        if (depth >= MaxSubdivideDepth || !ShouldSubdivideLosCast(hLeft, hRight, compareMirrorHitChannel: true)) {
            outMirror.Add(right.point);
            outRim.Add(hRight.point);
            rimCastInfos.Add(hRight);
            return;
        }
        float tMid = (left.mirrorT + right.mirrorT) * 0.5f;
        Vector3 qMid = worldA + segVec * tMid;
        Vector3 incomingMid = IncomingRayAtMirrorT(tMid, keySamples);
        if (incomingMid.sqrMagnitude < 1e-12f) {
            incomingMid = (qMid - eye).sqrMagnitude > 1e-12f ? (qMid - eye).normalized : Vector3.right;
        }
        var mid = new MirrorSample { mirrorT = tMid, point = qMid, incomingDir = incomingMid };
        RefineReflectionMirrorEdge(left, mid, keySamples, worldA, segVec, eye, nBase, excludeId, outMirror, outRim, rimCastInfos, depth + 1);
        RefineReflectionMirrorEdge(mid, right, keySamples, worldA, segVec, eye, nBase, excludeId, outMirror, outRim, rimCastInfos, depth + 1);
    }

    private static Vector3 ReflectedDirection(Vector3 incomingDir, Vector3 mirrorNormalXy) {
        Vector3 n = mirrorNormalXy;
        if (Vector3.Dot(n, incomingDir) < 0f) {
            n = -n;
        }
        return Vector3.Reflect(incomingDir, n).normalized;
    }

    private Vector3 CastReflectionRayEnd(Vector3 originOnMirror, Vector3 reflectedDir, int excludeMirrorGameObjectInstanceId) {
        return CastReflectionRayHit(originOnMirror, reflectedDir, excludeMirrorGameObjectInstanceId).point;
    }

    private ViewCastInfo CastReflectionRayHit(Vector3 originOnMirror, Vector3 reflectedDir, int excludeMirrorGameObjectInstanceId) {
        reflectedDir = reflectedDir.normalized;
        const float skin = 0.002f;
        float maxReach = ViewRadius;
        Vector3 missEnd = originOnMirror + reflectedDir * maxReach;

        float acc = skin;
        Vector3 firstRayOrigin = originOnMirror + reflectedDir * acc;
        float remainingFromFirst = maxReach - acc;
        if (remainingFromFirst < 1e-5f) {
            return new ViewCastInfo(false, missEnd, maxReach, 0f, 0, false, 0f, reflectedDir);
        }

        float bestPhysAlong = float.PositiveInfinity;
        Vector3 bestPhysPoint = missEnd;
        int bestPhysId = 0;
        Vector3 p = firstRayOrigin;
        float left = remainingFromFirst;
        while (left > 1e-5f) {
            if (!Physics.Raycast(p, reflectedDir, out RaycastHit hit, left, LosBlockers, QueryTriggerInteraction.Ignore)) {
                break;
            }
            if (hit.collider.gameObject.GetInstanceID() != excludeMirrorGameObjectInstanceId) {
                bestPhysAlong = acc + hit.distance;
                bestPhysPoint = hit.point;
                bestPhysId = hit.collider.gameObject.GetInstanceID();
                break;
            }
            float advance = Mathf.Max(hit.distance + skin, skin);
            acc += advance;
            p += reflectedDir * advance;
            left -= advance;
            if (advance < 1e-6f) {
                break;
            }
        }

        float bestMirAlong = float.PositiveInfinity;
        Vector3 bestMirPoint = default;
        int bestMirId = 0;
        float bestMirSegT = 0f;
        if (mirrorsForFrame != null) {
            for (int mi = 0; mi < mirrorsForFrame.Length; mi++) {
                TestMirror m = mirrorsForFrame[mi];
                if (m == null) {
                    continue;
                }
                if (m.gameObject.GetInstanceID() == excludeMirrorGameObjectInstanceId) {
                    continue;
                }
                if (TryRaySegmentHitXY(originOnMirror, reflectedDir, m.WorldA, m.WorldB, maxReach, out float mirT, out Vector3 mirPt, out float segT) && mirT < bestMirAlong) {
                    bestMirAlong = mirT;
                    bestMirPoint = mirPt;
                    bestMirId = m.gameObject.GetInstanceID();
                    bestMirSegT = segT;
                }
            }
        }

        bool hasMir = bestMirAlong < float.PositiveInfinity;
        bool hasPhys = bestPhysAlong < float.PositiveInfinity;
        if (!hasMir && !hasPhys) {
            return new ViewCastInfo(false, missEnd, maxReach, 0f, 0, false, 0f, reflectedDir);
        }
        if (hasMir && (!hasPhys || bestMirAlong < bestPhysAlong)) {
            return new ViewCastInfo(true, bestMirPoint, bestMirAlong, 0f, bestMirId, true, bestMirSegT, reflectedDir);
        }
        return new ViewCastInfo(true, bestPhysPoint, bestPhysAlong, 0f, bestPhysId, false, 0f, reflectedDir);
    }

    private static float Cross2XY(Vector3 a, Vector3 b) {
        return a.x * b.y - a.y * b.x;
    }

    private static bool TryRaySegmentHitXY(Vector3 origin, Vector3 dir, Vector3 segA, Vector3 segB, float maxT, out float rayT, out Vector3 hitPoint, out float segmentT) {
        Vector3 w = segA - origin;
        Vector3 v = segB - segA;
        float denom = Cross2XY(dir, v);
        if (Mathf.Abs(denom) < 1e-7f) {
            rayT = 0f;
            hitPoint = default;
            segmentT = 0f;
            return false;
        }
        float tt = Cross2XY(w, v) / denom;
        float uu = Cross2XY(w, dir) / denom;
        if (tt < 0f || tt > maxT || uu < 0f || uu > 1f) {
            rayT = 0f;
            hitPoint = default;
            segmentT = 0f;
            return false;
        }
        rayT = tt;
        segmentT = uu;
        hitPoint = origin + dir * tt;
        return true;
    }

    private ViewCastInfo ViewCast(float globalAngleDegrees) {
        Vector3 origin = transform.position;
        Vector3 dir = DirFromAngle(globalAngleDegrees, true);
        bool hasPhys = Physics.Raycast(origin, dir, out RaycastHit physHit, ViewRadius, LosBlockers);
        float physT = hasPhys ? physHit.distance : float.PositiveInfinity;

        float bestMirT = float.PositiveInfinity;
        Vector3 bestMirPoint = default;
        int bestMirId = 0;
        float bestMirrorSegT = 0f;
        if (mirrorsForFrame != null) {
            for (int mi = 0; mi < mirrorsForFrame.Length; mi++) {
                TestMirror mirror = mirrorsForFrame[mi];
                if (mirror == null) {
                    continue;
                }
                Vector3 a = mirror.WorldA;
                Vector3 b = mirror.WorldB;
                if (TryRaySegmentHitXY(origin, dir, a, b, ViewRadius, out float mirT, out Vector3 mirPoint, out float segT) && mirT < bestMirT) {
                    bestMirT = mirT;
                    bestMirPoint = mirPoint;
                    bestMirId = mirror.gameObject.GetInstanceID();
                    bestMirrorSegT = segT;
                }
            }
        }

        bool hasMir = bestMirT < float.PositiveInfinity;
        if (!hasPhys && !hasMir) {
            return new ViewCastInfo(false, origin + dir * ViewRadius, ViewRadius, globalAngleDegrees, 0, false, 0f, dir);
        }
        if (hasMir && (!hasPhys || bestMirT < physT)) {
            return new ViewCastInfo(true, bestMirPoint, bestMirT, globalAngleDegrees, bestMirId, true, bestMirrorSegT, dir);
        }
        return new ViewCastInfo(true, physHit.point, physHit.distance, globalAngleDegrees, physHit.collider.gameObject.GetInstanceID(), false, 0f, dir);
    }

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal) {
        if (!angleIsGlobal) {
            angleInDegrees += transform.eulerAngles.z;
        }
        float rad = angleInDegrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f);
    }

    private struct MirrorHitSegment {
        public int mirrorObjectInstanceId;
        public Color gizmoColor;
        public List<MirrorSample> samples;

        public static MirrorHitSegment Create(int mirrorObjectInstanceId, List<MirrorSample> samples) {
            return new MirrorHitSegment {
                mirrorObjectInstanceId = mirrorObjectInstanceId,
                samples = samples,
                gizmoColor = Color.white,
            };
        }
    }

    private struct ViewCastInfo {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;
        public int hitObjectInstanceId;
        public bool mirrorHit;
        public float mirrorT;
        public Vector3 incomingDir;

        public ViewCastInfo(bool hit, Vector3 point, float dst, float angle, int hitObjectInstanceId, bool mirrorHit, float mirrorT, Vector3 incomingDir) {
            this.hit = hit;
            this.point = point;
            this.dst = dst;
            this.angle = angle;
            this.hitObjectInstanceId = hitObjectInstanceId;
            this.mirrorHit = mirrorHit;
            this.mirrorT = mirrorT;
            this.incomingDir = incomingDir;
        }
    }
}
