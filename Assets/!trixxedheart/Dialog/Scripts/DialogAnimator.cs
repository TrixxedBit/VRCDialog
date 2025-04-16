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
    public AudioSource typingSound;
    public int soundEveryChars = 2;

    private string fullText;
    private float openProgress;
    private int charIndex;
    private bool isOpening;
    private bool isTyping;

    public void Show(string text) // show the stuff
    {
        fullText = text;
        charIndex = 0;
        textMesh.text = "";
        openProgress = 0f;
        isOpening = true;
        isTyping = false;
        panel.transform.localScale = Vector3.zero;
        SetTextAlpha(0f);

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

        SetTextAlpha(t); // i dont think this even works rn

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

        // if the typing effect encounters an html tag like <bold> it'll pause the typing effect and skip to find the ending > so it doesn't show up during the typewriter effect.
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

        // Same as < but with [ for dialog specific effects like [br] (will prob change this in the future)
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

        // if its anything else
        textMesh.text += currentChar;

        if (enableTypingSound && typingSound != null && charIndex % soundEveryChars == 0)
        {
            typingSound.Stop();
            typingSound.Play();
        }

        charIndex++;

        if (enableTypingEffect)
            SendCustomEventDelayedSeconds(nameof(TypeText), textSpeed);
        else
            SendCustomEventDelayedFrames(nameof(TypeText), 1); // Fast-forward
    }

    private void SetTextAlpha(float alpha) // do not know if this even works
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

    // options
    public void EnableTypingEffect(bool enable)
    {
        enableTypingEffect = enable;
    }

    public void EnableTypingSound(bool enable)
    {
        enableTypingSound = enable;
    }
}
