#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

using Object = UnityEngine.Object;

namespace MB
{
    /// <summary>
    /// UE4 style model collision importer,
    /// will apply colliders to imported meshes with the appropriate prefix,
    /// </summary>
    public static class ModelCollisionImporter
    {
        public static bool Enabled => true;

        public static Dictionary<string, ColliderType> Mapping = new Dictionary<string, ColliderType>()
        {
            { "UBX", ColliderType.Box },
            { "USP", ColliderType.Sphere },
            { "UCP", ColliderType.Capsule },
            { "UCX", ColliderType.Convex },
        };

        public static bool CheckCollider(Transform transform, out ColliderType type)
        {
            var name = transform.name.ToLower();

            foreach (var entry in Mapping)
            {
                var id = entry.Key.ToLower();

                if (name.StartsWith(id))
                {
                    type = entry.Value;
                    return true;
                }
            }

            type = default;
            return false;
        }
        public static Collider AddCollider(Transform transform, ColliderType type)
        {
            var component = ColliderToType(type);
            static Type ColliderToType(ColliderType type)
            {
                switch (type)
                {
                    case ColliderType.Box:
                        return typeof(BoxCollider);

                    case ColliderType.Sphere:
                        return typeof(SphereCollider);

                    case ColliderType.Capsule:
                        return typeof(CapsuleCollider);

                    case ColliderType.Convex:
                        return typeof(MeshCollider);
                }

                throw new NotImplementedException();
            }

            var collider = transform.gameObject.AddComponent(component) as Collider;

            if (collider is MeshCollider mesh)
                mesh.convex = true;

            RemoveRenderers(transform.gameObject);
            static void RemoveRenderers(GameObject gameObject)
            {
                foreach (var renderer in gameObject.GetComponents<Renderer>())
                    Object.DestroyImmediate(renderer);

                foreach (var filter in gameObject.GetComponents<MeshFilter>())
                    Object.DestroyImmediate(filter);
            }

            return collider;
        }

        public class PostProcessor : AssetPostprocessor
        {
            void OnPostprocessModel(GameObject gameObject)
            {
                if (Enabled == false) return;

                foreach (var child in MUtility.Unity.IterateHierarchy(gameObject))
                {
                    if (CheckCollider(child, out var type) == false) continue;

                    AddCollider(child, type);
                }
            }
        }

        public enum ColliderType
        {
            Box,
            Sphere,
            Capsule,
            Convex
        }
    }
}
#endif