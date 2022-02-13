using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class FOVscript : MonoBehaviour
{
#pragma warning disable 0649
    private bool TwitchPlaysActive;
#pragma warning restore 0649

    public new KMAudio audio;
    public KMAudio.KMAudioRef audioRef;
    public KMBombInfo bomb;
	public KMBombModule module;
    public KMBossModule bossHandler;

    public KMSelectable[] buttons;
    public KMSelectable bigScreen, smallScreen;
    public TextMesh stageCount, input, playButton;

    private int currentStage = -1;
    private int correctInputs = 0;
    private int totalStages;
    private int prevMod, prevSpeaker;
    private string[] ignoredModules;

    private int[] initialString;
    private int[] positions;
    private string finalString;
    public AudioClip[] speakers, solveSounds, strikeSounds;

    private string[] tableA = new string[]
    {
        "ShadowMeow", "Dicey", "MásQuéÉlite", "Obvious", "1254",
        "Dbros1000", "Bomberjack", "Danielstigman", "Depresso", "ktane1",
        "OEGamer", "jTIS", "Krispy", "Grunkle Squeaky", "Arceus",
        "ScopingLandscape", "Emik", "GhostSalt", "Short_c1rcuit", "Eltrick",
        "Axodeau", "Asew", "Cooldoom", "Piissii", "CrazyCaleb"
    };

    private int[] tableB = new int[]
    {
        00, 47, 18, 76, 29, 93, 85, 34, 61, 52,
        86, 11, 57, 28, 70, 39, 94, 45, 02, 63,
        95, 80, 22, 67, 38, 71, 49, 56, 13, 04,
        59, 96, 81, 33, 07, 48, 72, 60, 24, 15,
        73, 69, 90, 82, 44, 17, 58, 01, 35, 26,
        68, 74, 09, 91, 83, 55, 27, 12, 46, 30,
        37, 08, 75, 19, 92, 84, 66, 23, 50, 41,
        14, 25, 36, 40, 51, 62, 03, 77, 88, 99,
        21, 32, 43, 54, 65, 06, 10, 89, 97, 78,
        42, 53, 64, 05, 16, 20, 31, 98, 79, 87,
    };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool recovery = false;
    private bool prevVoice = false;
    private bool moduleActivated, inputMode, moduleSolved; // Some helpful booleans

    void Awake()
    {
    	moduleId = moduleIdCounter++;
        for (int i = 0; i < buttons.Length; i++)
        {
            int j = i;
            buttons[j].OnInteract += () => { buttonHandler(j); return false; };
        }
        bigScreen.OnInteract += () => { playHandler(); return false; };
        smallScreen.OnInteract += () => { recoveryHandler(); return false; };
    }

    void Start()
    {
        if (ignoredModules == null)
            ignoredModules = GetComponent<KMBossModule>().GetIgnoredModules("Forget Our Voices", new string[]{
                "14",
                "42",
                "501",
                "A>N<D",
                "Bamboozling Time Keeper",
                "Black Arrows",
                "Brainf---",
                "Busy Beaver",
                "Concentration",
                "Duck Konundrum",
                "Don't Touch Anything",
                "Floor Lights",
                "Forget Any Color",
                "Forget Enigma",
                "Forget Everything",
                "Forget Infinity",
                "Forget It Not",
                "Forget Maze Not",
                "Forget Me Later",
                "Forget Me Not",
                "Forget Our Voices",
                "Forget Perspective",
                "Forget The Colors",
                "Forget Them All",
                "Forget This",
                "Forget Us Not",
                "Iconic",
                "Keypad Directionality",
                "Kugelblitz",
                "Multitask",
                "OmegaDestroyer",
                "OmegaForest",
                "Organization",
                "Password Destroyer",
                "Purgatory",
                "RPS Judging",
                "Security Council",
                "Shoddy Chess",
                "Simon Forgets",
                "Simon's Stages",
                "Souvenir",
                "Tallordered Keys",
                "The Time Keeper",
                "Timing is Everything",
                "The Troll",
                "Turn The Key",
                "The Twin",
                "Übermodule",
                "Ultimate Custom Night",
                "The Very Annoying Button",
                "Whiteout"
            });

        module.OnActivate += delegate ()
        {
            totalStages = bomb.GetSolvableModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
            if (totalStages > 0)
            {
                Debug.LogFormat("[Forget Our Voices #{0}]: {1} stage(s) generatable.", moduleId, totalStages);
                initialString = new int[totalStages];
                positions = new int[totalStages];
                moduleActivated = true;
            }
            else
            {
                Debug.LogFormat("[Forget Our Voices #{0}]: No stages can be generated, autosolving module.", moduleId);
                stageCount.text = "OH";
                module.HandlePass();
            }
        };
        bomb.OnBombExploded += delegate ()
        {
            if (currentStage < totalStages)
            {
                Debug.LogFormat("[Forget Our Voices #{0}]: Up to {1} stage(s) are displayed before the bomb detonated. For checking, the answer string so far is {2}.", moduleId, currentStage + 1, finalString);
            }
        };
        playButton.text = "▶";
        input.text = "";

    }

    void inputTime()
    {
        playButton.text = "";
        stageCount.text = "--";
        inputMode = true;
        Debug.LogFormat("[Forget Our Voices #{0}]: Input time! Your final string should be {1}.", moduleId, finalString);
        ShowCurrentInput();
    }

    void playHandler()
    {
        bigScreen.AddInteractionPunch(0.5f);
        if (!inputMode)
        {
            Debug.Log(speakers[initialString[currentStage]]);            
            audio.PlaySoundAtTransform(speakers[initialString[currentStage]].name, transform);
        }
        else if (recovery)
        {
            if (correctInputs == 0)
            {
                audio.PlaySoundAtTransform(speakers[initialString[correctInputs]].name, transform);
            }
            else if (prevVoice)
            {
                audio.PlaySoundAtTransform(speakers[initialString[correctInputs - 1]].name, transform);
            }
            else
            {
                audio.PlaySoundAtTransform(speakers[initialString[correctInputs]].name, transform);
            }
        }
    }

    void stageGenerator()
    {
        Debug.LogFormat("[Forget Our Voices #{0}]: -------------------------------------------------------", moduleId);
        int speaker = UnityEngine.Random.Range(0, tableA.Length);
        int digit = UnityEngine.Random.Range(0, 10);
        initialString[currentStage] = 10 * speaker + digit;
        Debug.LogFormat("[Forget Our Voices #{0}]: In stage {1}, {2} spoke the digit {3}.", moduleId, currentStage + 1, tableA[speaker], digit);
        int modifier = 0;
        if (currentStage == 0)
        {
            var sn = bomb.GetSerialNumber();
            modifier = 10 * (sn[2] - '0') + sn[5] - '0';
        }
        else
        {
            if (prevSpeaker > speaker)
            {
                if (prevSpeaker / 5 == speaker / 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is west.", moduleId);
                    modifier = 10 * (prevMod / 10) + ((((prevMod % 10 - 1) % 10) + 10) % 10);
                }
                else if (prevSpeaker % 5 == speaker % 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is north.", moduleId);
                    modifier = 10 * ((((prevMod / 10 - 1) % 10) + 10) % 10) + prevMod % 10;
                }
                else if (prevSpeaker % 5 > speaker % 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is northwest.", moduleId);
                    modifier = 10 * ((((prevMod / 10 - 1) % 10) + 10) % 10) +((((prevMod % 10 - 1) % 10) + 10) % 10);
                }
                else if (prevSpeaker % 5 < speaker % 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is northeast.", moduleId);
                    modifier = 10 * ((((prevMod / 10 - 1) % 10) + 10) % 10) + (prevMod % 10 + 1) % 10;
                }
            }
            else if (prevSpeaker < speaker)
            {
                if (prevSpeaker / 5 == speaker / 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is east.", moduleId);
                    modifier = 10 * (prevMod / 10) + (prevMod % 10 + 1) % 10;
                }
                else if (prevSpeaker % 5 == speaker % 5)
                {                    
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is south.", moduleId);
                    modifier = 10 * ((prevMod / 10 + 1) % 10) + prevMod % 10;
                }
                else if (prevSpeaker % 5 > speaker % 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is southwest.", moduleId);
                    modifier = 10 * ((prevMod / 10 + 1) % 10) + ((((prevMod % 10 - 1) % 10) + 10) % 10);
                }
                else if (prevSpeaker % 5 < speaker % 5)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: The correct direction is southeast.", moduleId);
                    modifier = 10 * ((prevMod / 10 + 1) % 10) + (prevMod % 10 + 1) % 10;
                }
            }
            else
            {
                Debug.LogFormat("[Forget Our Voices #{0}]: There is no correct direction, just stay in place.", moduleId);
                modifier = prevMod;
            }      
        }
        prevMod = modifier;
        prevSpeaker = speaker;
        Debug.LogFormat("[Forget Our Voices #{0}]: The cell you should get in Table B is in row {1}. column {2}, getting the number {3}.", moduleId, modifier / 10, modifier % 10, tableB[modifier]);
        positions[currentStage] = modifier;
        int finalDigit = (((tableB[modifier] / 10 + 1) * (digit + 1)) % 11 + tableB[modifier] - 1) % 10;
        finalString = finalString + finalDigit.ToString();
        Debug.LogFormat("[Forget Our Voices #{0}]: The correct digit for this stage is {1}.", moduleId, finalString[currentStage]);
    }

    void buttonHandler(int k)
    {
        if (!moduleSolved)
        {
            if (inputMode)
            {
                if (int.Parse(finalString[correctInputs].ToString()) == k)
                {
                    Debug.LogFormat("[Forget Our Voices #{0}]: Button {1} pressed and it's correct for stage #{2}", moduleId, k, correctInputs + 1);
                    correctInputs++;
                    ShowCurrentInput();
                    if (correctInputs < 99)
                    {
                        if (correctInputs < 9)
                        {
                            stageCount.text = "0" + (correctInputs + 1).ToString();
                        }
                        else
                        {
                            stageCount.text = (correctInputs + 1).ToString();
                        }
                    }
                    else
                    {
                        stageCount.text = ((correctInputs + 1) % 100).ToString("00");
                    }
                    recovery = false;
                    prevVoice = false;
                    if (correctInputs >= totalStages)
                    {
                        module.HandlePass();
                        moduleSolved = true;
                        stageCount.text = "GG";
                        int yay = UnityEngine.Random.Range(0, solveSounds.Length);
                        audio.PlaySoundAtTransform(solveSounds[yay].name, transform);
                        Debug.LogFormat("[Forget Our Voices #{0}]: All inputs submitted, module solved.", moduleId);
                    }
                    else
                    {
                        int rndSpeak = UnityEngine.Random.Range(0, 16);
                        if (audioRef != null) { audioRef.StopSound(); audioRef = null; }
                        audioRef = audio.HandlePlaySoundAtTransformWithRef(speakers[10 * rndSpeak + k].name, transform, false);
                        
                    }
                }
                else
                {
                    module.HandleStrike();
                    int lmao = UnityEngine.Random.Range(0, strikeSounds.Length);
                    audio.PlaySoundAtTransform(strikeSounds[lmao].name, transform);
                    Debug.LogFormat("[Forget Our Voices #{0}]: Strike! Wrong input. The correct input is {1}.", moduleId, finalString[correctInputs]);
                    recovery = true;
                    prevVoice = true;
                    if (positions[correctInputs] < 10)
                    {
                        stageCount.text = "0" + positions[correctInputs].ToString();
                    }
                    else
                    {
                        stageCount.text = positions[correctInputs].ToString();
                    }
                }
            }
            else
            {
                module.HandleStrike();
                Debug.LogFormat("[Forget Our Voices #{0}]: Strike! The module isn't in input mode yet.", moduleId);
            }
        }
        
    }

    void recoveryHandler()
    {
        if (!moduleSolved && inputMode && recovery)
        {
            smallScreen.AddInteractionPunch(0.3f);
            prevVoice = !prevVoice;
        }
    }

    private int offsetStageCnt = 0;
    void ShowCurrentInput()
    {
        //
        // Referenced Display:
        // ### ### ### ###\n
        // ### ### ### ###
        //
        
        if (correctInputs - offsetStageCnt > 24)
        {
            offsetStageCnt += 12;
        }
        string prev = finalString.Substring(offsetStageCnt, Mathf.Min(finalString.Length - offsetStageCnt, 24));
        string toInputDisplay = "";
        for (int x = 0; x < Mathf.Min(finalString.Length - offsetStageCnt, 24); x++)
        {

            if (offsetStageCnt + x < correctInputs)
            {
                toInputDisplay += prev.Substring(x, 1);
            }
            else
            {
                toInputDisplay += "-";
            }
            if ((x + 1) % 12 == 0)
            {
                toInputDisplay += "\n";
            }
            else if ((x + 1) % 3 == 0)
            {
                toInputDisplay += " ";
            }
        }
        input.text = toInputDisplay;
    }

    void advanceStage()
    {
        stageGenerator();
        if (currentStage < 99)
        {
            if (currentStage < 9)
            {
                stageCount.text = "0" + (currentStage + 1).ToString();
            }
            else
            {
                stageCount.text = (currentStage + 1).ToString();
            }
        } 
        else
        {
            stageCount.text = ((currentStage + 1) % 100).ToString("00");
        }
    }

    void Update() //Runs every frame.
    {
        if (moduleActivated)
        {
            int solvecount = bomb.GetSolvedModuleNames().Where(a => !ignoredModules.Contains(a)).ToList().Count;
            if (solvecount > currentStage)
            {
                currentStage++;
                if (currentStage >= totalStages)
                {
                    inputTime();
                }
                else
                {
                    advanceStage();
                    if (TwitchPlaysActive)
                    {
                        bigScreen.OnInteract();
                    }
                }
            }
        }
    }

    //Twitch Plays
#pragma warning disable 414
    private const string TwitchHelpMessage = @"!{0} play to press the big button, !{0} type 69420 to type in 69420 at the end, !{0} stage to press the stage display in a stage recovery";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*play\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            bigScreen.OnInteract();
            yield return null;
        }
        if (Regex.IsMatch(command, @"^\s*stage\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (!recovery)
            {
                yield return "sendtochaterror The module did not enter stage recovery, command ignored.";
                yield break;
            }
            smallScreen.OnInteract();
            yield return null;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*type\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if (!inputMode)
            {
                yield return "sendtochaterror The module did not enter input mode yet (unless you're eager to strike), command ignored.";
                yield break;
            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Go on...type something...";
                yield break;
            }
            else
            {
                for (int i = 0; i < parameters[1].Length; i++)
                {
                    yield return new WaitForSeconds(0.1f);
                    for (int j = 0; j < 10; j++)
                    {
                        string comparer = parameters[1].ElementAt(i) + "";
                        if (comparer.Equals(j.ToString()))
                        {
                            buttons[j].OnInteract();
                        }
                    }
                }
            }
        }
    }
    IEnumerator TwitchHandleForcedSolve()
    {
        while (!inputMode)
        {
            yield return true;
        }
        while (!moduleSolved)
        {
            buttons[finalString[correctInputs] - '0'].OnInteract();
            yield return null;
        }
    }

}
