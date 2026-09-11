// ============================================================
// CalibrationUI.cs — REHABIT-EL
// Kalibrasyon ekranı UI: Min/Max butonları, progress bar, talimatlar.
// ============================================================
using UnityEngine;
using UnityEngine.UI;
using RehabitEL.Calibration;
using RehabitEL.Core;

namespace RehabitEL.UI
{
    /// <summary>
    /// Kalibrasyon sahnesindeki UI yönetimi.
    /// Kullanıcıya min/max değer kaydetme adımlarını gösterir.
    /// </summary>
    public class CalibrationUI : MonoBehaviour
    {
        // ── Inspector Ayarları ──
        [Header("UI Referansları")]
        [SerializeField] private Text titleText;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text statusText;
        [SerializeField] private Text rawValueText;
        [SerializeField] private Image progressFill;

        [Header("Butonlar")]
        [SerializeField] private Button minButton;
        [SerializeField] private Button maxButton;
        [SerializeField] private Button resetButton;
        [SerializeField] private Button doneButton;
        [SerializeField] private Button backButton;

        // ── Talimat Metinleri ──
        private const string INSTRUCTION_IDLE = "Kalibrasyona başlamak için\naşağıdaki butonları kullanın.";
        private const string INSTRUCTION_MIN = "Parmağınızı DÜZ tutun...\nÖrnek alınıyor...";
        private const string INSTRUCTION_MAX = "Parmağınızı MAKSİMUM bükün...\nÖrnek alınıyor...";
        private const string INSTRUCTION_DONE = "Kalibrasyon tamamlandı!\nOyuna başlayabilirsiniz.";

        // ════════════════════════════════════════════

        private void Start()
        {
            SetupButtons();
            UpdateUI(CalibrationManager.CalibrationState.Idle);
        }

        private void OnEnable()
        {
            if (CalibrationManager.Instance != null)
            {
                CalibrationManager.Instance.OnStateChanged += UpdateUI;
                CalibrationManager.Instance.OnProgressUpdated += UpdateProgress;
            }
        }

        private void OnDisable()
        {
            if (CalibrationManager.Instance != null)
            {
                CalibrationManager.Instance.OnStateChanged -= UpdateUI;
                CalibrationManager.Instance.OnProgressUpdated -= UpdateProgress;
            }
        }

        private void Update()
        {
            // Ham değer gösterimi
            if (rawValueText != null && RehabitEL.Input.InputManager.Instance != null)
            {
                float raw = RehabitEL.Input.InputManager.Instance.GetRawFlexValue();
                float filtered = RehabitEL.Input.InputManager.Instance.GetFilteredFlexValue();
                rawValueText.text = $"Ham: {raw:F3} | Filtreli: {filtered:F3}";
            }
        }

        private void SetupButtons()
        {
            if (minButton != null)
            {
                minButton.onClick.RemoveAllListeners();
                minButton.onClick.AddListener(() => {
                    if (CalibrationManager.Instance != null)
                        CalibrationManager.Instance.StartMinCalibration();
                });
            }

            if (maxButton != null)
            {
                maxButton.onClick.RemoveAllListeners();
                maxButton.onClick.AddListener(() => {
                    if (CalibrationManager.Instance != null)
                        CalibrationManager.Instance.StartMaxCalibration();
                });
            }

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(() => {
                    if (CalibrationManager.Instance != null)
                        CalibrationManager.Instance.ResetCalibration();
                });
            }

            if (doneButton != null)
            {
                doneButton.onClick.RemoveAllListeners();
                doneButton.onClick.AddListener(() => {
                    SceneLoader.LoadGame();
                });
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(() => {
                    SceneLoader.LoadMainMenu();
                });
            }
        }

        private void UpdateUI(CalibrationManager.CalibrationState state)
        {
            if (titleText != null)
                titleText.text = "KALİBRASYON";

            switch (state)
            {
                case CalibrationManager.CalibrationState.Idle:
                    if (instructionText != null) instructionText.text = INSTRUCTION_IDLE;
                    SetButtonsInteractable(true, true, true, false);
                    if (progressFill != null) progressFill.fillAmount = 0f;
                    break;

                case CalibrationManager.CalibrationState.SamplingMin:
                    if (instructionText != null) instructionText.text = INSTRUCTION_MIN;
                    SetButtonsInteractable(false, false, false, false);
                    break;

                case CalibrationManager.CalibrationState.SamplingMax:
                    if (instructionText != null) instructionText.text = INSTRUCTION_MAX;
                    SetButtonsInteractable(false, false, false, false);
                    break;

                case CalibrationManager.CalibrationState.Completed:
                    if (instructionText != null) instructionText.text = INSTRUCTION_DONE;
                    SetButtonsInteractable(true, true, true, true);
                    if (progressFill != null) progressFill.fillAmount = 1f;

                    if (statusText != null && CalibrationManager.Instance != null)
                    {
                        statusText.text = $"Min: {CalibrationManager.Instance.MinValue:F3}\n" +
                                          $"Max: {CalibrationManager.Instance.MaxValue:F3}\n" +
                                          $"Aralık: {(CalibrationManager.Instance.MaxValue - CalibrationManager.Instance.MinValue):F3}";
                    }
                    break;
            }
        }

        private void UpdateProgress(float progress)
        {
            if (progressFill != null)
            {
                progressFill.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private void SetButtonsInteractable(bool min, bool max, bool reset, bool done)
        {
            if (minButton != null) minButton.interactable = min;
            if (maxButton != null) maxButton.interactable = max;
            if (resetButton != null) resetButton.interactable = reset;
            if (doneButton != null) doneButton.interactable = done;
        }
    }
}
