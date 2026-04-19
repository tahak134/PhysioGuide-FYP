using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Editor tool that generates a complete modern 3D gym environment for the PhysioGuide VR rehab system.
/// Uses PolyRonin "Old School Gym Equipment" prefabs for equipment and procedural geometry for the room.
/// Run via: PhysioGuide > Build Gym Scene 3D
/// </summary>
public class PhysioGuideGymBuilder : EditorWindow
{
    // ------------------------------------------
    // ROOM DIMENSIONS
    // ------------------------------------------
    private const float ROOM_W = 16f;   // X-axis (side to side)
    private const float ROOM_D = 12f;   // Z-axis (front to back)
    private const float ROOM_H = 4.5f;  // Y-axis (floor to ceiling)
    private const float WALL_T = 0.15f; // Wall thickness

    // Mirror wall is at +Z (the wall avatars face)
    private const float MIRROR_Z = ROOM_D / 2f;

    // Avatar exercise area center
    private const float EXERCISE_Z = 1.0f; // Slightly forward of room center, facing +Z mirror

    // ------------------------------------------
    // PREFAB PATHS (PolyRonin Old School Gym Equipment)
    // ------------------------------------------
    private const string PREFAB_ROOT = "Assets/PolyRonin/Old School Gym Equipment/Prefabs/";
    private const string POSTER_ROOT = "Assets/_Project/Materials/";

    private static readonly string[] EQUIPMENT_PREFABS = new string[]
    {
        "BenchPress",
        "SquatRack",
        "DumbbellsRack",
        "LegPress",
        "PullUp-Dips-Abs",
        "PlatesRack",
        "Bench",
        "Dumbbell1",
        "Dumbbell5",
        "Plate1",
        "Kettlebell1",
    };

    private const string ROOT_NAME = "GymEnvironment";

    [MenuItem("PhysioGuide/Build Gym Scene 3D")]
    public static void BuildGymScene()
    {
        // -------------------------------------------
        // 0. CLEANUP previous generation
        // -------------------------------------------
        GameObject oldGym = GameObject.Find(ROOT_NAME);
        if (oldGym != null)
        {
            Undo.DestroyObjectImmediate(oldGym);
            Debug.Log("[GymBuilder] Removed previous gym environment.");
        }

        // Root container
        GameObject root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Build Gym Scene 3D");

        // -------------------------------------------
        // 1. MATERIALS (URP Lit)
        // -------------------------------------------
        Material matFloor    = CreateURPMaterial("Gym_Floor",      new Color(0.12f, 0.08f, 0.06f), 0.0f, 0.85f);
        Material matWall     = CreateURPMaterial("Gym_Wall",       new Color(0.18f, 0.18f, 0.20f), 0.0f, 0.3f);
        Material matCeiling  = CreateURPMaterial("Gym_Ceiling",    new Color(0.22f, 0.22f, 0.24f), 0.0f, 0.2f);
        Material matMirror   = CreateURPMaterial("Gym_Mirror",     new Color(0.85f, 0.88f, 0.90f), 1.0f, 0.95f);
        Material matTrim     = CreateURPMaterial("Gym_Trim",       new Color(0.10f, 0.10f, 0.12f), 0.8f, 0.7f);
        Material matYogaBlue = CreateURPMaterial("Gym_YogaMat_Blue", new Color(0.15f, 0.35f, 0.65f), 0.0f, 0.4f);
        Material matYogaRed  = CreateURPMaterial("Gym_YogaMat_Red",  new Color(0.65f, 0.18f, 0.18f), 0.0f, 0.4f);
        Material matRubber   = CreateURPMaterial("Gym_RubberMat",  new Color(0.08f, 0.08f, 0.08f), 0.0f, 0.15f);
        Material matAccent   = CreateURPMaterial("Gym_Accent",     new Color(0.20f, 0.55f, 0.78f), 0.0f, 0.5f);
        Material matMirrorFrame = CreateURPMaterial("Gym_MirrorFrame", new Color(0.06f, 0.06f, 0.08f), 0.9f, 0.8f);

        // -------------------------------------------
        // 2. ROOM STRUCTURE
        // -------------------------------------------
        GameObject structure = new GameObject("Structure");
        structure.transform.SetParent(root.transform, false);

        // Floor
        CreateBox("Floor", structure.transform,
            new Vector3(0, -0.05f, 0), new Vector3(ROOM_W, 0.1f, ROOM_D), matFloor);

        // Ceiling
        CreateBox("Ceiling", structure.transform,
            new Vector3(0, ROOM_H + 0.05f, 0), new Vector3(ROOM_W + 0.3f, 0.1f, ROOM_D + 0.3f), matCeiling);

        // Back wall (-Z)
        CreateBox("Wall_Back", structure.transform,
            new Vector3(0, ROOM_H / 2f, -ROOM_D / 2f - WALL_T / 2f),
            new Vector3(ROOM_W + WALL_T * 2, ROOM_H, WALL_T), matWall);

        // Left wall (-X)
        CreateBox("Wall_Left", structure.transform,
            new Vector3(-ROOM_W / 2f - WALL_T / 2f, ROOM_H / 2f, 0),
            new Vector3(WALL_T, ROOM_H, ROOM_D + WALL_T * 2), matWall);

        // Right wall (+X)
        CreateBox("Wall_Right", structure.transform,
            new Vector3(ROOM_W / 2f + WALL_T / 2f, ROOM_H / 2f, 0),
            new Vector3(WALL_T, ROOM_H, ROOM_D + WALL_T * 2), matWall);

        // Front wall (+Z) - solid wall, same material as other walls
        CreateBox("Wall_Front", structure.transform,
            new Vector3(0, ROOM_H / 2f, MIRROR_Z + WALL_T / 2f),
            new Vector3(ROOM_W, ROOM_H, WALL_T), matWall);

        // Mirror panel mounted on the front wall (75% width, 80% height)
        // Uses a Quad (single face, clean UVs) instead of a Cube
        float mirrorW = ROOM_W * 0.75f;
        float mirrorH = ROOM_H * 0.80f;
        float mirrorBottomGap = ROOM_H * 0.10f; // 10% gap from floor
        float mirrorCenterY = mirrorBottomGap + mirrorH / 2f;
        float mirrorSurfaceZ = MIRROR_Z - 0.005f; // Slightly in front of the wall

        GameObject mirrorPanel = GameObject.CreatePrimitive(PrimitiveType.Quad);
        mirrorPanel.name = "MirrorPanel";
        mirrorPanel.transform.SetParent(structure.transform, false);
        mirrorPanel.transform.localPosition = new Vector3(0, mirrorCenterY, mirrorSurfaceZ);
        // Quad faces +Z by default; rotate 180 so it faces -Z (into the room)
        mirrorPanel.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        mirrorPanel.transform.localScale = new Vector3(mirrorW, mirrorH, 1f);
        mirrorPanel.isStatic = false;

        Renderer mirrorRend = mirrorPanel.GetComponent<Renderer>();
        if (mirrorRend != null) mirrorRend.sharedMaterial = matMirror;

        // Remove collider from mirror quad (not needed, wall behind handles collision)
        Collider mirrorCol = mirrorPanel.GetComponent<Collider>();
        if (mirrorCol != null) Object.DestroyImmediate(mirrorCol);

        // Attach PlanarMirror component for real-time reflections
        PlanarMirror mirror = mirrorPanel.AddComponent<PlanarMirror>();
        mirror.mirrorWidth = mirrorW;
        mirror.mirrorHeight = mirrorH;
        mirror.textureHeight = 1024;
        mirror.farClipPlane = 30f;

        // Mirror frame (dark border around the mirror panel)
        float frameT = 0.06f; // Frame thickness
        float frameDepth = 0.04f;
        // Top frame
        CreateBox("MirrorFrame_Top", structure.transform,
            new Vector3(0, mirrorCenterY + mirrorH / 2f + frameT / 2f, mirrorSurfaceZ),
            new Vector3(mirrorW + frameT * 2f, frameT, frameDepth), matMirrorFrame);
        // Bottom frame
        CreateBox("MirrorFrame_Bottom", structure.transform,
            new Vector3(0, mirrorCenterY - mirrorH / 2f - frameT / 2f, mirrorSurfaceZ),
            new Vector3(mirrorW + frameT * 2f, frameT, frameDepth), matMirrorFrame);
        // Left frame
        CreateBox("MirrorFrame_Left", structure.transform,
            new Vector3(-mirrorW / 2f - frameT / 2f, mirrorCenterY, mirrorSurfaceZ),
            new Vector3(frameT, mirrorH + frameT * 2f, frameDepth), matMirrorFrame);
        // Right frame
        CreateBox("MirrorFrame_Right", structure.transform,
            new Vector3(mirrorW / 2f + frameT / 2f, mirrorCenterY, mirrorSurfaceZ),
            new Vector3(frameT, mirrorH + frameT * 2f, frameDepth), matMirrorFrame);

        // Floor accent stripe (blue line running along exercise area)
        CreateBox("FloorStripe", structure.transform,
            new Vector3(0, 0.005f, EXERCISE_Z),
            new Vector3(6f, 0.01f, 0.08f), matAccent);

        // Rubber exercise mat zone
        CreateBox("RubberMat_Zone", structure.transform,
            new Vector3(0, 0.005f, EXERCISE_Z),
            new Vector3(5f, 0.01f, 3f), matRubber);

        // Baseboard trim on all walls
        float baseH = 0.12f;
        CreateBox("Baseboard_Back", structure.transform,
            new Vector3(0, baseH / 2f, -ROOM_D / 2f + 0.01f),
            new Vector3(ROOM_W, baseH, 0.04f), matTrim);
        CreateBox("Baseboard_Left", structure.transform,
            new Vector3(-ROOM_W / 2f + 0.01f, baseH / 2f, 0),
            new Vector3(0.04f, baseH, ROOM_D), matTrim);
        CreateBox("Baseboard_Right", structure.transform,
            new Vector3(ROOM_W / 2f - 0.01f, baseH / 2f, 0),
            new Vector3(0.04f, baseH, ROOM_D), matTrim);

        // -------------------------------------------
        // 3. LIGHTING
        // -------------------------------------------
        GameObject lighting = new GameObject("Lighting");
        lighting.transform.SetParent(root.transform, false);

        // Directional light (warm sunlight coming from above-right)
        GameObject sunGO = new GameObject("DirectionalLight_Sun");
        sunGO.transform.SetParent(lighting.transform, false);
        sunGO.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        Light sun = sunGO.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.88f);
        sun.intensity = 1.2f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.6f;

        // Overhead spotlights in a 2x3 grid
        Color spotColor = new Color(1f, 0.98f, 0.95f);
        float spotIntensity = 25f;
        float spotRange = 10f;
        float spotAngle = 90f;

        float[] xPositions = { -4f, 0f, 4f };
        float[] zPositions = { -2.5f, 2.5f };

        int spotIdx = 0;
        foreach (float sz in zPositions)
        {
            foreach (float sx in xPositions)
            {
                GameObject spotGO = new GameObject($"Spotlight_{spotIdx++}");
                spotGO.transform.SetParent(lighting.transform, false);
                spotGO.transform.position = new Vector3(sx, ROOM_H - 0.3f, sz);
                spotGO.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // Pointing down

                Light spot = spotGO.AddComponent<Light>();
                spot.type = LightType.Spot;
                spot.color = spotColor;
                spot.intensity = spotIntensity;
                spot.range = spotRange;
                spot.spotAngle = spotAngle;
                spot.innerSpotAngle = spotAngle * 0.7f;
                spot.shadows = LightShadows.Soft;
                spot.shadowStrength = 0.4f;

                // Light fixture housing (visual only)
                CreateBox($"LightFixture_{spotIdx}", lighting.transform,
                    new Vector3(sx, ROOM_H - 0.05f, sz),
                    new Vector3(0.5f, 0.08f, 0.5f), matTrim);
            }
        }

        // -------------------------------------------
        // 4. REFLECTION PROBE (environment fallback for non-mirror surfaces)
        // -------------------------------------------
        GameObject probeGO = new GameObject("ReflectionProbe_Room");
        probeGO.transform.SetParent(root.transform, false);
        probeGO.transform.position = new Vector3(0, ROOM_H / 2f, 0);
        ReflectionProbe probe = probeGO.AddComponent<ReflectionProbe>();
        probe.mode = ReflectionProbeMode.Baked;
        probe.size = new Vector3(ROOM_W + 2f, ROOM_H + 2f, ROOM_D + 2f);
        probe.resolution = 256;
        probe.hdr = true;
        probe.boxProjection = true;

        // -------------------------------------------
        // 5. GYM EQUIPMENT (PolyRonin Prefabs)
        // -------------------------------------------
        GameObject equipment = new GameObject("Equipment");
        equipment.transform.SetParent(root.transform, false);

        // Layout: Equipment along walls, leaving center open for exercise
        // Back wall area (-Z side)
        SpawnPrefab("SquatRack",       equipment.transform, new Vector3(-4.5f, 0, -4.5f), 0f);
        SpawnPrefab("BenchPress",      equipment.transform, new Vector3(-1.5f, 0, -4.5f), 0f);
        SpawnPrefab("Bench",           equipment.transform, new Vector3(1.5f, 0, -4.5f), 0f);

        // Right wall area (+X side)
        SpawnPrefab("LegPress",        equipment.transform, new Vector3(6f, 0, -2f), -90f);
        SpawnPrefab("PullUp-Dips-Abs", equipment.transform, new Vector3(6f, 0, 1.5f), -90f);

        // Left wall area (-X side)
        SpawnPrefab("DumbbellsRack",   equipment.transform, new Vector3(-6.5f, 0, -1f), 90f);
        SpawnPrefab("PlatesRack",      equipment.transform, new Vector3(-6.5f, 0, 2f), 90f);

        // Scattered small props
        SpawnPrefab("Dumbbell1",       equipment.transform, new Vector3(-3f, 0, -2f), 15f);
        SpawnPrefab("Dumbbell5",       equipment.transform, new Vector3(-3.3f, 0, -1.7f), -10f);
        SpawnPrefab("Kettlebell1",     equipment.transform, new Vector3(4f, 0, -2.5f), 0f);
        SpawnPrefab("Plate1",          equipment.transform, new Vector3(4.5f, 0, -3f), 30f);

        // -------------------------------------------
        // 6. YOGA MATS (procedural)
        // -------------------------------------------
        GameObject props = new GameObject("Props");
        props.transform.SetParent(root.transform, false);

        CreateYogaMat("YogaMat_Blue", props.transform,
            new Vector3(3.5f, 0.01f, -1f), 5f, matYogaBlue);
        CreateYogaMat("YogaMat_Red", props.transform,
            new Vector3(4.5f, 0.01f, 0.5f), -20f, matYogaRed);

        // -------------------------------------------
        // 7. WALL POSTERS
        // -------------------------------------------
        SpawnPoster("Poster_1", props.transform,
            POSTER_ROOT + "life-better-gym-tshirt-poster-600w-1678297390.png",
            new Vector3(-ROOM_W / 2f + 0.02f, 2.2f, -2f),
            new Vector3(1.2f, 1.6f, 0.02f),
            Quaternion.Euler(0, 90, 0));

        SpawnPoster("Poster_2", props.transform,
            POSTER_ROOT + "poster-fitness-center-grunge-style-260nw-542633353.png",
            new Vector3(-ROOM_W / 2f + 0.02f, 2.2f, 1.5f),
            new Vector3(1.2f, 1.6f, 0.02f),
            Quaternion.Euler(0, 90, 0));

        // -------------------------------------------
        // 8. AVATAR SPAWN POINTS
        // -------------------------------------------
        GameObject spawns = new GameObject("AvatarSpawns");
        spawns.transform.SetParent(root.transform, false);

        // Facing +Z (toward mirror wall), separated by 2m, centered
        GameObject trainerSpawn = CreateSpawnPoint("TrainerAvatar_Spawn", spawns.transform,
            new Vector3(-1.2f, 0f, EXERCISE_Z),
            Quaternion.Euler(0, 0, 0),  // Facing +Z (toward mirror)
            new Color(0.2f, 0.8f, 0.3f, 0.8f));

        GameObject patientSpawn = CreateSpawnPoint("PatientAvatar_Spawn", spawns.transform,
            new Vector3(1.2f, 0f, EXERCISE_Z),
            Quaternion.Euler(0, 0, 0),  // Facing +Z (toward mirror)
            new Color(0.3f, 0.5f, 1.0f, 0.8f));

        // -------------------------------------------
        // 9. AMBIENT SETTINGS
        // -------------------------------------------
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.18f);

        // -------------------------------------------
        // DONE
        // -------------------------------------------
        Selection.activeGameObject = root;
        EditorUtility.SetDirty(root);
        Debug.Log("<color=cyan>[PhysioGuide]</color> 3D Gym Environment built! Equipment, lighting, mirror wall, and avatar spawn points are ready.");
    }

    // -------------------------------------------
    // HELPER: Create URP Lit Material
    // -------------------------------------------
    private static Material CreateURPMaterial(string name, Color baseColor, float metallic, float smoothness)
    {
        // Try URP Lit first, fall back to Standard
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogWarning($"[GymBuilder] Could not find URP/Lit or Standard shader for '{name}'.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        Material mat = new Material(shader);
        mat.name = name;

        // URP Lit property names
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", baseColor);

        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);

        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        // URP surface type: 0 = Opaque
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 0);

        return mat;
    }

    // -------------------------------------------
    // HELPER: Create Box (Cube)
    // -------------------------------------------
    private static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = scale;
        go.isStatic = true;

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;

        return go;
    }

    // -------------------------------------------
    // HELPER: Spawn PolyRonin Prefab
    // -------------------------------------------
    private static GameObject SpawnPrefab(string prefabName, Transform parent, Vector3 position, float yRotation)
    {
        string path = PREFAB_ROOT + prefabName + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[GymBuilder] Prefab not found: {path}. Skipping.");
            return null;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.SetParent(parent, false);
        instance.transform.localPosition = position;
        instance.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
        instance.isStatic = true;

        return instance;
    }

    // -------------------------------------------
    // HELPER: Create Yoga Mat (thin box)
    // -------------------------------------------
    private static void CreateYogaMat(string name, Transform parent, Vector3 position, float yRotation, Material mat)
    {
        GameObject yogaMat = GameObject.CreatePrimitive(PrimitiveType.Cube);
        yogaMat.name = name;
        yogaMat.transform.SetParent(parent, false);
        yogaMat.transform.localPosition = position;
        yogaMat.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
        yogaMat.transform.localScale = new Vector3(0.65f, 0.015f, 1.8f);
        yogaMat.isStatic = true;

        Renderer rend = yogaMat.GetComponent<Renderer>();
        if (rend != null && mat != null) rend.sharedMaterial = mat;
    }

    // -------------------------------------------
    // HELPER: Spawn Wall Poster
    // -------------------------------------------
    private static void SpawnPoster(string name, Transform parent, string texturePath,
        Vector3 position, Vector3 scale, Quaternion rotation)
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);

        Material posterMat = CreateURPMaterial(name + "_Mat", Color.white, 0f, 0.1f);
        if (tex != null)
        {
            if (posterMat.HasProperty("_BaseMap"))
                posterMat.SetTexture("_BaseMap", tex);
            else if (posterMat.HasProperty("_MainTex"))
                posterMat.SetTexture("_MainTex", tex);
        }
        else
        {
            Debug.LogWarning($"[GymBuilder] Poster texture not found: {texturePath}");
        }

        GameObject poster = GameObject.CreatePrimitive(PrimitiveType.Cube);
        poster.name = name;
        poster.transform.SetParent(parent, false);
        poster.transform.localPosition = position;
        poster.transform.localRotation = rotation;
        poster.transform.localScale = scale;
        poster.isStatic = true;

        Renderer rend = poster.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = posterMat;

        // Poster frame
        GameObject frame = GameObject.CreatePrimitive(PrimitiveType.Cube);
        frame.name = name + "_Frame";
        frame.transform.SetParent(poster.transform, false);
        frame.transform.localPosition = Vector3.zero;
        frame.transform.localScale = new Vector3(1.06f, 1.04f, 0.9f);
        frame.isStatic = true;

        Material frameMat = CreateURPMaterial(name + "_FrameMat", new Color(0.06f, 0.06f, 0.06f), 0.5f, 0.6f);
        Renderer frameRend = frame.GetComponent<Renderer>();
        if (frameRend != null) frameRend.sharedMaterial = frameMat;
    }

    // -------------------------------------------
    // HELPER: Create Spawn Point with Gizmo Component
    // -------------------------------------------
    private static GameObject CreateSpawnPoint(string name, Transform parent, Vector3 position,
        Quaternion rotation, Color gizmoColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;

        SpawnPointGizmo gizmo = go.AddComponent<SpawnPointGizmo>();
        gizmo.gizmoColor = gizmoColor;
        gizmo.label = name.Replace("_Spawn", "").Replace("_", " ");

        return go;
    }
}
