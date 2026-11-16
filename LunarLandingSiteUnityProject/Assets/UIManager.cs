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
    
    LandingSite currentLandingSite;

    string lightScore, topoScore, commsActive;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RunPython();
        credits.SetActive(false);
        tutorialText.SetActive(false);
        scoreText.SetActive(true);

        // using var process = Process.Start("dotnet", "--version");
        // process.WaitForExit();

        // print($"exit code: {process.ExitCode}");

        questionButton.onClick.AddListener(() =>
        {
            credits.SetActive(!credits.activeSelf);
            tutorialText.SetActive(credits.activeSelf);
            scoreText.SetActive(!credits.activeSelf);
        });

        yearField.onEndEdit.AddListener((string s) =>
        {
            year = s;
        });
        monthField.onEndEdit.AddListener((string s) =>
        {
            month = s;
        });
        dayField.onEndEdit.AddListener((string s) =>
        {
            day = s;
        });
        hourField.onEndEdit.AddListener((string s) =>
        {
            hour = s;
        });

        HaworthButton.onClick.AddListener(()=>
        {
            currentLandingSite = Haworth;
            WritePython();
            RunPython();
            ReadPython();
            light_score_text.text = lightScore;
            topo_score_text.text = topoScore;
            comms_text.text = commsActive;
            titleText.text = Haworth.site;
            totalScore.text = (int.Parse(topoScore) + int.Parse(lightScore)).ToString();

        });
        ConnectingRidgeButton.onClick.AddListener(()=>
        {
            currentLandingSite = ConnectingRidge;
            WritePython();
            RunPython();
            ReadPython();
            light_score_text.text = lightScore;
            topo_score_text.text = topoScore;
            comms_text.text = commsActive;
            titleText.text = ConnectingRidge.site;
            totalScore.text = (int.Parse(topoScore) + int.Parse(lightScore)).ToString();
        });
        
    }

private void RunPython()
{
    string scriptsDir = Path.Combine(GetPath(), "PythonScripts");
    print(scriptsDir);
    string venvPython = Path.Combine(scriptsDir, ".venv", "bin", "python");
    string mainPy = Path.Combine(scriptsDir, "main.py");

    if (!File.Exists(venvPython)) { UnityEngine.Debug.LogError($"Missing venv python: {venvPython}"); return; }
    if (!File.Exists(mainPy))     { UnityEngine.Debug.LogError($"Missing main.py: {mainPy}"); return; }

    Run(venvPython, new[] { mainPy }, scriptsDir);
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
        string path = GetPath() + "/PythonScripts/scores_data.csv";
        File.WriteAllText(path, output);
    }

    private void ReadPython()
    {
        print("REEEEAD");
        //Launch python script
        List<List<string>> data = CSVParser.Instance.ReadData(
                GetPath() + "/PythonScripts", "scores_data.csv");
        lightScore = data[1][7];
        topoScore = data[1][8];
        commsActive = data[1][9];
        print($"{lightScore},{topoScore},{commsActive}");
    }

    private static string GetPath(){
			#if UNITY_EDITOR
			return Application.dataPath;
			#elif UNITY_ANDROID
			return Application.persistentDataPath;
			#elif UNITY_IPHONE
			return GetiPhoneDocumentsPath();
			#else
			return Application.dataPath;
			#endif
		}
}
