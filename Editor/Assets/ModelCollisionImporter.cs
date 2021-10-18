#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;

namespace MB
{
    /// <summary>
    /// UE4 style model collision importer,
    /// will apply colliders to imported meshes with the appropriate prefix,
    /// exposes a project settings entry under "Moe Baker/Model Collision Importer"
    /// </summary>
    [Global(ScriptableManagerScope.Project)]
    [EditorOnly]
    [SettingsMenu(Toolbox.Paths.Root + "Model Collision Importer")]
    public class ModelCollisionImporter : ScriptableManager<ModelCollisionImporter>
    {
        [SerializeField]
        bool enabled = true;
        public bool Enabled => enabled;

        [SerializeField]
        UDictionary<string, ColliderType> mapping = default;
        public UDictionary<string, ColliderType> Mapping => mapping;

        void Reset()
        {
            mapping = new UDictionary<string, ColliderType>();
            mapping.Add("UBX", ColliderType.Box);
            mapping.Add("USP", ColliderType.Sphere);
            mapping.Add("UCP", ColliderType.Capsule);
            mapping.Add("UCX", ColliderType.Convex);
        }

        public bool CheckCollider(Transform transform, out ColliderType type)
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

        public Collider AddCollider(Transform transform, ColliderType type)
        {
            var component = ColliderToType(type);

            var collider = transform.gameObject.AddComponent(component) as Collider;

            if (collider is MeshCollider mesh)
                mesh.convex = true;

            RemoveRenderers(transform.gameObject);

            return collider;
        }

        public void RemoveRenderers(GameObject gameObject)
        {
            foreach (var renderer in gameObject.GetComponents<Renderer>())
                DestroyImmediate(renderer);

            foreach (var filter in gameObject.GetComponents<MeshFilter>())
                DestroyImmediate(filter);
        }

        public class PostProcessor : AssetPostprocessor
        {
            static ModelCollisionImporter Manager => ModelCollisionImporter.Instance;

            void OnPostprocessModel(GameObject gameObject)
            {
                if (Manager.Enabled == false) return;

                foreach (var child in MUtility.IterateTransformHierarchy(gameObject))
                {
                    if (Manager.CheckCollider(child, out var type) == false) continue;

                    Manager.AddCollider(child, type);
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

        public static Type ColliderToType(ColliderType type)
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
    }
}
#endif