﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class GameController : MonoBehaviour
{
    enum GameModes { Simple, TimeFight, Perfect };

    [SerializeField]
    public GameObject QuestionText;
    [SerializeField]
    private Text QuestionNumText;
    [SerializeField]
    private Text PointsText;
    [SerializeField]
    public AnswerItem[] AnswerItems = new AnswerItem[4];

    [SerializeField] public GameObject QuestionsCanvas;
    [SerializeField] public GameObject TeacherCanvasImage;
    [SerializeField] public GameObject TeacherCanvasText;
    [SerializeField] public GameObject TeacherLeftImage;
    [SerializeField] public GameObject TeacherLeftText;
    [SerializeField] public Text EducationalText;
    [SerializeField] public GameObject TeacherEndGameObject;
    [SerializeField]
    public GameObject TeacherEndGameHighScore;
    [SerializeField]
    public Text TeacherEndGameHighScoreText;
    [SerializeField]
    public InputField TeacherEndGameHighScoreInput;
    [SerializeField] public Text TeacherEndGameText;

    [SerializeField]
    public GameObject EndGameScoreMessage;
    [SerializeField]
    private GameObject RemoveIncorrectButton;
    [SerializeField]
    private Slider TimeBar;

    private byte CorrectAnswerId;

    private SoundController soundSource;

    List<Question> QuestionList = new List<Question>();

    private int CurrentQuestionNumber = 0;
    private int Score = 0;
    private int TimeFightSeconds = 20;
    private float secondsCount;

    private bool HasGameEnded = false;

    private GameModes ModeId = GameModes.Simple;

    private bool RemovedIncorrect;

    private bool showingEducationalCanvas = false;

    void Awake()
    {
        try {
            ModeId = (GameModes)GameObject.Find("GameDataObject").GetComponent<GameData>().ModeId;
        } catch {
            throw new Exception("You should start the game from the Menu scene!");
        }

        if (ModeId == GameModes.TimeFight)
        {
            TimeBar.gameObject.SetActive(true);
            secondsCount = TimeFightSeconds;
        }
        else
            TimeBar.gameObject.SetActive(false);
    }

    void Start()
    {

        soundSource = (GameObject.Find("SoundController")).GetComponent<SoundController>();

        QuestionList.Add(new Question("Premierem którego kraju był Winston Churchill?", "Churchill był jednym z najważniejszych polityków Aliantów w czasie II wojny światowej, brał udział w konferencjach teherańskiej, jałtańskiej i poczdamskiej, gdzie ustalano losy świata po wojnie.", "Wielkiej Brytanii",
            "Stanów Zjednoczonych", "Australii", "Rosji"));
        QuestionList.Add(new Question("W którym roku odbyła się bitwa pod Grunwaldem?", "Jedna z największych bitew w historii średniowiecznej Europy, stoczona na polach pod Grunwaldem 15 lipca 1410 w czasie trwania wielkiej wojny między siłami zakonu krzyżackiego a połączonymi siłami polskimi i litewskimi.", "1410", "996", "1944", "2137"));
        QuestionList.Add(new Question("Które miasto było pierwszą stolicą Polski?", "Dagome iudex, dokument z roku 991 podpisany przez Mieszka I, wskazuje jednoznacznie, że stolicą Polski było Gniezno, choć ważną rolę pełnił również Poznań, a wczasach plemiennych - Kalisz oraz Giecz.", "Gniezno", "Warszawa", "Kalety",
            "Bogdaniec"));
        QuestionList.Add(new Question("Na terenie którego kraju powstała pierwsza cywilizacja?", "Najnowsze badania wskazują, że cywilizacja doliny Indusu istniała już 6 000 lat p.n.e., co czyni ją pierwszą cywilizacją na ziemi.", "Indie",
            "Stany Zjednoczone", "Egipt", "Rosja"));
        QuestionList.Add(new Question("Który z władców nie władał Rzymem?", "Aleksander III Macedoński, zwany Aleksandrem Wielkim, wybitny strateg, jeden z największych zdobywców w historii. Okres panowania Aleksandra wyznacza granicę między dwiema epokami historii starożytnej: okresem klasycznym i epoką hellenistyczną.", "Aleksander Wielki", "Neron",
            "Juliusz Cezar", "Kaligula"));
        StartCoroutine(DisplayStartGameCanvas());
    }
    void Update()
    {
        if (ModeId == GameModes.TimeFight && showingEducationalCanvas == false )
        {
            delayUntilEndGame();
        }
    }
    public void GetAnswer(AnswerItem clickedAnswer)
    {
        if (clickedAnswer.answer.isCorrect())
        {
            clickedAnswer.showAsCorrect();
            soundSource.playCorrectSound();
            Score++;

            PointsText.text = "Punktów: " + Score;
        }
        else
        {
            soundSource.playIncorrectSound();
            clickedAnswer.showAsIncorrect();
            AnswerItems[CorrectAnswerId].showAsCorrect();
            if (ModeId == GameModes.Perfect) StartCoroutine(DisplayEndGameCanvas());
        }

        StartCoroutine(delayAfterAnswer());
    }

    public void removeTwoIncorrectAnswers()
    {
        if (RemovedIncorrect) return;

        RemovedIncorrect = true;
        RemoveIncorrectButton.SetActive(false);

        System.Random rnd = new System.Random();
        AnswerItem[] items = AnswerItems.OrderBy(x => rnd.Next()).
            Where(item => item.answer.isCorrect() == false).
            Take(2).ToArray();

        foreach (AnswerItem item in items)
        {
            item.hideButton();
        }
    }
    IEnumerator delayAfterAnswer()
    {
        foreach (AnswerItem item in AnswerItems)
            item.disableButton();

        yield return new WaitForSeconds(1);
        StartCoroutine(DisplayEducationalCanvas());
    }
    public void skipQuestion( GameObject skipButton )
    {
        skipButton.SetActive(false);

        foreach (AnswerItem item in AnswerItems)
            item.disableButton();

        StartCoroutine(DisplayEducationalCanvas());
    }
    void NextQuestion(Question q)
    {
        RemovedIncorrect = false;
        //RemoveIncorrectButton.SetActive(true);
        QuestionNumText.text = "Pytanie: " + (CurrentQuestionNumber+1) + "/" + QuestionList.Count;
        QuestionText.GetComponent<Text>().text = q.QuestionText;
        Answer[] shuffledAnswers = Question.shuffleAnswers(q);
        CorrectAnswerId = (byte)Array.IndexOf(shuffledAnswers, q.Answers[0]);

        for (int i = 0; i < 4; i++)
        {
            AnswerItems[i].showButton();
            AnswerItems[i].setAnswer(shuffledAnswers[i]);
        }
    }

    void delayUntilEndGame()
    {
        TimeBar.value = secondsCount / TimeFightSeconds;
        secondsCount -= Time.deltaTime;
        if (secondsCount <= 0)
        {
            StartCoroutine(DisplayEndGameCanvas());
        }
    }

    IEnumerator DisplayEducationalCanvas()
    {
        if (!HasGameEnded)
        {
            showingEducationalCanvas = true;
            QuestionsCanvas.SetActive(false);
            Transform teacherPosition = TeacherLeftImage.GetComponent<Transform>();
            while (teacherPosition.localPosition.x < -600)
            {
                teacherPosition.localPosition += Vector3.right * Time.deltaTime / 0.001f;
                yield return null;
            }
            TeacherLeftText.SetActive(true);
            EducationalText.text = QuestionList[(CurrentQuestionNumber++)].EducationalText;
            while (!Input.GetMouseButtonDown(0))
            {
                yield return null;
            }
            TeacherLeftText.SetActive(false);
            while (teacherPosition.localPosition.x > -1200)
            {
                teacherPosition.localPosition += Vector3.left * Time.deltaTime / 0.001f;
                yield return null;
            }
            QuestionsCanvas.SetActive(true);
            if (CurrentQuestionNumber < QuestionList.Count)
            {
                NextQuestion(QuestionList[CurrentQuestionNumber]);
                foreach (AnswerItem item in AnswerItems)
                {
                    item.enableButton();
                }
                showingEducationalCanvas = false;
            }
            else StartCoroutine(DisplayEndGameCanvas());
        }
    }

    IEnumerator DisplayStartGameCanvas()
    {
        Transform teacherPosition = TeacherCanvasImage.GetComponent<Transform>();
        while (teacherPosition.localPosition.x > 600)
        {
            teacherPosition.localPosition += Vector3.left * Time.deltaTime / 0.001f;
            yield return null;
        }
        TeacherCanvasText.SetActive(true);
        yield return new WaitForSeconds(3);
        TeacherCanvasText.SetActive(false);
        while (teacherPosition.localPosition.x < 1200)
        {
            teacherPosition.localPosition += Vector3.right * Time.deltaTime / 0.001f;
            yield return null;
        }
        QuestionsCanvas.SetActive(true);
        NextQuestion(QuestionList[CurrentQuestionNumber]);
    }

    IEnumerator DisplayEndGameCanvas()
    {
        HasGameEnded = true;
        QuestionsCanvas.SetActive(false);
        Transform teacherPosition = TeacherCanvasImage.GetComponent<Transform>();
        while (teacherPosition.localPosition.x > 600)
        {
            teacherPosition.localPosition += Vector3.left * Time.deltaTime / 0.001f;
            yield return null;
        }
        if( hasBeatenRecord(Score) ) {
            String EndGameMessage = "Gratulacje! Pobiłeś nowy rekord! " + Score + "/" + CurrentQuestionNumber;
            TeacherEndGameHighScoreText.text = EndGameMessage;
            TeacherEndGameHighScore.SetActive(true);
        }
        else if( Score * 100 / CurrentQuestionNumber > 40 )
        {
            String EndGameMessage = "Gratulacje! Zdałeś! \n Twój wynik to " + Score + "/" + CurrentQuestionNumber;
            TeacherEndGameText.text = EndGameMessage;
            TeacherEndGameObject.SetActive(true);
        }
        else
        {
            String EndGameMessage = "Niestety oblałeś! \n Twój wynik to " + Score + "/" + CurrentQuestionNumber;
            TeacherEndGameText.text = EndGameMessage;
            TeacherEndGameObject.SetActive(true);
        }
    }

    public void saveRecord()
    {
        string newName = TeacherEndGameHighScoreInput.text;
        string prefix = getPrefix();

        int myPlace = 0;
        for (int i = 3; i > 0; i-- )
            if (PlayerPrefs.GetInt(prefix + "_points_" + i) < Score)
                myPlace = i;

        for( int i = 2; i > myPlace; i--)
        {
            PlayerPrefs.SetInt(prefix + "_points_" + i, PlayerPrefs.GetInt(prefix + "_points_" + (i-1)));
            PlayerPrefs.SetString(prefix + "_name_" + i, PlayerPrefs.GetString(prefix + "_name_" + (i-1)));
        }
        PlayerPrefs.SetInt(prefix + "_points_" + myPlace, Score);
        PlayerPrefs.SetString(prefix + "_name_" + myPlace, newName);
        ReturnToMenu();
    }
    bool hasBeatenRecord( int num )
    {
        string prefix = getPrefix();
        if (PlayerPrefs.GetInt(prefix + "_points_" + 3) < num)
            return true;

        return false;
    }

    string getPrefix()
    {
        switch (ModeId)
        {
            case GameModes.Simple: return "Standard";
            case GameModes.TimeFight: return "Time";
            case GameModes.Perfect: return "Perfect";
            default: return "Standard";
        }
    }
    public void ReturnToMenu()
    {
        Destroy(GameObject.Find("GameDataObject"));
        SceneManager.LoadScene(0);
    }
}
