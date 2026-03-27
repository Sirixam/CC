using UnityEngine;

[CreateAssetMenu(fileName = "CharacterPortraitConfig", menuName = "Game Tools/Character Portrait Config")]
public class CharacterPortraitConfig : ScriptableObject
{
    [Header("Folders")]
    public string SourceFolder = "Assets/03_Prefabs/NPCs";
    public bool IncludeNestedFolders = true;
    public string OutputFolder = "Assets/PortraitOutput";

    [Header("Camera")]
    public int ResolutionWidth = 512;
    public int ResolutionHeight = 512;
    public float CameraDistance = 2f;
    public float CameraHeight = 1.1f;
    public float FieldOfView = 35f;

    [Header("Character")]
    public float CharacterRotationY = 0f;
    public Vector3 CharacterOffset = Vector3.zero;
    public Vector3 CharacterOffsetFemale = Vector3.zero;

    [Header("Lighting")]
    public float LightIntensity = 1.2f;
    public Vector3 LightRotation = new Vector3(50, -30, 0f);
}
