using System;
using System.Reflection;
using UnityEngine;

namespace PaintedAlive.Figures.StainSupport
{
    public readonly struct StainWatercolorFlowSample
    {
        public StainWatercolorFlowSample(
            Vector3 closestPoint,
            Vector3 surfaceNormal,
            Vector3 direction,
            float speed)
        {
            ClosestPoint = closestPoint;
            SurfaceNormal = surfaceNormal;
            Direction = direction;
            Speed = speed;
        }

        public Vector3 ClosestPoint { get; }
        public Vector3 SurfaceNormal { get; }
        public Vector3 Direction { get; }
        public float Speed { get; }
    }

    [DisallowMultipleComponent]
    public sealed class WatercolorFlowSourceAdapter : MonoBehaviour
    {
        private const BindingFlags ReflectionFlags =
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.NonPublic;

        [SerializeField] private MonoBehaviour sourceBehaviour;
        [SerializeField] private Collider surfaceCollider;
        [SerializeField] private Vector3 fallbackLocalDirection = Vector3.forward;
        [SerializeField, Min(0f)] private float fallbackSpeed = 4.4f;

        private MonoBehaviour[] stateBehaviours = Array.Empty<MonoBehaviour>();
        private MemberInfo vectorMember;
        private MethodInfo vectorMethod;
        private bool vectorMethodUsesPosition;
        private MemberInfo speedMember;
        private MethodInfo speedMethod;

        public MonoBehaviour SourceBehaviour => sourceBehaviour;
        public Collider SurfaceCollider => surfaceCollider;

        public void Configure(
            MonoBehaviour source,
            Collider collider,
            float defaultSpeed)
        {
            sourceBehaviour = source;
            surfaceCollider = collider;
            fallbackSpeed = Mathf.Max(0f, defaultSpeed);
            CacheReflectionAccessors();
        }

        private void Awake()
        {
            CacheReflectionAccessors();
        }

        public bool TrySample(
            Vector3 worldPosition,
            out StainWatercolorFlowSample sample)
        {
            sample = default;

            if (!IsFlowActive())
            {
                return false;
            }

            Collider collider = ResolveCollider();
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!TryResolveSurfaceSample(
                    collider,
                    worldPosition,
                    out Vector3 closestPoint,
                    out Vector3 normal))
            {
                return false;
            }

            Vector3 direction = ReadFlowVector(worldPosition, out float vectorMagnitude);
            direction = Vector3.ProjectOnPlane(direction, normal);

            if (direction.sqrMagnitude < 0.0001f)
            {
                Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, normal);
                direction = downhill.sqrMagnitude > 0.0064f
                    ? downhill
                    : Vector3.ProjectOnPlane(
                        transform.TransformDirection(fallbackLocalDirection),
                        normal);
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.ProjectOnPlane(transform.forward, normal);
            }

            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;
            }

            direction.Normalize();

            float reflectedSpeed = ReadSpeed();
            float speed = reflectedSpeed > 0.001f
                ? reflectedSpeed
                : vectorMagnitude > 0.001f
                    ? vectorMagnitude
                    : fallbackSpeed;

            sample = new StainWatercolorFlowSample(
                closestPoint,
                normal,
                direction,
                Mathf.Max(0f, speed));

            return true;
        }

        private static bool TryResolveSurfaceSample(
            Collider collider,
            Vector3 worldPosition,
            out Vector3 closestPoint,
            out Vector3 surfaceNormal)
        {
            closestPoint = worldPosition;
            surfaceNormal = Vector3.up;

            if (collider == null)
            {
                return false;
            }

            if (SupportsClosestPoint(collider))
            {
                closestPoint = collider.ClosestPoint(worldPosition);
                Vector3 fromSurface = worldPosition - closestPoint;

                if (fromSurface.sqrMagnitude > 0.0001f)
                {
                    surfaceNormal = fromSurface.normalized;
                    return true;
                }

                if (TryRaycastSurface(collider, worldPosition, out RaycastHit supportedHit))
                {
                    closestPoint = supportedHit.point;
                    surfaceNormal = supportedHit.normal;
                    return true;
                }

                surfaceNormal = ResolveFallbackNormal(collider, worldPosition);
                return true;
            }

            // Unity does not permit Collider.ClosestPoint on a non-convex
            // MeshCollider (or other unsupported collider types). Raycast the
            // collider itself so M13's generated watercolor mesh can still
            // provide a real surface point and normal without flooding Console.
            if (TryRaycastSurface(collider, worldPosition, out RaycastHit hit))
            {
                closestPoint = hit.point;
                surfaceNormal = hit.normal;
                return true;
            }

            Bounds bounds = collider.bounds;
            closestPoint = bounds.ClosestPoint(worldPosition);
            surfaceNormal = ResolveFallbackNormal(collider, worldPosition);
            return true;
        }

        private static bool SupportsClosestPoint(Collider collider)
        {
            if (collider is BoxCollider ||
                collider is SphereCollider ||
                collider is CapsuleCollider)
            {
                return true;
            }

            return collider is MeshCollider meshCollider && meshCollider.convex;
        }

        private static bool TryRaycastSurface(
            Collider collider,
            Vector3 worldPosition,
            out RaycastHit bestHit)
        {
            bestHit = default;

            Bounds bounds = collider.bounds;
            float castOffset = Mathf.Max(bounds.extents.magnitude + 0.5f, 1f);
            float castDistance = castOffset * 2f;
            float bestDistanceSquared = float.PositiveInfinity;
            bool found = false;

            Vector3 localUp = collider.transform.up;
            if (localUp.sqrMagnitude < 0.0001f)
            {
                localUp = Vector3.up;
            }

            localUp.Normalize();

            TryRayPair(
                collider,
                worldPosition,
                localUp,
                castOffset,
                castDistance,
                ref found,
                ref bestDistanceSquared,
                ref bestHit);

            if (Vector3.Cross(localUp, Vector3.up).sqrMagnitude > 0.0001f)
            {
                TryRayPair(
                    collider,
                    worldPosition,
                    Vector3.up,
                    castOffset,
                    castDistance,
                    ref found,
                    ref bestDistanceSquared,
                    ref bestHit);
            }

            Vector3 towardBounds = bounds.center - worldPosition;
            if (towardBounds.sqrMagnitude > 0.0001f)
            {
                TryRayPair(
                    collider,
                    worldPosition,
                    towardBounds.normalized,
                    castOffset,
                    castDistance,
                    ref found,
                    ref bestDistanceSquared,
                    ref bestHit);
            }

            return found;
        }

        private static void TryRayPair(
            Collider collider,
            Vector3 worldPosition,
            Vector3 axis,
            float castOffset,
            float castDistance,
            ref bool found,
            ref float bestDistanceSquared,
            ref RaycastHit bestHit)
        {
            TryColliderRay(
                collider,
                worldPosition + axis * castOffset,
                -axis,
                worldPosition,
                castDistance,
                ref found,
                ref bestDistanceSquared,
                ref bestHit);

            TryColliderRay(
                collider,
                worldPosition - axis * castOffset,
                axis,
                worldPosition,
                castDistance,
                ref found,
                ref bestDistanceSquared,
                ref bestHit);
        }

        private static void TryColliderRay(
            Collider collider,
            Vector3 origin,
            Vector3 direction,
            Vector3 worldPosition,
            float maximumDistance,
            ref bool found,
            ref float bestDistanceSquared,
            ref RaycastHit bestHit)
        {
            Ray ray = new Ray(origin, direction);
            if (!collider.Raycast(ray, out RaycastHit hit, maximumDistance))
            {
                return;
            }

            float distanceSquared = (hit.point - worldPosition).sqrMagnitude;
            if (distanceSquared >= bestDistanceSquared)
            {
                return;
            }

            found = true;
            bestDistanceSquared = distanceSquared;
            bestHit = hit;
        }

        private static Vector3 ResolveFallbackNormal(
            Collider collider,
            Vector3 worldPosition)
        {
            Vector3 normal = collider.transform.up;
            if (normal.sqrMagnitude < 0.0001f)
            {
                normal = Vector3.up;
            }

            normal.Normalize();

            if (Vector3.Dot(worldPosition - collider.bounds.center, normal) < 0f)
            {
                normal = -normal;
            }

            return normal;
        }

        private Collider ResolveCollider()
        {
            if (surfaceCollider != null)
            {
                return surfaceCollider;
            }

            surfaceCollider = GetComponent<Collider>();
            if (surfaceCollider == null)
            {
                surfaceCollider = GetComponentInChildren<Collider>(true);
            }

            if (surfaceCollider == null)
            {
                surfaceCollider = GetComponentInParent<Collider>();
            }

            return surfaceCollider;
        }

        private bool IsFlowActive()
        {
            if (!isActiveAndEnabled)
            {
                return false;
            }

            if (sourceBehaviour != null &&
                (!sourceBehaviour.enabled || !sourceBehaviour.gameObject.activeInHierarchy))
            {
                return false;
            }

            for (int i = 0; i < stateBehaviours.Length; i++)
            {
                MonoBehaviour behaviour = stateBehaviours[i];
                if (behaviour == null || !behaviour.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (TryReadNegativeFlowState(behaviour, out bool blocked) && blocked)
                {
                    return false;
                }

            }

            return true;
        }

        private Vector3 ReadFlowVector(
            Vector3 worldPosition,
            out float magnitude)
        {
            magnitude = 0f;
            object target = sourceBehaviour != null ? sourceBehaviour : this;

            try
            {
                object value = null;

                if (vectorMethod != null)
                {
                    value = vectorMethodUsesPosition
                        ? vectorMethod.Invoke(target, new object[] { worldPosition })
                        : vectorMethod.Invoke(target, null);
                }
                else if (vectorMember != null)
                {
                    value = GetMemberValue(vectorMember, target);
                }

                if (value is Vector3 vector)
                {
                    magnitude = vector.magnitude;
                    return vector;
                }
            }
            catch (Exception)
            {
                vectorMethod = null;
                vectorMember = null;
            }

            return transform.TransformDirection(fallbackLocalDirection);
        }

        private float ReadSpeed()
        {
            object target = sourceBehaviour != null ? sourceBehaviour : this;

            try
            {
                object value = null;

                if (speedMethod != null)
                {
                    value = speedMethod.Invoke(target, null);
                }
                else if (speedMember != null)
                {
                    value = GetMemberValue(speedMember, target);
                }

                if (value is float floatValue)
                {
                    return Mathf.Max(0f, floatValue);
                }

                if (value is double doubleValue)
                {
                    return Mathf.Max(0f, (float)doubleValue);
                }
            }
            catch (Exception)
            {
                speedMethod = null;
                speedMember = null;
            }

            return 0f;
        }

        private void CacheReflectionAccessors()
        {
            if (sourceBehaviour == null)
            {
                sourceBehaviour = FindLikelySourceBehaviour();
            }

            stateBehaviours = GetComponentsInParent<MonoBehaviour>(true);
            ResolveCollider();

            if (sourceBehaviour == null)
            {
                return;
            }

            Type type = sourceBehaviour.GetType();
            vectorMethod = FindVectorMethod(type, out vectorMethodUsesPosition);
            vectorMember = vectorMethod == null ? FindVectorMember(type) : null;
            speedMethod = FindFloatMethod(type);
            speedMember = speedMethod == null ? FindFloatMember(type) : null;
        }

        private MonoBehaviour FindLikelySourceBehaviour()
        {
            MonoBehaviour[] behaviours = GetComponentsInParent<MonoBehaviour>(true);
            MonoBehaviour best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < behaviours.Length; i++)
            {
                MonoBehaviour behaviour = behaviours[i];
                if (behaviour == null || behaviour == this)
                {
                    continue;
                }

                string typeName = behaviour.GetType().Name;
                int score = ScoreWatercolorSourceType(typeName);
                if (score > bestScore)
                {
                    best = behaviour;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static int ScoreWatercolorSourceType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return int.MinValue;
            }

            if (typeName.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) < 0 ||
                typeName.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return int.MinValue;
            }

            if (ContainsAny(
                    typeName,
                    "Interactor",
                    "Body",
                    "Debug",
                    "Spawner",
                    "Fixative",
                    "Reaction",
                    "Adapter"))
            {
                return -100;
            }

            int score = 10;
            if (typeName.Equals("WatercolorFlowSurface", StringComparison.OrdinalIgnoreCase))
            {
                score += 100;
            }

            if (typeName.IndexOf("Surface", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 50;
            }

            if (typeName.IndexOf("Runtime", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            return score;
        }

        private static MethodInfo FindVectorMethod(
            Type type,
            out bool usesPosition)
        {
            usesPosition = false;
            MethodInfo best = null;
            int bestScore = int.MinValue;

            MethodInfo[] methods = type.GetMethods(ReflectionFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.ReturnType != typeof(Vector3))
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();
                bool positionParameter =
                    parameters.Length == 1 && parameters[0].ParameterType == typeof(Vector3);

                if (parameters.Length != 0 && !positionParameter)
                {
                    continue;
                }

                int score = ScoreVectorName(method.Name);
                if (score > bestScore)
                {
                    best = method;
                    bestScore = score;
                    usesPosition = positionParameter;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static MemberInfo FindVectorMember(Type type)
        {
            MemberInfo best = null;
            int bestScore = int.MinValue;

            PropertyInfo[] properties = type.GetProperties(ReflectionFlags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property.PropertyType != typeof(Vector3) || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                int score = ScoreVectorName(property.Name);
                if (score > bestScore)
                {
                    best = property;
                    bestScore = score;
                }
            }

            FieldInfo[] fields = type.GetFields(ReflectionFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.FieldType != typeof(Vector3))
                {
                    continue;
                }

                int score = ScoreVectorName(field.Name);
                if (score > bestScore)
                {
                    best = field;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static MethodInfo FindFloatMethod(Type type)
        {
            MethodInfo best = null;
            int bestScore = int.MinValue;

            MethodInfo[] methods = type.GetMethods(ReflectionFlags);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo method = methods[i];
                if (method.ReturnType != typeof(float) || method.GetParameters().Length != 0)
                {
                    continue;
                }

                int score = ScoreSpeedName(method.Name);
                if (score > bestScore)
                {
                    best = method;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static MemberInfo FindFloatMember(Type type)
        {
            MemberInfo best = null;
            int bestScore = int.MinValue;

            PropertyInfo[] properties = type.GetProperties(ReflectionFlags);
            for (int i = 0; i < properties.Length; i++)
            {
                PropertyInfo property = properties[i];
                if (property.PropertyType != typeof(float) || property.GetIndexParameters().Length != 0)
                {
                    continue;
                }

                int score = ScoreSpeedName(property.Name);
                if (score > bestScore)
                {
                    best = property;
                    bestScore = score;
                }
            }

            FieldInfo[] fields = type.GetFields(ReflectionFlags);
            for (int i = 0; i < fields.Length; i++)
            {
                FieldInfo field = fields[i];
                if (field.FieldType != typeof(float))
                {
                    continue;
                }

                int score = ScoreSpeedName(field.Name);
                if (score > bestScore)
                {
                    best = field;
                    bestScore = score;
                }
            }

            return bestScore > 0 ? best : null;
        }

        private static int ScoreVectorName(string name)
        {
            int score = 0;
            if (name.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (name.IndexOf("Velocity", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (name.IndexOf("Direction", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 16;
            }

            if (name.IndexOf("Current", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 8;
            }

            if (name.IndexOf("World", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 4;
            }

            return score;
        }

        private static int ScoreSpeedName(string name)
        {
            int score = 0;
            if (name.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 14;
            }

            if (name.IndexOf("Speed", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 20;
            }

            if (name.IndexOf("Strength", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("Force", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 12;
            }

            if (name.IndexOf("Current", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                score += 6;
            }

            return score;
        }

        private static bool TryReadNegativeFlowState(
            MonoBehaviour behaviour,
            out bool blocked)
        {
            return TryReadNamedBool(
                behaviour,
                out blocked,
                "IsFrozen",
                "Frozen",
                "IsDepleted",
                "Depleted",
                "IsConsumed",
                "Consumed",
                "IsExpired",
                "Expired");
        }

        private static bool TryReadPositiveFlowState(
            MonoBehaviour behaviour,
            out bool active)
        {
            string typeName = behaviour.GetType().Name;
            if (typeName.IndexOf("Watercolor", StringComparison.OrdinalIgnoreCase) < 0 ||
                typeName.IndexOf("Flow", StringComparison.OrdinalIgnoreCase) < 0)
            {
                active = true;
                return false;
            }

            return TryReadNamedBool(
                behaviour,
                out active,
                "IsFlowing",
                "Flowing",
                "CanFlow",
                "IsRunning");
        }

        private static bool TryReadNamedBool(
            object target,
            out bool value,
            params string[] names)
        {
            value = false;
            if (target == null)
            {
                return false;
            }

            Type type = target.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                PropertyInfo property = type.GetProperty(names[i], ReflectionFlags);
                if (property != null &&
                    property.PropertyType == typeof(bool) &&
                    property.GetIndexParameters().Length == 0)
                {
                    try
                    {
                        value = (bool)property.GetValue(target);
                        return true;
                    }
                    catch (Exception)
                    {
                        // Ignore inaccessible prototype diagnostics.
                    }
                }

                FieldInfo field = type.GetField(names[i], ReflectionFlags);
                if (field != null && field.FieldType == typeof(bool))
                {
                    try
                    {
                        value = (bool)field.GetValue(target);
                        return true;
                    }
                    catch (Exception)
                    {
                        // Ignore inaccessible prototype diagnostics.
                    }
                }
            }

            return false;
        }

        private static object GetMemberValue(MemberInfo member, object target)
        {
            if (member is PropertyInfo property)
            {
                return property.GetValue(target);
            }

            return member is FieldInfo field ? field.GetValue(target) : null;
        }

        private static bool ContainsAny(string value, params string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
