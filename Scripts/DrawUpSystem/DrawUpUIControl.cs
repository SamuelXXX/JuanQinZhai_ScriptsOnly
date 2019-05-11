using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DrawUpUIControl : MonoBehaviour
{
    #region Structure Symbol Defination
    const char sentenceSplitSymbol = '|';
    const char hintSymbol = '#';
    const char answerSplitSymbol = '/';
    const char rightAnswerSymbol = '&';
    const char answerBeginningSymbol = '@';
    #endregion

    #region Singleton
    private static DrawUpUIControl singleton;
    public static DrawUpUIControl Singleton
    {
        get
        {
            return singleton;
        }
    }
    #endregion

    #region Inner-class Defination
    [System.Serializable]
    public class OptionUIItem
    {
        public Button controlButton;

        DrawUpUIControl manager;
        Text optionText;
        Canvas canvas;
        string hintSentence;
        bool isRightAnswer;
        public void Initialize(DrawUpUIControl manager)
        {
            this.manager = manager;
            if (controlButton != null)
            {
                controlButton.onClick.AddListener(OnClickCallBack);
                optionText = controlButton.GetComponentInChildren<Text>();
                canvas = controlButton.GetComponent<Canvas>();
            }
        }

        void OnClickCallBack()
        {
            if (manager == null)
                return;

            if (isRightAnswer)
            {
                manager.OnClickRightAnswer(hintSentence);
            }
            else
            {
                manager.OnClickWrongAnswer(hintSentence);
            }
        }

        public void ShowAnswer(AnswerParseResult result)
        {
            if (controlButton == null)
                return;

            if (optionText == null)
                optionText = controlButton.GetComponentInChildren<Text>();

            if (optionText == null)
                return;

            isRightAnswer = result.isRightAnswer;
            optionText.text = result.answer;
            hintSentence = result.answerHint;

            if (canvas == null)
                canvas = controlButton.GetComponent<Canvas>();

            if (canvas != null)
                canvas.enabled = true;
        }

        public void HideAnswer()
        {
            if (canvas == null)
                canvas = controlButton.GetComponent<Canvas>();

            if (canvas != null)
                canvas.enabled = false;
        }
    }

    public struct AnswerParseResult
    {
        public bool isRightAnswer;
        public string answer;
        public string answerHint;
    }
    #endregion

    #region Settings and run-time data
    [Header("Normal Conversation Settings")]
    public Text conversationText;
    public Image roleImage;
    public Image nextButton;
    public bool enableSlowAppear = false;
    public float appearTimeGap = 0.1f;

    [Header("Question Conversation Settings")]
    public List<OptionUIItem> optionUIList = new List<OptionUIItem>();
    public string defaultRightAnswerResponse = "不错，孺子可教也！";
    public string defaultWrongAnswerResponse = "不对，再试试看！";


    protected Canvas mCanvas;
    /// <summary>
    /// Sentence items list parse from original sentences-combined string of DrawUpContentProvider
    /// </summary>
    List<string> sentencesList = new List<string>();
    List<AnswerParseResult> answersList = new List<AnswerParseResult>();

    int sentenceIndex = 0;
    string currentSentenceBuffer;
    #endregion

    #region Mono Events
    void Awake()
    {
        singleton = this;

        foreach (var o in optionUIList)
        {
            o.Initialize(this);
        }

        mCanvas = GetComponent<Canvas>();
    }

    private void Start()
    {
        GlobalEventManager.RegisterHandler(GlobalEventManager.DrawUpClicked, OnClickDrawUp);
    }

    private void OnDestroy()
    {
        if (singleton == this)
        {
            singleton = null;
        }

        GlobalEventManager.UnregisterHandler(GlobalEventManager.DrawUpClicked, OnClickDrawUp);
    }

    void Update()
    {

    }
    #endregion

    #region Call Back Events
    /// <summary>
    /// Called when draw up is clicked
    /// </summary>
    /// <param name="evt"></param>
    void OnClickDrawUp(GlobalEvent evt)
    {
        if (answerTableOn)
            return;

        if (DrawUpUIControl.Singleton.DrawUpCompleted)
        {
            HideDrawUp();
        }
        else if (SentenceCompleted)
        {
            ShowNextSentence();
        }
        else
        {
            DrawUpUIControl.Singleton.CompleteSentence();
        }
    }

    /// <summary>
    /// Called when this sentence is completed
    /// </summary>
    void OnSentenceCompleted()
    {
        if (nextButton != null)
            nextButton.enabled = true;
        if (needToAnswerAtCurrentSentence)
        {
            ShowAnswerTable();
        }
    }

    /// <summary>
    /// Called when click the right answer to this question
    /// </summary>
    /// <param name="hintSentence"></param>
    void OnClickRightAnswer(string hintSentence)
    {
        if (string.IsNullOrEmpty(hintSentence))
            hintSentence = defaultRightAnswerResponse;

        Sprite rightFace = DrawUpContentProvider.Singleton.GetSprite("Me", "right");
        if (rightFace)
        {
            roleImage.sprite = rightFace;
        }
        HideAnswerTable();
        ShowResponseSentence(hintSentence);
    }

    /// <summary>
    /// Called when click the wrong answer to this question
    /// </summary>
    /// <param name="hintSentence"></param>
    void OnClickWrongAnswer(string hintSentence)
    {
        if (string.IsNullOrEmpty(hintSentence))
            hintSentence = defaultWrongAnswerResponse;

        Sprite wrongFace = DrawUpContentProvider.Singleton.GetSprite("Me", "wrong");
        if (wrongFace)
        {
            roleImage.sprite = wrongFace;
        }
        HideAnswerTable();
        ShowResponseSentence(hintSentence);
        //To repeat question when click draw up
        sentenceIndex--;
    }
    #endregion

    #region Conversation Control
    IEnumerator SlowAppear()
    {
        int i = 0;
        if (nextButton != null)
            nextButton.enabled = false;
        while (currentSentenceBuffer.Length != 0 && i < currentSentenceBuffer.Length)
        {
            conversationText.text = currentSentenceBuffer.Substring(0, i + 1);
            i++;
            yield return new WaitForSeconds(appearTimeGap);
        }
        OnSentenceCompleted();
    }

    /// <summary>
    /// Sentence completed flag
    /// </summary>
    public bool SentenceCompleted
    {
        get
        {
            return currentSentenceBuffer.Length == conversationText.text.Length;
        }
    }

    /// <summary>
    /// All draw content completed flag
    /// </summary>
    public bool DrawUpCompleted
    {
        get
        {
            return SentenceCompleted && sentenceIndex == sentencesList.Count - 1;
        }
    }

    /// <summary>
    /// Get content from conversation provider and put it on draw up ui
    /// </summary>
    public void ShowDrawUp()
    {
        sentencesList.Clear();
        sentencesList.AddRange(DrawUpContentProvider.Singleton.currentConversation.Split(sentenceSplitSymbol));
        sentenceIndex = 0;
        ParseSentence(sentencesList[sentenceIndex]);

        roleImage.sprite = DrawUpContentProvider.Singleton.currentSprite;
        
        if (!enableSlowAppear)
            DrawUpUIControl.Singleton.CompleteSentence();
        else
            StartCoroutine("SlowAppear");

        mCanvas.enabled = true;
    }

    void HideDrawUp()
    {
        mCanvas.enabled = false;
    }

    #region Answers Table Control
    bool answerTableOn = false;
    bool needToAnswerAtCurrentSentence = false;
    void ShowAnswerTable()
    {
        for (int i = 0; i < answersList.Count; i++)
        {
            if (i >= optionUIList.Count)
            {
                break;
            }
            optionUIList[i].HideAnswer();
        }

        for (int i = 0; i < answersList.Count; i++)
        {
            if (i >= optionUIList.Count)
            {
                break;
            }
            optionUIList[i].ShowAnswer(answersList[i]);
        }
        answerTableOn = true;
    }

    void HideAnswerTable()
    {
        foreach (var a in optionUIList)
        {
            a.HideAnswer();
        }
        answerTableOn = false;
    }
    #endregion

    #region Sentence Parsing Tool
    /// <summary>
    /// Parsing this sentence and store all question-answers info in cache
    /// </summary>
    /// <param name="sentence"></param>
    void ParseSentence(string sentence)
    {
        if (string.IsNullOrEmpty(sentence))
            return;
        answersList.Clear();
        int indexOfAnsweresBeginner = sentence.IndexOf(answerBeginningSymbol);
        //Is a question
        if (indexOfAnsweresBeginner >= 0)
        {
            currentSentenceBuffer = sentence.Substring(0, indexOfAnsweresBeginner);
            string answers = sentence.Substring(indexOfAnsweresBeginner + 1, sentence.Length - indexOfAnsweresBeginner - 1);
            if (string.IsNullOrEmpty(answers))
            {
                needToAnswerAtCurrentSentence = false;
                return;
            }

            string[] answersArray = answers.Split(answerSplitSymbol);

            foreach (var a in answersArray)
            {
                answersList.Add(ParseAnswer(a));
            }
        }
        else//Is not a question
        {
            currentSentenceBuffer = sentence;
        }

        if (answersList.Count != 0)
            needToAnswerAtCurrentSentence = true;
        else
            needToAnswerAtCurrentSentence = false;
    }

    AnswerParseResult ParseAnswer(string answer)
    {
        AnswerParseResult result = new AnswerParseResult();
        int indexOfHintBeginner = answer.IndexOf(hintSymbol);

        string answerContent = null;
        string answerHint = null;
        //is a question
        if (indexOfHintBeginner >= 0)
        {
            answerContent = answer.Substring(0, indexOfHintBeginner);
            answerHint = answer.Substring(indexOfHintBeginner + 1, answer.Length - indexOfHintBeginner - 1);
        }
        else//is not a question
        {
            answerContent = answer;
        }

        result.answerHint = answerHint;
        if (answerContent[0] == rightAnswerSymbol)
        {
            result.answer = answerContent.Substring(1);
            result.isRightAnswer = true;
        }
        else
        {
            result.answer = answerContent;
            result.isRightAnswer = false;
        }
        return result;
    }
    #endregion

    #region Sentence sequence Control
    void CompleteSentence()
    {
        if(enableSlowAppear)
            StopCoroutine("SlowAppear");
        conversationText.text = currentSentenceBuffer;
        OnSentenceCompleted();
    }

    void ShowNextSentence()
    {
        StopAllCoroutines();
        sentenceIndex++;
        ParseSentence(sentencesList[sentenceIndex]);

        if (enableSlowAppear)
            StartCoroutine("SlowAppear");
        else
            DrawUpUIControl.Singleton.CompleteSentence();

        Sprite newFace = DrawUpContentProvider.Singleton.GetSprite("Me", "default");
        if (needToAnswerAtCurrentSentence)
        {
            newFace = DrawUpContentProvider.Singleton.GetSprite("Me", "doubt");
        }

        if (newFace)
        {
            roleImage.sprite = newFace;
        }
    }

    void ShowResponseSentence(string response)
    {
        if (enableSlowAppear)
            StopCoroutine("SlowAppear");
        currentSentenceBuffer = response;
        needToAnswerAtCurrentSentence = false;
        if(enableSlowAppear)
            StartCoroutine("SlowAppear");
        else
            DrawUpUIControl.Singleton.CompleteSentence();
    }
    #endregion

    public bool isHiding
    {
        get
        {
            return mCanvas.enabled == false;
        }
    }
    #endregion
}
