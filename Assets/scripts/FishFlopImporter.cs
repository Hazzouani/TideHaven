using UnityEditor;

// Sets the right import settings for the fish FBX files automatically:
// Generic rig and looping clips. Works for any FBX inside a folder called "FishFlop".
public class FishFlopImporter : AssetPostprocessor
{
    bool IsFishFile => assetPath.Contains("FishFlop") && assetPath.ToLower().EndsWith(".fbx");

    void OnPreprocessModel()
    {
        if (!IsFishFile) return;
        var importer = (ModelImporter)assetImporter;
        importer.animationType = ModelImporterAnimationType.Generic;
        importer.importAnimation = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
    }

    void OnPreprocessAnimation()
    {
        if (!IsFishFile) return;
        var importer = (ModelImporter)assetImporter;
        var clips = importer.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.loopTime = true;
            clip.loopPose = false;
            clip.name = clip.takeName; // "Flop_Floor" / "Flop_Rod"
        }
        importer.clipAnimations = clips;
    }
}
