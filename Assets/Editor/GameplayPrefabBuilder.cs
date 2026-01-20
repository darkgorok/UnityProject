using System.IO;
using UnityEditor;
using UnityEngine;

public static class GameplayPrefabBuilder
{
    private const string BasePath = "Assets/Prefabs/Gameplay";

    [MenuItem("Tools/AlphaTest/Create Gameplay Prefabs")]
    public static void CreatePrefabs()
    {
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(BasePath);

        var projectilePrefabPath = Path.Combine(BasePath, "Projectile.prefab");
        var obstaclePrefabPath = Path.Combine(BasePath, "Obstacle.prefab");
        var doorPrefabPath = Path.Combine(BasePath, "Door.prefab");
        var playerPrefabPath = Path.Combine(BasePath, "Player.prefab");
        var levelPrefabPath = Path.Combine(BasePath, "Level.prefab");

        var projectilePrefab = CreateProjectilePrefab(projectilePrefabPath);
        var obstaclePrefab = CreateObstaclePrefab(obstaclePrefabPath);
        var doorPrefab = CreateDoorPrefab(doorPrefabPath);
        CreatePlayerPrefab(playerPrefabPath, projectilePrefab);
        CreateLevelPrefab(levelPrefabPath, doorPrefab, obstaclePrefab);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static GameObject CreateProjectilePrefab(string path)
    {
        DeleteIfExists(path);

        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Projectile";
        root.transform.localScale = Vector3.one * 0.5f;

        var collider = root.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        var rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = false;
        rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

        root.AddComponent<Projectile>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateObstaclePrefab(string path)
    {
        DeleteIfExists(path);

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Obstacle";
        root.transform.localScale = new Vector3(1f, 1f, 1f);
        root.AddComponent<Obstacle>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateDoorPrefab(string path)
    {
        DeleteIfExists(path);

        var root = new GameObject("Door");
        var collider = root.AddComponent<SphereCollider>();
        collider.isTrigger = true;
        collider.radius = 5f;

        root.AddComponent<DoorController>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreatePlayerPrefab(string path, GameObject projectilePrefab)
    {
        DeleteIfExists(path);

        var root = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        root.name = "Player";
        root.transform.localScale = Vector3.one;

        var collider = root.GetComponent<Collider>();
        if (collider != null)
            collider.isTrigger = false;

        var rigidbody = root.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;

        var scaleController = root.AddComponent<PlayerScaleController>();
        var squashAnimator = root.AddComponent<PlayerSquashAnimator>();
        var spawner = root.AddComponent<PlayerProjectileSpawner>();
        var shootInput = root.AddComponent<PlayerShootInput>();
        var shootGate = root.AddComponent<PlayerShootGate>();
        var failWatcher = root.AddComponent<PlayerFailWatcher>();
        var movement = root.AddComponent<PlayerMovement>();
        var shooting = root.AddComponent<PlayerShooting>();

        var spawnerSerialized = new SerializedObject(spawner);
        spawnerSerialized.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab != null
            ? projectilePrefab.GetComponent<Projectile>()
            : null;
        spawnerSerialized.ApplyModifiedPropertiesWithoutUndo();

        var shootingSerialized = new SerializedObject(shooting);
        shootingSerialized.FindProperty("shootInput").objectReferenceValue = shootInput;
        shootingSerialized.FindProperty("shootGate").objectReferenceValue = shootGate;
        shootingSerialized.FindProperty("failWatcher").objectReferenceValue = failWatcher;
        shootingSerialized.FindProperty("movement").objectReferenceValue = movement;
        shootingSerialized.FindProperty("scaleController").objectReferenceValue = scaleController;
        shootingSerialized.FindProperty("squashAnimator").objectReferenceValue = squashAnimator;
        shootingSerialized.FindProperty("projectileSpawner").objectReferenceValue = spawner;
        shootingSerialized.ApplyModifiedPropertiesWithoutUndo();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateLevelPrefab(
        string path,
        GameObject doorPrefab,
        GameObject obstaclePrefab)
    {
        DeleteIfExists(path);

        var root = new GameObject("LevelRoot");

        var door = PrefabUtility.InstantiatePrefab(doorPrefab) as GameObject;
        if (door != null)
        {
            door.name = "Door";
            door.transform.SetParent(root.transform);
            door.transform.localPosition = new Vector3(8f, 0f, 8f);
        }

        var aim = new GameObject("GoalAim");
        aim.transform.SetParent(root.transform);
        var aimProvider = aim.AddComponent<GoalAimDirectionProvider>();
        if (door != null)
        {
            var aimSerialized = new SerializedObject(aimProvider);
            aimSerialized.FindProperty("target").objectReferenceValue = door.transform;
            aimSerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var facade = root.AddComponent<LevelFacade>();
        var facadeSerialized = new SerializedObject(facade);
        facadeSerialized.FindProperty("door").objectReferenceValue = door != null
            ? door.GetComponent<DoorController>()
            : null;
        facadeSerialized.FindProperty("aimProvider").objectReferenceValue = aimProvider;
        facadeSerialized.ApplyModifiedPropertiesWithoutUndo();

        var installer = root.AddComponent<LevelInstaller>();
        var installerSerialized = new SerializedObject(installer);
        installerSerialized.FindProperty("facade").objectReferenceValue = facade;
        installerSerialized.ApplyModifiedPropertiesWithoutUndo();

        var context = root.AddComponent<Zenject.GameObjectContext>();
        var contextSerialized = new SerializedObject(context);
        var installersProp = contextSerialized.FindProperty("_monoInstallers");
        installersProp.arraySize = 1;
        installersProp.GetArrayElementAtIndex(0).objectReferenceValue = installer;
        contextSerialized.ApplyModifiedPropertiesWithoutUndo();

        var obstaclesRoot = new GameObject("Obstacles");
        obstaclesRoot.transform.SetParent(root.transform);

        if (obstaclePrefab != null)
        {
            for (var i = 0; i < 3; i++)
            {
                var obstacle = PrefabUtility.InstantiatePrefab(obstaclePrefab) as GameObject;
                if (obstacle == null)
                    continue;

                obstacle.transform.SetParent(obstaclesRoot.transform);
                obstacle.transform.localPosition = new Vector3(2f + i * 2f, 0f, 2f + i * 2f);
            }
        }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        var parent = Path.GetDirectoryName(path);
        var name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        AssetDatabase.CreateFolder(parent ?? "Assets", name);
    }

    private static void DeleteIfExists(string path)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            AssetDatabase.DeleteAsset(path);
    }
}
