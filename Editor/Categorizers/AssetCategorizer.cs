using System.Collections.Generic;
using UnityEditor;

namespace BuildMetrics.Editor
{
    internal static class AssetCategorizer
    {
        internal static string CategorizeAsset(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "other";

            var lowerPath = path.ToLowerInvariant();

            if (lowerPath.EndsWith(".cs") || lowerPath.EndsWith(".js") || lowerPath.EndsWith(".boo") ||
                lowerPath.Contains("/scripts/") || lowerPath.Contains("\\scripts\\"))
                return "scripts";

            if (lowerPath.Contains("/resources/") || lowerPath.Contains("\\resources\\"))
                return "resources";

            if (lowerPath.Contains("/streamingassets/") || lowerPath.Contains("\\streamingassets\\"))
                return "streamingAssets";

            if (lowerPath.Contains("/plugins/") || lowerPath.Contains("\\plugins\\") ||
                lowerPath.EndsWith(".dll") || lowerPath.EndsWith(".so") || lowerPath.EndsWith(".bundle"))
                return "plugins";

            if (lowerPath.EndsWith(".unity"))
                return "scenes";

            if (lowerPath.EndsWith(".shader") || lowerPath.EndsWith(".cginc") || lowerPath.EndsWith(".shadergraph") ||
                lowerPath.Contains("/shaders/") || lowerPath.Contains("\\shaders\\"))
                return "shaders";

            return "other";
        }

        internal static string CategorizeOtherFile(string path, BuildTarget platform)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "other";

            var lowerPath = path.ToLowerInvariant();

            if (platform == BuildTarget.iOS)
            {
                if (lowerPath.Contains("assets.car") || lowerPath.Contains("/assets.car"))
                    return "iosAssetCatalogs";

                if (lowerPath.EndsWith(".storyboardc") || lowerPath.EndsWith(".nib") ||
                    lowerPath.EndsWith(".storyboard") || lowerPath.Contains("/base.lproj/") ||
                    (lowerPath.Contains(".app/") && (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg"))))
                    return "iosAppResources";

                if (lowerPath.Contains("/frameworks/") || lowerPath.Contains(".framework/") ||
                    lowerPath.Contains("/swiftsupport/") || lowerPath.Contains("/plugins/") ||
                    lowerPath.Contains("_codesignature/") || lowerPath.Contains("/meta-inf/") ||
                    lowerPath.EndsWith(".dylib") || lowerPath.Contains("/extensions/"))
                    return "iosSystem";
            }
            else if (platform == BuildTarget.Android)
            {
                if (lowerPath.Contains("/assets/aa/") || lowerPath.Contains("\\assets\\aa\\") ||
                    (lowerPath.Contains("/assets/") && lowerPath.EndsWith(".bundle")))
                    return "androidAddressables";

                if (lowerPath.Contains("/assets/bin/data/") || lowerPath.Contains("\\assets\\bin\\data\\") ||
                    lowerPath.Contains("sharedassets") || lowerPath.EndsWith(".resS"))
                    return "androidUnityData";

                if (lowerPath.Contains("/res/") || lowerPath.Contains("\\res\\") ||
                    lowerPath.Contains("resources.arsc"))
                    return "androidResources";

                if (lowerPath.Contains("classes") && lowerPath.EndsWith(".dex"))
                    return "androidCode";

                if (lowerPath.Contains("/lib/") || lowerPath.Contains("/jnilibs/") ||
                    lowerPath.Contains("\\lib\\") || lowerPath.Contains("\\jnilibs\\"))
                    return "androidSystem";
            }
            else if (platform == BuildTarget.WebGL)
            {
                if (lowerPath.EndsWith(".data"))   return "webglData";
                if (lowerPath.EndsWith(".wasm"))   return "webglWasm";
                if (lowerPath.EndsWith(".js"))     return "webglJs";
            }

            if (lowerPath.EndsWith(".spriteatlas") || lowerPath.EndsWith(".spriteatlasv2"))
                return "spriteAtlases";

            if (lowerPath.EndsWith(".pvrtc") || lowerPath.EndsWith(".etc") || lowerPath.EndsWith(".etc2") ||
                lowerPath.EndsWith(".astc") || lowerPath.EndsWith(".dds") || lowerPath.EndsWith(".ktx") ||
                lowerPath.Contains("texture") || lowerPath.Contains(".png") || lowerPath.Contains(".jpg"))
                return "textures";

            if (lowerPath.Contains("mesh") || lowerPath.EndsWith(".mesh"))
                return "meshes";

            if (lowerPath.EndsWith(".mp3") || lowerPath.EndsWith(".ogg") || lowerPath.EndsWith(".wav") ||
                lowerPath.EndsWith(".m4a") || lowerPath.EndsWith(".aac"))
                return "audio";

            if (lowerPath.EndsWith(".bundle") || lowerPath.Contains("assetbundle") ||
                lowerPath.Contains("/aa/") || lowerPath.Contains("\\aa\\"))
                return "assetBundles";

            if (lowerPath.Contains("sharedassets") || lowerPath.Contains("globalgamemanagers") ||
                lowerPath.Contains("level") || lowerPath.EndsWith(".resource") ||
                lowerPath.EndsWith(".assets") || lowerPath.EndsWith(".resS"))
                return "unityRuntime";

            if (lowerPath.EndsWith(".ttf") || lowerPath.EndsWith(".otf") || lowerPath.Contains("font"))
                return "fonts";

            return "other";
        }

        internal static string CategorizeAssetByType(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "otherAssets";

            var lowerPath = path.ToLowerInvariant();

            if (lowerPath.EndsWith(".spriteatlas") || lowerPath.EndsWith(".spriteatlasv2"))
                return "spriteAtlases";

            if (lowerPath.EndsWith(".png") || lowerPath.EndsWith(".jpg") || lowerPath.EndsWith(".jpeg") ||
                lowerPath.EndsWith(".tga") || lowerPath.EndsWith(".psd") || lowerPath.EndsWith(".tif") ||
                lowerPath.EndsWith(".tiff") || lowerPath.EndsWith(".gif") || lowerPath.EndsWith(".bmp") ||
                lowerPath.EndsWith(".exr") || lowerPath.EndsWith(".hdr"))
                return "textures";

            if (lowerPath.EndsWith(".mp3") || lowerPath.EndsWith(".wav") || lowerPath.EndsWith(".ogg") ||
                lowerPath.EndsWith(".aiff") || lowerPath.EndsWith(".aif") || lowerPath.EndsWith(".mod") ||
                lowerPath.EndsWith(".it") || lowerPath.EndsWith(".s3m") || lowerPath.EndsWith(".xm"))
                return "audio";

            if (lowerPath.EndsWith(".fbx") || lowerPath.EndsWith(".dae") || lowerPath.EndsWith(".3ds") ||
                lowerPath.EndsWith(".dxf") || lowerPath.EndsWith(".obj") || lowerPath.EndsWith(".skp") ||
                lowerPath.EndsWith(".blend") || lowerPath.EndsWith(".mb") || lowerPath.EndsWith(".ma"))
                return "models";

            if (lowerPath.EndsWith(".anim") || lowerPath.EndsWith(".controller") ||
                lowerPath.EndsWith(".overridecontroller"))
                return "animations";

            if (lowerPath.EndsWith(".prefab"))   return "prefabs";
            if (lowerPath.EndsWith(".unity"))    return "scenes";

            if (lowerPath.EndsWith(".cs") || lowerPath.EndsWith(".js") || lowerPath.EndsWith(".boo"))
                return "scripts";

            if (lowerPath.EndsWith(".shader") || lowerPath.EndsWith(".cginc") || lowerPath.EndsWith(".hlsl") ||
                lowerPath.EndsWith(".compute") || lowerPath.EndsWith(".shadergraph") || lowerPath.EndsWith(".shadersubgraph"))
                return "shaders";

            if (lowerPath.EndsWith(".mat"))      return "materials";

            if (lowerPath.EndsWith(".ttf") || lowerPath.EndsWith(".otf") ||
                (lowerPath.EndsWith(".asset") && lowerPath.Contains("textmesh")))
                return "fonts";

            if (lowerPath.EndsWith(".mp4") || lowerPath.EndsWith(".mov") || lowerPath.EndsWith(".avi") ||
                lowerPath.EndsWith(".webm") || lowerPath.EndsWith(".ogv"))
                return "videos";

            return "otherAssets";
        }
    }
}
