using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using CSVToolKit;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using System.Text;
using System;
using System.Threading.Tasks;

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
    [SerializeField] GameObject credits;
    [SerializeField] Button questionButton; 
    [SerializeField] CanvasGroup viewBlocking;
    [SerializeField] Button backArrow;

    [Header("Sidebar Texts")]
    [SerializeField] GameObject scoreText;
    [SerializeField] GameObject tutorialText;
    [SerializeField]  TMP_Text titleText;

    
    [Header("Input Fields")]
    [SerializeField] TMP_InputField yearField;
     [SerializeField] TMP_InputField monthField, dayField, hourField;

    [Header("Scores")]
    [SerializeField]  TMP_Text totalScore;
    [SerializeField] TMP_Text topo_score_text, comms_text, light_score_text;
    


    string year, month, day, hour = "";

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
        RunPython();
        credits.SetActive(false);
        tutorialText.SetActive(false);
        scoreText.SetActive(true);
        viewBlocking.gameObject.SetActive(true);
        backArrow.gameObject.SetActive(false);

        questionButton.onClick.AddListener(() =>
        {
            credits.SetActive(!credits.activeSelf);
            tutorialText.SetActive(credits.activeSelf);
            scoreText.SetActive(!credits.activeSelf);
        });

        yearField.onEndEdit.AddListener((string s) => {if(ParseDate(yearField,s, 1900, 2052)) year = s;});
        monthField.onEndEdit.AddListener((string s) => {if(ParseDate(monthField,s, 1, 12)) month = s;});
        dayField.onEndEdit.AddListener((string s) => {if(ParseDate(dayField,s, 1, 31)) day = s;});
        hourField.onEndEdit.AddListener((string s) => {if(ParseDate(hourField,s, 0, 23)) hour = s;});

        foreach(var location in locationButtons)
        {
            location.button.onClick.AddListener(()=>
            {
                if(year == "" || month == "" || day == "" || hour == "") return;

                if(TerrainManager.currentLandingSiteData == location.landingSiteData) {
                    RemoveBlockingUI();
                    return;
                }
                TerrainManager.Instance.ChangeTerrain(location.landingSiteData);
                CameraControl.Instance.SetCenterLanding(TerrainManager.currentLandingSiteData.landingSite.terrainData);
                CalculateScores();
                UpdateScoresUI();

                RemoveBlockingUI();
            });
        } 

        backArrow.onClick.AddListener(() => {
            viewBlocking.alpha = 1;
            viewBlocking.interactable = true;
            backArrow.gameObject.SetActive(false);
            CameraControl.isCameraLocked = true;
        });

    }


    void CalculateScores()
    {
        WritePython();
        RunPython();
        ReadPython();
    } 

    void RemoveBlockingUI()
    {
        viewBlocking.interactable = false;
        viewBlocking.alpha = 0;
        backArrow.gameObject.SetActive(true);
        CameraControl.isCameraLocked = false;
    }

    void UpdateScoresUI()
    {
        light_score_text.text = lightScore;
        topo_score_text.text = topoScore;
        comms_text.text = commsActive;
        titleText.text = TerrainManager.currentLandingSiteData.landingSite.site;
        totalScore.text = (int.Parse(topoScore) + int.Parse(lightScore)).ToString();

    }

    private void RunPython()
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
        var logs = new StringBuilder();
        var errors = new StringBuilder();

        process.OutputDataReceived += (_, e) => { if (e.Data != null) logs.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data != null) errors.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        if (logs.Length > 0)  UnityEngine.Debug.Log(logs.ToString());
        if (errors.Length > 0) UnityEngine.Debug.LogError(errors.ToString());

        process.WaitForExit();

        UnityEngine.Debug.Log($"Python exit code: {process.ExitCode}");
        if (logs.Length > 0)  UnityEngine.Debug.Log(logs.ToString());
        if (errors.Length > 0) UnityEngine.Debug.LogError(errors.ToString());
    }   

    private void Run(string file, string[] args, string workingDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = file,
            WorkingDirectory = workingDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var a in args) psi.ArgumentList.Add(a);   // handles spaces safely

        using var process = new Process { StartInfo = psi };
        var sb = new StringBuilder();
        var err = new StringBuilder();
        process.OutputDataReceived += (_, e) => { if (e.Data != null) sb.AppendLine(e.Data); };
        process.ErrorDataReceived  += (_, e) => { if (e.Data != null) err.AppendLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        if (sb.Length > 0)  UnityEngine.Debug.Log(sb.ToString());
        if (err.Length > 0) UnityEngine.Debug.LogError(err.ToString());

        process.WaitForExit();

        UnityEngine.Debug.Log($"Python exit code: {process.ExitCode}");
        if (sb.Length > 0)  UnityEngine.Debug.Log(sb.ToString());
        if (err.Length > 0) UnityEngine.Debug.LogError(err.ToString());
    }

    private void WritePython()
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

    private void ReadPython()
    {
        //Launch python script
        List<List<string>> data = CSVParser.Instance.ReadData(pythonDataPath, "scores_data.csv");
        lightScore = data[1][7];
        topoScore = data[1][8];
        commsActive = data[1][9];
    }

}
