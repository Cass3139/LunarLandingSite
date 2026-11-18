using System.Collections.Generic;
using System.IO;
using CSVToolKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System;
using System.Threading.Tasks;
using UnityEngine.InputSystem;
using System.Collections;

[Serializable]
public struct LocationButton
{
    public Button button;
    public LandingSiteData landingSiteData;
}

public class UIManager : MonoBehaviour
{
    [SerializeField] private LocationButton[] locationButtons;
    [Header("UI")]
    [SerializeField] Button questionButton; 
    [SerializeField] CanvasGroup viewBlocking;
    [SerializeField] Button backArrow;
    [SerializeField] GameObject calculatingText;

    [Header("Sidebar Texts")]
    [SerializeField] GameObject scoreSidebar;
    [SerializeField] GameObject tutorialSidebar;
    [SerializeField]  TMP_Text titleText;

    
    [Header("Input Fields")]
    [SerializeField] TMP_InputField yearField;
     [SerializeField] TMP_InputField monthField, dayField, hourField;

    [Header("Scores")]
    [SerializeField]  TMP_Text totalScore;
    [SerializeField] TMP_Text topo_score_text, comms_text, light_score_text;

    [Header("Control Buttons")]
    [SerializeField] Button quitButton;
    [SerializeField] private float quitFadeTime = 2;
    [SerializeField] private float backgroundFadeOutTime;
    private InputAction escapeAction;

    

    private static bool IS_CALCULATING = false; 
    string year, month, day, hour = "";
    string lastYear, lastMonth, lastday, lastHour ="";

    static string pythonDirectoryPath = Path.Combine(Application.streamingAssetsPath, "PythonScripts"); 
    static string pythonFilePath = Path.Combine(pythonDirectoryPath, "dist","main.exe");
    static string pythonDataPath = Path.Combine(pythonDirectoryPath, "Data");
    static string pathToCSV =  Path.Combine(pythonDataPath,"scores_data.csv");    
    
    string lightScore, topoScore, commsActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    bool ParseDate(TMP_InputField inputField,string s, int lowerBound, int upperBound)
    {
        
        if(int.TryParse(s, out int date))
            {
                if(date >= lowerBound && date <= upperBound)
                {
                    month = s;
                    return true;
                }
                inputField.text = "OutOfBound";
                return false;
            }
            inputField.text = "INVALID";
            return false;
    }
    void Start()
    {
        // RunPython();
        InitialUISetUp();


        questionButton.onClick.AddListener(() =>
        {
            tutorialSidebar.SetActive(!tutorialSidebar.activeSelf);
            scoreSidebar.SetActive(!tutorialSidebar.activeSelf);
        });

        escapeAction = InputSystem.actions.FindAction("Escape");
        escapeAction.performed += _ => {
            StopCoroutine("QuitButtonFade");
            StartCoroutine(QuitButtonFade());
        };

        quitButton.onClick.AddListener(() => {
            print("Quitting");
            Application.Quit();
        });

        yearField.onEndEdit.AddListener((string s) => {if(ParseDate(yearField,s, 1900, 2052))  year = s;});
        monthField.onEndEdit.AddListener((string s) => {if(ParseDate(monthField,s, 1, 12))  month = s;});
        dayField.onEndEdit.AddListener((string s) => {if(ParseDate(dayField,s, 1, 31))  day = s;});
        hourField.onEndEdit.AddListener((string s) => {if(ParseDate(hourField,s, 0, 23))  hour = s;});

        foreach(var location in locationButtons)
        {
            location.button.onClick.AddListener(()=>
            {
                if(IS_CALCULATING || year == "" || month == "" || day == "" || hour == "" ) return;

                if(TerrainManager.currentLandingSiteData == location.landingSiteData) {
                    if(hour == lastHour && day == lastday && month == lastMonth && year == lastYear)
                    {
                        RemoveBlockingUI();
                        return;
                    }
                }

                lastHour = hour; lastday = day; lastMonth = month; lastYear = year;
                TerrainManager.Instance.ChangeTerrain(location.landingSiteData);
                CameraControl.Instance.SetCenterLanding(TerrainManager.currentLandingSiteData.landingSite.terrainData);
                calculatingText.SetActive(true);
                viewBlocking.interactable = false;
                CalculateScoreAndEnterPlay();
            });
        } 

        backArrow.onClick.AddListener(() => {
            viewBlocking.alpha = 1;
            viewBlocking.interactable = true;
            backArrow.gameObject.SetActive(false);
            CameraControl.isCameraLocked = true;
        });

    }


    private void InitialUISetUp()
    {
        // print("Setup");
        tutorialSidebar.SetActive(false);
        scoreSidebar.SetActive(true);
        viewBlocking.gameObject.SetActive(true);
        backArrow.gameObject.SetActive(false);
        calculatingText.SetActive(false);
        CanvasGroup quitGroup = quitButton.GetComponent<CanvasGroup>();
        quitGroup.alpha = 0;
        quitGroup.interactable = false;
    }


    private async Task CalculateScoreAndEnterPlay()
    {
        IS_CALCULATING = true;
        await WritePython();
        await RunPython();
        await ReadPython();
        IS_CALCULATING = false;

        UpdateScoresUI();
        RemoveBlockingUI();
    } 

    void RemoveBlockingUI()
    {
        viewBlocking.interactable = false;
        StartCoroutine(FadeOutCanvasGroup(viewBlocking, backgroundFadeOutTime));
        backArrow.gameObject.SetActive(true);
        calculatingText.SetActive(false);
        tutorialSidebar.SetActive(false);
        scoreSidebar.SetActive(true);
        CameraControl.isCameraLocked = false;

    }

    void UpdateScoresUI()
    {
        light_score_text.text = lightScore +"/10";
        topo_score_text.text = topoScore +"/10";
        comms_text.text = commsActive;
        titleText.text = TerrainManager.currentLandingSiteData.landingSite.site;
        totalScore.text = (int.Parse(topoScore) + int.Parse(lightScore)).ToString() +"/20";

    }

    IEnumerator QuitButtonFade()
    {
        CanvasGroup groupe = quitButton.GetComponent<CanvasGroup>();
        groupe.alpha = 1;
        groupe.interactable = true;

        yield return new WaitForSeconds(quitFadeTime*2);

        while (groupe.alpha > 0)
        {
            groupe.alpha -= 1/quitFadeTime*Time.deltaTime;
            yield return null;
        }
        groupe.alpha = 0;
        groupe.interactable = false;
    }

    IEnumerator FadeOutCanvasGroup(CanvasGroup groupe, float fadeTime)
    {
        while (groupe.alpha > 0)
        {
            groupe.alpha -= 1/fadeTime*Time.deltaTime;
            yield return null;
        }
        groupe.alpha = 0;
    }

    private async Task RunPython()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonFilePath,
            WorkingDirectory = pythonDirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(pythonDataPath);
        startInfo.ArgumentList.Add(pathToCSV);

        var process = new Process { StartInfo = startInfo };

        process.OutputDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                UnityEngine.Debug.Log(args.Data);
        };

        process.ErrorDataReceived += (sender, args) =>
        {
            if (args.Data != null)
                UnityEngine.Debug.LogError(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        while (!process.HasExited)
        {
            await Task.Yield();
        }

        process.Dispose();
    }   

    private async Task WritePython()
    {
        string header = 
        "site_file,lat,lon,year,month,day,hour,light_score,topology_score,comms_status\n";

        LandingSite currentLandingSite = TerrainManager.currentLandingSiteData.landingSite;
        string stringData = 
        $"{currentLandingSite.slopeFile},{currentLandingSite.lat},{currentLandingSite.lon},{year},{month},{day},{hour},0,0,False";
        string output = header + stringData;
        string path = pathToCSV;
        File.WriteAllText(path, output);
    }

    private async Task ReadPython()
    {
        //Launch python script
        List<List<string>> data = CSVParser.Instance.ReadData(pythonDataPath, "scores_data.csv");
        lightScore = data[1][7];
        topoScore = data[1][8];
        commsActive = data[1][9];
    }

}
