using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Runtime planar mirror for URP. Renders the scene from a reflected camera    
/// into a RenderTexture and applies it to this object's material.
/// Attach to a Quad that faces into the room (the Quad's forward direction).   
/// Only active during Play mode.
/// </summary>
public class PlanarMirror : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Vertical resolution of the mirror render texture")]
    public int textureHeight = 1024;

    [Tooltip("Mirror panel width in world units (set by builder or manually)")] 
    public float mirrorWidth = 12f;

    [Tooltip("Mirror panel height in world units (set by builder or manually)")]
    public float mirrorHeight = 3.6f;

    [Tooltip("Max draw distance for the mirror camera")]
    public float farClipPlane = 30f;

    [Tooltip("Layers the mirror camera renders")]
    public LayerMask reflectionLayers = ~0;

    [Header("Performance")]
    [Tooltip("Skip rendering when player faces away from the mirror")]
    public bool enableVisibilityCulling = false;

    [Tooltip("Dot product threshold for visibility (-1 = always render, 0 = within 90 degrees)")]
    [Range(-1f, 0.5f)]
    public float cullingThreshold = -1.0f;

    private Camera _mirrorCamera;
    private RenderTexture _renderTexture;
    private Material _mirrorMaterial;
    private Material _originalMaterial;

    private float MirrorAspect => mirrorWidth / Mathf.Max(mirrorHeight, 0.01f); 

    void OnEnable()
    {
        if (!Application.isPlaying) return;

        // Save original material to restore on disable
        Renderer rend = GetComponent<Renderer>();
        if (rend != null) _originalMaterial = rend.sharedMaterial;

        SetupMirrorCamera();
        SetupRenderTexture();
        ApplyToMaterial();

        RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;

        Renderer rend = GetComponent<Renderer>();
        if (rend != null && _originalMaterial != null)
            rend.sharedMaterial = _originalMaterial;

        Cleanup();
    }

    void OnBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if (!Application.isPlaying) return;

        // Prevent recursive rendering in mirror camera
        if (camera.cameraType == CameraType.Reflection || camera == _mirrorCamera)
            return;

        Camera mainCam = Camera.main;
        if (camera != mainCam || mainCam == null || _mirrorCamera == null) return;

        Vector3 mirrorNormal = transform.forward;

        if (enableVisibilityCulling)
        {
            Vector3 toMirror = (transform.position - mainCam.transform.position).normalized;
            float dot = Vector3.Dot(toMirror, mirrorNormal);

            if (dot > cullingThreshold)
                return;
        }

        RenderReflection(context, mainCam, mirrorNormal);
    }

    private void RenderReflection(ScriptableRenderContext context, Camera mainCam, Vector3 mirrorNormal)
    {
        Vector3 mirrorPos = transform.position;

        float d = -Vector3.Dot(mirrorNormal, mirrorPos);
        Vector4 reflectionPlane = new Vector4(mirrorNormal.x, mirrorNormal.y, mirrorNormal.z, d);
        
        Matrix4x4 reflectionMatrix = CalculateReflectionMatrix(reflectionPlane);

        _mirrorCamera.worldToCameraMatrix = mainCam.worldToCameraMatrix * reflectionMatrix;
        
        Vector4 clipPlane = CameraSpacePlane(_mirrorCamera, mirrorPos, mirrorNormal, 1.0f);
        _mirrorCamera.projectionMatrix = mainCam.CalculateObliqueMatrix(clipPlane);
        
        _mirrorCamera.fieldOfView = mainCam.fieldOfView;
        _mirrorCamera.aspect = MirrorAspect;
        _mirrorCamera.nearClipPlane = mainCam.nearClipPlane;
        _mirrorCamera.farClipPlane = farClipPlane;
        _mirrorCamera.clearFlags = mainCam.clearFlags;
        _mirrorCamera.backgroundColor = mainCam.backgroundColor;

        GL.invertCulling = true;
        
#pragma warning disable CS0618
        UniversalRenderPipeline.RenderSingleCamera(context, _mirrorCamera);
#pragma warning restore CS0618
        
        GL.invertCulling = false;
    }

    private void SetupMirrorCamera()
    {
        GameObject camGO = new GameObject("MirrorCamera");
        camGO.transform.SetParent(transform, false);
        camGO.hideFlags = HideFlags.HideAndDontSave;
        _mirrorCamera = camGO.AddComponent<Camera>();

        _mirrorCamera.enabled = false;
        _mirrorCamera.cameraType = CameraType.Reflection;
        _mirrorCamera.farClipPlane = farClipPlane;
        _mirrorCamera.cullingMask = reflectionLayers;

        // URP requires UniversalAdditionalCameraData on every camera.
        // Without it, RenderSingleCamera silently renders nothing.
        var urpData = camGO.AddComponent<UniversalAdditionalCameraData>();
        urpData.renderShadows = false;
        urpData.requiresColorOption = CameraOverrideOption.Off;
        urpData.requiresDepthOption = CameraOverrideOption.Off;

        // Copy renderer index from main camera if available
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            var mainUrpData = mainCam.GetComponent<UniversalAdditionalCameraData>();
            if (mainUrpData != null && mainUrpData.scriptableRenderer != null)
                urpData.SetRenderer(0); // Use same renderer as project default
        }
    }

    private void SetupRenderTexture()
    {
        int rtHeight = textureHeight;
        int rtWidth = Mathf.RoundToInt(rtHeight * MirrorAspect);
        rtWidth = Mathf.Max(rtWidth, 64);

        _renderTexture = new RenderTexture(rtWidth, rtHeight, 16);
        _renderTexture.name = "MirrorRT";
        _renderTexture.filterMode = FilterMode.Bilinear;
        _renderTexture.Create();

        _mirrorCamera.targetTexture = _renderTexture;
    }

    private void ApplyToMaterial()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend == null) return;

        _mirrorMaterial = new Material(rend.sharedMaterial);
        _mirrorMaterial.name = "Gym_Mirror_Reflective";
        rend.material = _mirrorMaterial;

        if (_mirrorMaterial.HasProperty("_BaseMap"))
            _mirrorMaterial.SetTexture("_BaseMap", _renderTexture);
        if (_mirrorMaterial.HasProperty("_MainTex"))
            _mirrorMaterial.SetTexture("_MainTex", _renderTexture);

        if (_mirrorMaterial.HasProperty("_Metallic"))
            _mirrorMaterial.SetFloat("_Metallic", 0.0f);
        if (_mirrorMaterial.HasProperty("_Smoothness"))
            _mirrorMaterial.SetFloat("_Smoothness", 0.0f);
    }

    private static Matrix4x4 CalculateReflectionMatrix(Vector4 plane)
    {
        Matrix4x4 m = Matrix4x4.identity;
        m.m00 = 1f - 2f * plane.x * plane.x;
        m.m01 = -2f * plane.x * plane.y;
        m.m02 = -2f * plane.x * plane.z;
        m.m03 = -2f * plane.x * plane.w;

        m.m10 = -2f * plane.y * plane.x;
        m.m11 = 1f - 2f * plane.y * plane.y;
        m.m12 = -2f * plane.y * plane.z;
        m.m13 = -2f * plane.y * plane.w;

        m.m20 = -2f * plane.z * plane.x;
        m.m21 = -2f * plane.z * plane.y;
        m.m22 = 1f - 2f * plane.z * plane.z;
        m.m23 = -2f * plane.z * plane.w;

        m.m30 = 0;
        m.m31 = 0;
        m.m32 = 0;
        m.m33 = 1;

        return m;
    }

    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos + normal * 0.07f;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;       
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));                                                                           
    }

    private void Cleanup()
    {
        if (_renderTexture != null)
        {
            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }

        if (_mirrorMaterial != null)
        {
            Destroy(_mirrorMaterial);
            _mirrorMaterial = null;
        }

        if (_mirrorCamera != null)
        {
            Destroy(_mirrorCamera.gameObject);
            _mirrorCamera = null;
        }
    }
}
