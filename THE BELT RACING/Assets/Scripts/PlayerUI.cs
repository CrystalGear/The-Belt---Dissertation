using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] Image energyRadial;
    [SerializeField] TextMeshProUGUI speed;
    PlayerMovement playerMovement;

    private void Awake()
    {
        playerMovement = FindAnyObjectByType<PlayerMovement>();
    }
    private void OnEnable()
    {
        playerMovement.OnSpeedChanged  += UpdatePlayerSpeed;
        playerMovement.OnEnergyChanged += UpdateEnergyRadial;
    }

    private void OnDisable()
    {
        playerMovement.OnSpeedChanged  -= UpdatePlayerSpeed;
        playerMovement.OnEnergyChanged -= UpdateEnergyRadial;
    }
    private void UpdatePlayerSpeed(int value)
    {
        speed.text = value.ToString();
    }

    private void UpdateEnergyRadial(float value)
    {
        energyRadial.fillAmount = value;
    }
}
