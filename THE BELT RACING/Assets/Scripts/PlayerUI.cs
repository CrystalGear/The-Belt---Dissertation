
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private Slider speedSlider;
    [SerializeField] private TextMeshProUGUI speedText;

    [Header("Boost")]
    [SerializeField] private Image energyRadial;
    [SerializeField] private TextMeshProUGUI boostText;

    [Header("Boost Colours")]
    [SerializeField] private Color boostReadyColour = Color.cyan;
    [SerializeField] private Color boostOverheatedColour = Color.red;

    [Header("Boost Ready Flash")]
    [SerializeField] private float flashDuration = 1f;
    [SerializeField] private float flashSpeed = 5f;
    [SerializeField] private float readyTextDuration = 1.5f;

    [Header("References")]
    [SerializeField] private PlayerMovement playerMovement;

    private Coroutine flashCoroutine;
    private Coroutine readyTextCoroutine;

    private float currentBoostEnergy;

    private bool showingReadyText;
    private bool isFlashing;

    private void OnEnable()
    {
        if (playerMovement == null)
            return;

        playerMovement.OnSpeedChanged += UpdateSpeed;
        playerMovement.OnEnergyChanged += UpdateBoost;
        playerMovement.OnBoostReady += BoostReady;

        currentBoostEnergy = playerMovement.CurrentBoostEnergy;

        UpdateSpeed(0);
        UpdateBoost(currentBoostEnergy);
    }

    private void OnDisable()
    {
        if (playerMovement != null)
        {
            playerMovement.OnSpeedChanged -= UpdateSpeed;
            playerMovement.OnEnergyChanged -= UpdateBoost;
            playerMovement.OnBoostReady -= BoostReady;
        }

        StopAllCoroutines();

        showingReadyText = false;
        isFlashing = false;
    }

    #region Speed UI

    private void UpdateSpeed(int speed)
    {
        if (speedSlider != null)
            speedSlider.value = speed;

        if (speedText != null)
            speedText.text = speed.ToString();
    }

    #endregion

    #region Boost UI

    private void UpdateBoost(float energy)
    {
        if (playerMovement == null)
            return;

        currentBoostEnergy = energy;

        // Update radial fill.
        if (energyRadial != null)
        {
            float fillAmount = playerMovement.MaxBoostEnergy > 0f
                ? energy / playerMovement.MaxBoostEnergy
                : 0f;

            energyRadial.fillAmount = Mathf.Clamp01(fillAmount);
        }

        // Only update the text if READY is not currently being displayed.
        if (boostText != null && !showingReadyText)
        {
            if (playerMovement.EnginesOverheated)
                boostText.text = "COOLING";
            else
                boostText.text = Mathf.RoundToInt(currentBoostEnergy).ToString();
        }

        UpdateBoostColour();
    }

    private void UpdateBoostColour()
    {
        if (energyRadial == null || playerMovement == null)
            return;

        // Don't interrupt the flashing animation.
        if (isFlashing)
            return;

        energyRadial.color = playerMovement.EnginesOverheated
            ? boostOverheatedColour
            : boostReadyColour;
    }

    #endregion

    #region Boost Ready Effects

    private void BoostReady()
    {
        // Stop any existing effects.
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        if (readyTextCoroutine != null)
            StopCoroutine(readyTextCoroutine);

        // Start the ready effects.
        flashCoroutine = StartCoroutine(FlashBoostRadial());
        readyTextCoroutine = StartCoroutine(ShowReadyText());
    }

    private IEnumerator FlashBoostRadial()
    {
        isFlashing = true;

        float timer = 0f;

        while (timer < flashDuration)
        {
            timer += Time.deltaTime;

            float flash = Mathf.PingPong(timer * flashSpeed, 1f);

            if (energyRadial != null)
            {
                energyRadial.color = Color.Lerp(
                    boostReadyColour,
                    Color.white,
                    flash
                );
            }

            yield return null;
        }

        isFlashing = false;

        // Restore the correct colour.
        UpdateBoostColour();

        flashCoroutine = null;
    }

    private IEnumerator ShowReadyText()
    {
        // Prevent energy updates from overwriting READY.
        showingReadyText = true;

        if (boostText != null)
            boostText.text = "READY";

        yield return new WaitForSeconds(readyTextDuration);

        // Allow normal energy updates again.
        showingReadyText = false;

        if (boostText != null)
            boostText.text = Mathf.RoundToInt(currentBoostEnergy).ToString();

        readyTextCoroutine = null;
    }

    #endregion
}
