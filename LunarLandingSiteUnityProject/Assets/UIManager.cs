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


public class UIManager : MonoBehaviour
{
    [SerializeField] GameObject credits;
    [SerializeField] Button questionButton; 
    [SerializeField] GameObject scoreText;
    [SerializeField] GameObject tutorialText;
    [SerializeField]  Button HaworthButton;
    [SerializeField] Button ConnectingRidgeButton;
    [SerializeField] TMP_InputField yearField, monthField, dayField, hourField;

    [SerializeField]  TMP_Text light_score_text;
    [SerializeField]  TMP_Text topo_score_text;
    [SerializeField]  TMP_Text comms_text;
    [SerializeField]  TMP_Text titleText, totalScore;
    


    string year, month, day, hour;

    static string pythonDirectoryPath = Path.Combine(Application.streamingAssetsPath, "PythonScripts"); 
    static string pythonFilePath = Path.Combine(pythonDirectoryPath, "dist","main.exe");
    static string pathToCSV =  Path.Combine(pythonDirectoryPath, "scores_data.csv");    
    struct LandingSite
    {
        public string site,file,lat,lon;
        public LandingSite(string _site,string _file,string _lat,string _lon)
        {
            site = _site;
            file = _file;
            lat = _lat;
            lon = _lon;
        }
    }

    LandingSite Haworth = new(
        "Haworth",
        "Haworth_final_adj_5mpp_slp.tif",
        "-86.7",
        "336");

    LandingSite ConnectingRidge = new(
        "01 - Connecting Ridge",
        "Site01_final_adj_5mpp_slp.tif",
        "-89.5",
        "223");
    
    string lightScore, topoScore, commsActive;
    LandingSite currentLandingSite;

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

        questionButton.onClick.AddListener(() =>
        {
            credits.SetActive(!credits.activeSelf);
            tutorialText.SetActive(credits.activeSelf);
            scoreText.SetActive(!credits.activeSelf);
        });

        yearField.onEndEdit.AddListener((string s) => {if(ParseDate(yearField,s, 1900, 2053)) year = s;});
        monthField.onEndEdit.AddListener((string s) => {if(ParseDate(monthField,s, 1, 12)) month = s;});
        dayField.onEndEdit.AddListener((string s) => {if(ParseDate(dayField,s, 1, 31)) day = s;});
        hourField.onEndEdit.AddListener((string s) => {if(ParseDate(hourField,s, 0, 23)) hour = s;});

        HaworthButton.onClick.AddListener(()=>
        {
            currentLandingSite = Haworth;
            CalculateScores();
            UpdateScores();

        });
        ConnectingRidgeButton.onClick.AddListener(()=>
        {
            currentLandingSite = ConnectingRidge;
            CalculateScores();
            UpdateScores();
        });   
    }


    void CalculateScores()
    {
        WritePython();
        RunPython();
        ReadPython();
    } 
    void UpdateScores()
    {
        light_score_text.text = lightScore;
        topo_score_text.text = topoScore;
        comms_text.text = commsActive;
        titleText.text = currentLandingSite.site;
        totalScore.text = (int.Parse(topoScore) + int.Parse(lightScore)).ToString();
    }

    private void RunPython()
    {
        print(Application.dataPath);
        var startInfo = new ProcessStartInfo
        {
            FileName = pythonFilePath,
            WorkingDirectory = pythonDirectoryPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(pythonDirectoryPath);
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

        // string scriptsDir = Path.Combine(GetPath(), "PythonScripts");
        // print(scriptsDir);
        // string venvPython = Path.Combine(scriptsDir, ".venv", "bin", "python");
        // string mainPy = Path.Combine(scriptsDir, "main.py");

        // if (!File.Exists(venvPython)) { UnityEngine.Debug.LogError($"Missing venv python: {venvPython}"); return; }
        // if (!File.Exists(mainPy))     { UnityEngine.Debug.LogError($"Missing main.py: {mainPy}"); return; }

        // Run(venvPython, new[] { mainPy }, scriptsDir);
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
        print("WRIIITE");
        string header = 
        "site_file,lat,lon,year,month,day,hour,light_score,topology_score,comms_status\n";
        string stringData = 
        $"{currentLandingSite.file},{currentLandingSite.lat},{currentLandingSite.lon},{year},{month},{day},{hour},0,0,False";
        string output = header + stringData;
        string path = pathToCSV;
        File.WriteAllText(path, output);
    }

    private void ReadPython()
    {
        print("REEEEAD");
        //Launch python script
        List<List<string>> data = CSVParser.Instance.ReadData(pythonDirectoryPath, "scores_data.csv");
        lightScore = data[1][7];
        topoScore = data[1][8];
        commsActive = data[1][9];
        print($"{lightScore},{topoScore},{commsActive}");
    }

}
