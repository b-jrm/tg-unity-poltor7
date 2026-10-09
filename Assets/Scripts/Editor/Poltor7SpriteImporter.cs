#if UNITY_EDITOR
// POLTOR7 - Importador automatico de spritesheets 2D.
// Guardar en: Assets/Scripts/Editor/Poltor7SpriteImporter.cs  (cualquier carpeta Editor sirve)
// Uso: menu  Poltor7 > Importar sprites 2D
//
// Busca solo la carpeta que contiene animations.json (en cualquier lugar de Assets).
// Esa carpeta debe tener ademas la subcarpeta sheets/ con <Nombre>_sheet.png
// Ejemplo: Assets/Art/Sprites2D/animations.json  y  Assets/Art/Sprites2D/sheets/Ike_sheet.png
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public static class Poltor7SpriteImporter
{
    // Se calcula al ejecutar: es la carpeta donde esta animations.json
    static string Root;

    static string FindRoot()
    {
        foreach (string guid in AssetDatabase.FindAssets("animations"))
        {
            string p = AssetDatabase.GUIDToAssetPath(guid);
            if (p.EndsWith("/animations.json") && AssetDatabase.IsValidFolder(Path.GetDirectoryName(p).Replace('\\', '/') + "/sheets"))
                return Path.GetDirectoryName(p).Replace('\\', '/');
        }
        return null;
    }

    [Serializable] class AnimInfo { public string name; public int row; public int frames; public int fps; public bool loop; }
    [Serializable] class CharInfo { public string name; public string display; public string file; public int cell; public float pivotX; public float pivotY; public List<AnimInfo> anims; }
    [Serializable] class Meta { public int ppu; public List<CharInfo> characters; }

    [MenuItem("Poltor7/Importar sprites 2D")]
    public static void Run()
    {
        Root = FindRoot();
        if (Root == null)
        {
            EditorUtility.DisplayDialog("POLTOR7",
                "No se encontro animations.json junto a una carpeta 'sheets'.\n\n" +
                "Copie la carpeta Poltor7_Sprites2D (con animations.json y sheets) dentro de Assets, " +
                "por ejemplo Assets/Art/Sprites2D, espere a que Unity la importe y vuelva a ejecutar.", "OK");
            return;
        }
        string jsonPath = Root + "/animations.json";
        Debug.Log("POLTOR7: usando carpeta " + Root);

        Meta meta = JsonUtility.FromJson<Meta>(AssetDatabase.LoadAssetAtPath<TextAsset>(jsonPath).text);
        int count = 0;
        foreach (CharInfo ch in meta.characters)
        {
            string sheetPath = Root + "/sheets/" + ch.file;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(sheetPath) == null) { Debug.LogWarning("Falta " + sheetPath); continue; }

            SliceSheet(sheetPath, ch, meta.ppu);
            Dictionary<string, Sprite> sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(sheetPath)
                .OfType<Sprite>().ToDictionary(s => s.name, s => s);

            string animDir = Root + "/Animations/" + ch.name;
            Directory.CreateDirectory(animDir);
            AssetDatabase.Refresh();

            List<(AnimInfo info, AnimationClip clip)> clips = new List<(AnimInfo, AnimationClip)>();
            foreach (AnimInfo a in ch.anims)
            {
                AnimationClip clip = BuildClip(ch, a, sprites, animDir);
                clips.Add((a, clip));
            }

            AnimatorController ctrl = BuildController(ch, clips, animDir);
            BuildPrefab(ch, sprites, ctrl);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("POLTOR7", "Personajes importados: " + count + "\nPrefabs en " + Root + "/Prefabs", "OK");
    }

    // ---------- 1. Cortar el spritesheet en celdas ----------
    static void SliceSheet(string path, CharInfo ch, int ppu)
    {
        TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(path);
        ti.textureType = TextureImporterType.Sprite;
        ti.spriteImportMode = SpriteImportMode.Multiple;
        ti.spritePixelsPerUnit = ppu;
        ti.filterMode = FilterMode.Point;
        ti.textureCompression = TextureImporterCompression.Uncompressed;
        ti.mipmapEnabled = false;
        ti.alphaIsTransparency = true;
        ti.SaveAndReimport();

        int texH = ch.anims.Count * ch.cell;
        List<SpriteRect> rects = new List<SpriteRect>();
        foreach (AnimInfo a in ch.anims)
        {
            for (int i = 0; i < a.frames; i++)
            {
                SpriteRect r = new SpriteRect();
                r.name = FrameName(ch, a, i);
                r.rect = new Rect(i * ch.cell, texH - (a.row + 1) * ch.cell, ch.cell, ch.cell);
                r.alignment = SpriteAlignment.Custom;
                r.pivot = new Vector2(ch.pivotX, ch.pivotY);
                r.spriteID = GUID.Generate();
                rects.Add(r);
            }
        }

        SpriteDataProviderFactories factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider dp = factory.GetSpriteEditorDataProviderFromObject(ti);
        dp.InitSpriteEditorDataProvider();
        dp.SetSpriteRects(rects.ToArray());
        dp.Apply();
        ((AssetImporter)dp.targetObject).SaveAndReimport();
    }

    static string FrameName(CharInfo ch, AnimInfo a, int i)
    {
        return ch.name + "_" + a.name + "_" + i.ToString("00");
    }

    // ---------- 2. Crear AnimationClip ----------
    static AnimationClip BuildClip(CharInfo ch, AnimInfo a, Dictionary<string, Sprite> sprites, string dir)
    {
        AnimationClip clip = new AnimationClip();
        clip.frameRate = a.fps;

        List<ObjectReferenceKeyframe> keys = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < a.frames; i++)
        {
            keys.Add(new ObjectReferenceKeyframe { time = i / (float)a.fps, value = sprites[FrameName(ch, a, i)] });
        }
        // Ultimo cuadro repetido para que dure su tiempo completo
        keys.Add(new ObjectReferenceKeyframe { time = a.frames / (float)a.fps, value = sprites[FrameName(ch, a, a.frames - 1)] });

        EditorCurveBinding binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys.ToArray());

        AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = a.loop;
        AnimationUtility.SetAnimationClipSettings(clip, s);

        string p = dir + "/" + ch.name + "_" + a.name + ".anim";
        AssetDatabase.DeleteAsset(p);
        AssetDatabase.CreateAsset(clip, p);
        return clip;
    }

    // ---------- 3. Crear AnimatorController ----------
    // Parametros: bool para animaciones en bucle (Walk, Run, Move, Talk...)
    //             trigger para las que se ejecutan una vez (Attack, Shoot, Hurt, Die...)
    static AnimatorController BuildController(CharInfo ch, List<(AnimInfo info, AnimationClip clip)> clips, string dir)
    {
        string path = dir + "/AC_" + ch.name + ".controller";
        AssetDatabase.DeleteAsset(path);
        AnimatorController ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine sm = ctrl.layers[0].stateMachine;

        Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>();
        foreach (var (info, clip) in clips)
        {
            AnimatorState st = sm.AddState(info.name);
            st.motion = clip;
            states[info.name] = st;
        }

        AnimatorState idle = states.ContainsKey("Idle") ? states["Idle"] : states.Values.First();
        sm.defaultState = idle;

        foreach (var (info, clip) in clips)
        {
            AnimatorState st = states[info.name];
            if (st == idle) continue;

            if (info.loop)
            {
                ctrl.AddParameter(info.name, AnimatorControllerParameterType.Bool);

                AnimatorStateTransition toState = idle.AddTransition(st);
                toState.hasExitTime = false; toState.duration = 0f;
                toState.AddCondition(AnimatorConditionMode.If, 0, info.name);

                AnimatorStateTransition back = st.AddTransition(idle);
                back.hasExitTime = false; back.duration = 0f;
                back.AddCondition(AnimatorConditionMode.IfNot, 0, info.name);
            }
            else
            {
                ctrl.AddParameter(info.name, AnimatorControllerParameterType.Trigger);

                AnimatorStateTransition any = sm.AddAnyStateTransition(st);
                any.hasExitTime = false; any.duration = 0f; any.canTransitionToSelf = false;
                any.AddCondition(AnimatorConditionMode.If, 0, info.name);

                // Die y Explode se quedan en el ultimo cuadro; el resto vuelve a Idle
                bool terminal = info.name == "Die" || info.name == "Explode";
                if (!terminal)
                {
                    AnimatorStateTransition exit = st.AddTransition(idle);
                    exit.hasExitTime = true; exit.exitTime = 1f; exit.duration = 0f;
                }
            }
        }

        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    // ---------- 4. Crear prefab listo para usar ----------
    static void BuildPrefab(CharInfo ch, Dictionary<string, Sprite> sprites, AnimatorController ctrl)
    {
        string dir = Root + "/Prefabs";
        Directory.CreateDirectory(dir);
        AssetDatabase.Refresh();

        GameObject go = new GameObject(ch.name);
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        AnimInfo first = ch.anims[0];
        sr.sprite = sprites[FrameName(ch, first, 0)];
        sr.sortingOrder = 5;
        Animator an = go.AddComponent<Animator>();
        an.runtimeAnimatorController = ctrl;

        string p = dir + "/" + ch.name + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(go, p);
        UnityEngine.Object.DestroyImmediate(go);
    }
}
#endif
