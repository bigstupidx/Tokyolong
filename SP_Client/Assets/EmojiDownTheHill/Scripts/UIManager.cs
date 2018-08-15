﻿using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;
using SgLib;

#if EASY_MOBILE
using EasyMobile;
#endif

namespace Emoji2
{
    public class UIManager : MonoBehaviour
    {
		const int EASY_FINISH_POINT = 20;
		const int HARD_FINISH_POINT = 50;

        [Header("Object References")]
        public GameManager gameManager;
        public SelectCharacter characterSprite;
        public GameObject header;
        public Text title;
        public Text score;
        public Text bestScore;
        public GameObject nextBtn;
        public GameObject prevBtn;
        public GameObject tapToStart;
        public GameObject restartBtn;
        public GameObject menuButtons;
        public GameObject settingsUI;
        public GameObject soundOnBtn;
        public GameObject soundOffBtn;
		public RawImage imgVictory;
		public GameObject objVictory;
		public GameObject objSendServer;
		public GameObject objGameOver;

        [Header("Premium Features Buttons")]
        public GameObject leaderboardBtn;
        public GameObject shareBtn;
        public GameObject removeAdsBtn;
        public GameObject restorePurchaseBtn;

        Animator scoreAnimator;

		int finishPoint = 0;
		bool success = false;

        void OnEnable()
        {
            GameManager.GameStateChanged += GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated += OnScoreUpdated;
        }

        void OnDisable()
        {
            GameManager.GameStateChanged -= GameManager_GameStateChanged;
            ScoreManager.ScoreUpdated -= OnScoreUpdated;
        }

        // Use this for initialization
        void Start()
        {
			finishPoint = Info.GameDiscountWon == (short)EDiscount.e1000won ? EASY_FINISH_POINT : HARD_FINISH_POINT;
			score.text = "0 / " + finishPoint.ToString ();

            scoreAnimator = score.GetComponent<Animator>();

            Reset();
            ShowStartUI();
        }

        // Update is called once per frame
        void Update()
        {
            /*score.text = ScoreManager.Instance.Score.ToString();
            bestScore.text = ScoreManager.Instance.HighScore.ToString();

            if (settingsUI.activeSelf)
            {
                UpdateMuteButtons();
            }*/
        }

        void GameManager_GameStateChanged(GameState newState, GameState oldState)
        {
            if (newState == GameState.Playing)
            {              
                ShowGameUI();
            }
            else if (newState == GameState.PreGameOver)
            {
                // Before game over, i.e. game potentially will be recovered
            }
            else if (newState == GameState.GameOver)
            {
                Invoke("ShowGameOverUI", 1f);
            }
        }

        void OnScoreUpdated(int newScore)
        {
            scoreAnimator.Play("NewScore");
			score.text = ScoreManager.Instance.Score.ToString () + " / " + finishPoint.ToString();

			if (ScoreManager.Instance.Score >= finishPoint)
				StartCoroutine (_SuccessEndGame ());
        }

		IEnumerator _SuccessEndGame()
		{
			success = true;
			GameManager.Instance.GameOver ();

			ShiningGraphic.Start (imgVictory);
			objVictory.SetActive (true);
			yield return new WaitForSeconds (4f);

			UITweenAlpha.Start (objVictory, 1f, 0f, TWParam.New (.5f).Curve (TWCurve.CurveLevel2));
			UITweenAlpha.Start (objSendServer, 0f, 1f, TWParam.New (.5f).Curve (TWCurve.CurveLevel2));

			yield return new WaitForSeconds (1f);
			NetworkManager.Instance.Game_Discount_REQ (Info.GameDiscountWon);
		}

        void Reset()
        {
            settingsUI.SetActive(false);

            header.SetActive(false);
            title.gameObject.SetActive(false);
            score.gameObject.SetActive(false);
            tapToStart.SetActive(false);
            restartBtn.SetActive(false);
            menuButtons.SetActive(false);
            settingsUI.SetActive(false);

            // Enable or disable premium stuff
            bool enablePremium = PremiumFeaturesManager.Instance.enablePremiumFeatures;
            leaderboardBtn.SetActive(enablePremium);
            shareBtn.SetActive(enablePremium);
            removeAdsBtn.SetActive(enablePremium);
            restorePurchaseBtn.SetActive(enablePremium);

            // Hidden by default
            settingsUI.SetActive(false);
        }

        public void NextCharacter()
        {
            characterSprite.index++;
        }

        public void PrevCharacter()
        {
            characterSprite.index--;
        }

        public void StartGame()
        {
            gameManager.StartGame();
        }

        public void EndGame()
        {
            gameManager.GameOver();
        }

        public void RestartGame()
        {
            gameManager.RestartGame(0.2f);
        }

        public void ShowStartUI()
        {            
			tapToStart.SetActive(true);
			title.gameObject.SetActive(true);

			/*settingsUI.SetActive(false);
            header.SetActive(true);
            nextBtn.SetActive(true);
            prevBtn.SetActive(true);*/
        }

        public void ShowGameUI()
        {
			score.gameObject.SetActive(true);
			title.gameObject.SetActive(false);
			tapToStart.SetActive(false);

            /*header.SetActive(true);            
            nextBtn.SetActive(false);
            prevBtn.SetActive(false);*/
        }

        public void ShowGameOverUI()
        {
			if (success == false)
				objGameOver.SetActive (true);

            /*header.SetActive(true);
            title.gameObject.SetActive(false);
            score.gameObject.SetActive(true);
            tapToStart.SetActive(false);
            restartBtn.SetActive(true);
            menuButtons.SetActive(true);
            settingsUI.SetActive(false);*/
        }

        public void ShowSettingsUI()
        {
            settingsUI.SetActive(true);
        }

        public void HideSettingsUI()
        {
            settingsUI.SetActive(false);
        }

        public void ShowLeaderboardUI()
        {
            #if EASY_MOBILE
            if (GameServices.IsInitialized())
            {
                GameServices.ShowLeaderboardUI();
            }
            else
            {
                #if UNITY_IOS
                NativeUI.Alert("Service Unavailable", "The user is not logged in to Game Center.");
                #elif UNITY_ANDROID
                GameServices.Init();
                #endif
            }
            #endif
        }

        public void PurchaseRemoveAds()
        {
            #if EASY_MOBILE
            InAppPurchaser.Instance.Purchase(InAppPurchaser.Instance.removeAds);
            #endif
        }

        public void RestorePurchase()
        {
            #if EASY_MOBILE
            InAppPurchaser.Instance.RestorePurchase();
            #endif
        }

        public void ShareScreenshot()
        {
            #if EASY_MOBILE
            ScreenshotSharer.Instance.ShareScreenshot();
            #endif
        }

        public void ToggleSound()
        {
            SoundManager.Instance.ToggleMute();
        }

        public void RateApp()
        {
            Utilities.RateApp();
        }

        public void OpenTwitterPage()
        {
            Utilities.OpenTwitterPage();
        }

        public void OpenFacebookPage()
        {
            Utilities.OpenFacebookPage();
        }

        public void ButtonClickSound()
        {
            Utilities.ButtonClickSound();
        }

        void UpdateMuteButtons()
        {
            if (SoundManager.Instance.IsMuted())
            {
                soundOnBtn.gameObject.SetActive(false);
                soundOffBtn.gameObject.SetActive(true);
            }
            else
            {
                soundOnBtn.gameObject.SetActive(true);
                soundOffBtn.gameObject.SetActive(false);
            }
        }

        bool IsPremiumFeaturesEnabled()
        {
            return PremiumFeaturesManager.Instance != null && PremiumFeaturesManager.Instance.enablePremiumFeatures;
        }
    }
}