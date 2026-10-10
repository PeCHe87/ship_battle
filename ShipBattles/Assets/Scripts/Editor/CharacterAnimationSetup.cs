#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class CharacterAnimationSetup
{
    const string AnimFolder = "Assets/Animations/Player";
    const string ControllerPath = AnimFolder + "/PlayerLocomotion.controller";
    const string PlayerPrefabPath = "Assets/Prefabs/player.prefab";
    const string StanderPrefabPath = "Assets/Shinabro/Platform_Animation/Prefabs/Stander.prefab";

    const string IdleClipPath = "Assets/Shinabro/Platform_Animation/Animation/00_Base/Stander@Idle.FBX";
    const string RunClipPath = "Assets/Shinabro/Platform_Animation/Animation/00_Base/Stander@Run.FBX";
    const string SwimRunClipPath = "Assets/Shinabro/Platform_Animation/Animation/50_Adventure/Stander@Swim_Run.FBX";
    const string SwimIdleClipPath = "Assets/Shinabro/Platform_Animation/Animation/50_Adventure/Stander@Swim_Idle.FBX";
    const string DeadClipPath = "Assets/Shinabro/Platform_Animation/Animation/98_Damage/Stander@LyingBack.FBX";

    [MenuItem("ShipBattles/Setup Character Animation")]
    public static void SetupFromMenu()
    {
        Setup();
    }

    public static void SetupFromCommandLine()
    {
        try
        {
            Setup();
            EditorApplication.Exit(0);
        }
        catch (System.Exception ex)
        {
            Debug.LogError(ex);
            EditorApplication.Exit(1);
        }
    }

    public static void Setup()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");
        if (!AssetDatabase.IsValidFolder(AnimFolder))
            AssetDatabase.CreateFolder("Assets/Animations", "Player");

        AnimationClip idle = LoadFirstClip(IdleClipPath);
        AnimationClip running = LoadFirstClip(RunClipPath);
        AnimationClip spaceMovement = LoadFirstClip(SwimRunClipPath);
        AnimationClip spaceIdle = LoadFirstClip(SwimIdleClipPath);
        AnimationClip dead = LoadFirstClip(DeadClipPath);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null)
            controller = CreateLocomotionController(idle, running, spaceMovement, spaceIdle, dead);

        ConfigurePlayerPrefab(controller, idle, running, spaceMovement, spaceIdle, dead);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CharacterAnimationSetup] Player locomotion animation setup complete.");
    }

    static AnimatorController CreateLocomotionController(
        AnimationClip idle,
        AnimationClip running,
        AnimationClip spaceMovement,
        AnimationClip spaceIdle,
        AnimationClip dead)
    {
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        AnimatorStateMachine root = controller.layers[0].stateMachine;

        ChildAnimatorState[] children = root.states;
        for (int i = children.Length - 1; i >= 0; i--)
            root.RemoveState(children[i].state);

        AnimatorState idleState = root.AddState("Idle", new Vector3(300, 0, 0));
        idleState.motion = idle;
        AnimatorState runState = root.AddState("Running", new Vector3(300, 70, 0));
        runState.motion = running;
        AnimatorState spaceMoveState = root.AddState("SpaceMovement", new Vector3(300, 140, 0));
        spaceMoveState.motion = spaceMovement;
        AnimatorState spaceIdleState = root.AddState("SpaceIdle", new Vector3(300, 210, 0));
        spaceIdleState.motion = spaceIdle;
        AnimatorState deadState = root.AddState("Dead", new Vector3(300, 280, 0));
        deadState.motion = dead;

        root.defaultState = idleState;
        EditorUtility.SetDirty(controller);
        return controller;
    }

    static AnimationClip LoadFirstClip(string fbxPath)
    {
        AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<AnimationClip>()
            .FirstOrDefault(c => c != null && !c.name.StartsWith("__preview", System.StringComparison.Ordinal));
        if (clip == null)
            Debug.LogError($"[CharacterAnimationSetup] No AnimationClip in {fbxPath}");
        return clip;
    }

    static void ConfigurePlayerPrefab(
        RuntimeAnimatorController controller,
        AnimationClip idle,
        AnimationClip running,
        AnimationClip spaceMovement,
        AnimationClip spaceIdle,
        AnimationClip dead)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        try
        {
            Transform art = root.transform.Find("art");
            if (art == null)
            {
                var artGo = new GameObject("art");
                artGo.transform.SetParent(root.transform, false);
                art = artGo.transform;
            }

            foreach (Transform child in art)
            {
                if (child.name.StartsWith("Cube", System.StringComparison.Ordinal))
                    child.gameObject.SetActive(false);
            }

            Transform stander = art.Find("StanderVisual");
            if (stander == null)
            {
                GameObject standerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StanderPrefabPath);
                if (standerPrefab == null)
                    throw new FileNotFoundException(StanderPrefabPath);

                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(standerPrefab, art);
                instance.name = "StanderVisual";
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                stander = instance.transform;

                Rigidbody rb = instance.GetComponent<Rigidbody>();
                if (rb != null)
                    Object.DestroyImmediate(rb);
            }

            Animator animator = stander.GetComponent<Animator>();
            if (animator == null)
                animator = stander.gameObject.AddComponent<Animator>();
            animator.applyRootMotion = false;
            animator.runtimeAnimatorController = controller;

            CharacterAnimationManager manager = root.GetComponent<CharacterAnimationManager>();
            if (manager == null)
                manager = root.AddComponent<CharacterAnimationManager>();

            SerializedObject so = new SerializedObject(manager);
            so.FindProperty("animator").objectReferenceValue = animator;
            so.FindProperty("baseController").objectReferenceValue = controller;
            so.FindProperty("idle").objectReferenceValue = idle;
            so.FindProperty("running").objectReferenceValue = running;
            so.FindProperty("spaceMovement").objectReferenceValue = spaceMovement;
            so.FindProperty("spaceIdle").objectReferenceValue = spaceIdle;
            so.FindProperty("dead").objectReferenceValue = dead;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
#endif
