#nullable enable

using System.Linq;
using System;

using UnityEngine;
using UnityEditor;
//using Unity.VisualScripting;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

#if UNITY_EDITOR

public class DisplayHierarchyIconAttribute : Attribute
{
    public bool ShowIconInHierarchy { get; set; } = true;
}

public static class HierarchyIconDrawer
{
    private const int IconSqSizeBase = 16;

    private static Color DisabledOverrideColor => new(1, 1, 1, DisabledOpacity);


    public static float DisabledOpacity { get; set; } = 0.15f;
        

    [InitializeOnLoadMethod]
    private static void Initialize() {
        EditorApplication.hierarchyWindowItemOnGUI += OnGUI;
    }

    private static bool HasAttribute<TAttribute>(this Type type, [NotNullWhen(true)] out TAttribute? attribute) where TAttribute : Attribute {
        attribute = type.GetCustomAttribute<TAttribute>();
        return attribute != null;
    }

    private static void OnGUI(int instanceID, Rect selectionRect) {

        var obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if(obj is null) {
            return;
        }

        var components = obj.GetComponents<Component>().Where(x => x is not Transform && x is not Renderer)
                                                       .Where(x => {
                                                           if(x is MonoBehaviour) {
                                                               if(x.GetType().HasAttribute<DisplayHierarchyIconAttribute>(out var att)) {
                                                                   return att.ShowIconInHierarchy;
                                                               }
                                                               else if(x.GetType().HasAttribute<HideInInspector>(out var hideInInspectorAttribute)){
                                                                   return false;
                                                               }
                                                           }
                                                           return true;
                                                       })
                                                       .ToArray();



#if false

        if(components.Any(x => x is Renderer)) {
            components = components.Where(x => x is not Transform && x is not Renderer).ToArray();
        }
        else {
            components = components.Where(x => x is not MeshFilter).ToArray();
        }

#endif

        if(components.Length == 0) return;

        selectionRect.x = selectionRect.xMax - (IconSqSizeBase * components.Length);
        selectionRect.width = IconSqSizeBase;

        foreach(var component in components) {

            float applyingSq = IconSqSizeBase;

            bool isEnabled = true;

            var texture2D = AssetPreview.GetMiniThumbnail(component);
            if(component is MonoBehaviour monoBehaviour) {
                if(!monoBehaviour.enabled) {
                    //SetAlpha(texture2D);
                    //applyingSq = IconSqSizeBase / 2;
                    isEnabled = false;
                }
            }
            if(texture2D != null) {
                if(isEnabled) {
                    GUI.DrawTexture(selectionRect, texture2D);
                }
                else {
                    GUI.DrawTexture(selectionRect, texture2D,
                scaleMode: ScaleMode.ScaleToFit,
                alphaBlend: true,
                imageAspect: 1,
                color: DisabledOverrideColor,
                borderWidth: selectionRect.width,
                borderRadius: selectionRect.width);
                }
            }
            else {

            }
            selectionRect.x += applyingSq;
        }
    }


    private static Texture2D ToReadable(this Texture2D texture) {
        RenderTexture tmp = RenderTexture.GetTemporary(texture.width, texture.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
        Graphics.Blit(texture, tmp);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = tmp;

        Texture2D readableTexture = new Texture2D(texture.width, texture.height);
        readableTexture.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
        readableTexture.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(tmp);

        return readableTexture;
    }

    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="texture"></param>
    /// <remarks>
    /// Occurs the texture data is either not readable error.
    /// </remarks>
    private static void SetAlpha(Texture2D texture) {
        texture = texture.ToReadable();
        Color[] pixels = texture.GetPixels();

        for(int i = 0; i < pixels.Length; i++) {
            if(pixels[i].a > 0f)
            {
                Color c= pixels[i];
                pixels[i] = new Color(c.r, c.g, c.b, 0.1f);
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();
    }
}

#endif
