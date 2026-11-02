
using System;
using System.IO;
using System.Collections;
using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
public class ScreenshotCapture : MonoBehaviour
{
    [Header("Screenshot Settings")]
    [SerializeField] private int width = 1920;
    [SerializeField] private int height = 1080;

    [Header("Hotkey")]
    [SerializeField] private KeyCode captureKey = KeyCode.F9;

    [Header("Player")]
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 teleportPosition = new Vector2(5f, -70f);

    [Header("File Settings")]
    [SerializeField] private string folderName = "Screenshots";
    [SerializeField] private string filePrefix = "Venik2D";

    private bool isCapturing;

    private void Update()
    {
        bool pressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null &&
            Keyboard.current.f9Key.wasPressedThisFrame)
            pressed = true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(captureKey))
            pressed = true;
#endif

        if (pressed)
            CaptureScreenshot();
    }

    public void CaptureScreenshot()
    {
        if (!Application.isPlaying || isCapturing)
            return;

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("ScreenshotCapture: Invalid resolution.");
            return;
        }

        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
                player = playerObject.transform;
        }

        if (player == null)
        {
            Debug.LogError("ScreenshotCapture: Player not found.");
            return;
        }

        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine()
    {
        isCapturing = true;

        SpriteRenderer[] renderers =
            player.GetComponentsInChildren<SpriteRenderer>(true);

        bool[] previousStates = new bool[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            previousStates[i] = renderers[i].enabled;

        // Teleport player.
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.position = teleportPosition;
            rb.velocity = Vector2.zero;
        }
        else
        {
            player.position = new Vector3(
                teleportPosition.x,
                teleportPosition.y,
                player.position.z
            );
        }

        Physics2D.SyncTransforms();

        // Hide all player sprites.
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].enabled = false;
        }

        // Wait for Cinemachine and rendering.
        yield return null;
        yield return new WaitForEndOfFrame();

        string error = null;

        Texture2D source = null;
        Texture2D cropped = null;
        Texture2D result = null;
        RenderTexture rt = null;

        RenderTexture previousActive = RenderTexture.active;

        try
        {
            source = ScreenCapture.CaptureScreenshotAsTexture();

            if (source == null)
                throw new Exception("Failed to capture screen.");

            float targetAspect = (float)width / height;
            float sourceAspect = (float)source.width / source.height;

            int cropWidth = source.width;
            int cropHeight = source.height;

            if (sourceAspect > targetAspect)
                cropWidth = Mathf.RoundToInt(source.height * targetAspect);
            else if (sourceAspect < targetAspect)
                cropHeight = Mathf.RoundToInt(source.width / targetAspect);

            cropWidth = Mathf.Clamp(cropWidth, 1, source.width);
            cropHeight = Mathf.Clamp(cropHeight, 1, source.height);

            int offsetX = (source.width - cropWidth) / 2;
            int offsetY = (source.height - cropHeight) / 2;

            cropped = new Texture2D(
                cropWidth,
                cropHeight,
                TextureFormat.RGB24,
                false
            );

            cropped.SetPixels(source.GetPixels(
                offsetX,
                offsetY,
                cropWidth,
                cropHeight
            ));

            cropped.Apply(false, false);

            rt = RenderTexture.GetTemporary(
                width,
                height,
                0,
                RenderTextureFormat.ARGB32
            );

            rt.filterMode = FilterMode.Bilinear;

            Graphics.Blit(cropped, rt);
            RenderTexture.active = rt;

            result = new Texture2D(
                width,
                height,
                TextureFormat.RGB24,
                false
            );

            result.ReadPixels(
                new Rect(0, 0, width, height),
                0,
                0
            );

            result.Apply(false, false);

            string projectFolder =
                Directory.GetParent(Application.dataPath).FullName;

            string outputFolder = Path.Combine(
                projectFolder,
                folderName
            );

            Directory.CreateDirectory(outputFolder);

            string timestamp = DateTime.Now.ToString(
                "yyyy-MM-dd_HH-mm-ss"
            );

            string path = Path.Combine(
                outputFolder,
                $"{filePrefix}_{timestamp}.png"
            );

            File.WriteAllBytes(path, result.EncodeToPNG());

            Debug.Log(
                $"Screenshot saved: {path} ({width}x{height})"
            );
        }
        catch (Exception exception)
        {
            error = exception.ToString();
        }
        finally
        {
            RenderTexture.active = previousActive;

            if (rt != null)
                RenderTexture.ReleaseTemporary(rt);

            if (source != null) Destroy(source);
            if (cropped != null) Destroy(cropped);
            if (result != null) Destroy(result);

            // Restore original player visibility.
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].enabled = previousStates[i];
            }

            isCapturing = false;
        }

        if (error != null)
            Debug.LogError("ScreenshotCapture: " + error);
    }
}
