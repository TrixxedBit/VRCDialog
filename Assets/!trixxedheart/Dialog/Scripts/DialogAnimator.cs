using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VRC.SDKBase;
using VRC.Udon;

public class DialogAnimator : UdonSharpBehaviour
{
    [Header("UI (Required)")]
    public GameObject panel;
    public TextMeshProUGUI textMesh;

    [Header("Animation Settings")]
    public float openDuration = 0.3f;
    public float textSpeed = 0.02f;
    public float updateInterval = 0.02f;
    public float waitAfterBR = 0.5f;
    public bool enableTypingEffect = true;

    [Header("Sounds")]
    public bool enableTypingSound = true;
    public AudioSource openSound;
    public AudioSource typingAudioSource;
    public AudioClip[] typingClips;
    public int soundEveryChars = 2;
    public float pitchVariation = 0.05f;

    private float baseTypingPitch = 1f;

    private string fullText;
    private float openProgress;
    private int charIndex;
    private bool isOpening;
    private bool isTyping;

    public void Show(string text)
    {
        fullText = text;
        charIndex = 0;
        textMesh.text = "";
        openProgress = 0f;
        isOpening = true;
        isTyping = false;
        panel.transform.localScale = new Vector3(0f, 1f, 1f);
        SetTextAlpha(0f);

        if (typingAudioSource != null)
            baseTypingPitch = typingAudioSource.pitch;

        if (openSound != null)
            openSound.Play();

        SendCustomEventDelayedSeconds(nameof(AnimateOpen), updateInterval);
    }

    public void AnimateOpen()
    {
        if (!isOpening) return;

        openProgress += updateInterval;
        float t = Mathf.Clamp01(openProgress / openDuration);
        Vector3 scale = panel.transform.localScale;
        scale.x = Mathf.Lerp(0f, 1f, t);
        scale.y = 1f;
        scale.z = 1f;
        panel.transform.localScale = scale;

        SetTextAlpha(t);

        if (t < 1f)
        {
            SendCustomEventDelayedSeconds(nameof(AnimateOpen), updateInterval);
        }
        else
        {
            isOpening = false;
            isTyping = true;
            SendCustomEventDelayedSeconds(nameof(TypeText), textSpeed);
        }
    }

    public void TypeText()
    {
        if (!isTyping) return;

        if (charIndex >= fullText.Length)
        {
            isTyping = false;
            return;
        }

        char currentChar = fullText[charIndex];

        // Handle TMP tags like <b>...</b>
        if (currentChar == '<')
        {
            int closingIndex = fullText.IndexOf('>', charIndex);
            if (closingIndex != -1)
            {
                string tag = fullText.Substring(charIndex, closingIndex - charIndex + 1);
                textMesh.text += tag;
                charIndex = closingIndex + 1;
                SendCustomEventDelayedSeconds(nameof(TypeText), textSpeed);
                return;
            }
        }

        // Handle custom tag [br] for pause
        if (currentChar == '[')
        {
            int closingIndex = fullText.IndexOf(']', charIndex);
            if (closingIndex != -1)
            {
                string tag = fullText.Substring(charIndex, closingIndex - charIndex + 1);
                if (tag == "[br]")
                {
                    charIndex = closingIndex + 1;
                    SendCustomEventDelayedSeconds(nameof(TypeText), waitAfterBR);
                    return;
                }
            }
        }

        // Add character
        textMesh.text += currentChar;

        // Play typing sound
        if (enableTypingSound && typingAudioSource != null && typingClips != null && typingClips.Length > 0 && charIndex % soundEveryChars == 0)
        {
            AudioClip clip = typingClips[Random.Range(0, typingClips.Length)];
            typingAudioSource.pitch = baseTypingPitch + Random.Range(-pitchVariation, pitchVariation);
            typingAudioSource.Stop(); // Optional: avoids sound overlap
            typingAudioSource.PlayOneShot(clip);
        }

        charIndex++;

        if (enableTypingEffect)
            SendCustomEventDelayedSeconds(nameof(TypeText), textSpeed);
        else
            SendCustomEventDelayedFrames(nameof(TypeText), 1); // Instant mode
    }

    private void SetTextAlpha(float alpha)
    {
        Color c = textMesh.color;
        c.a = alpha;
        textMesh.color = c;
    }

    public void CloseDialog()
    {
        isTyping = false;
        panel.transform.localScale = Vector3.zero;
        textMesh.text = "";
    }

    public void EnableTypingEffect(bool enable)
    {
        enableTypingEffect = enable;
    }

    public void EnableTypingSound(bool enable)
    {
        enableTypingSound = enable;
    }
}
