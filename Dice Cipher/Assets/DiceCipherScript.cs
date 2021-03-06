using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class DiceCipherScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable roll;
    public KMSelectable submit;

    public GameObject[] Die1Pips, Die2Pips, Die3Pips, Die4Pips;
    private GameObject[][] allPips = new GameObject[4][];
    public GameObject[] sidePips;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    int[][] pipPatterns = new int[][] { new[] { 3 }, new[] { 0 }, new[] { 1 }, new[] { 2 }, new[] { 4 }, new[] { 7 }, new[] { 6 }, new[] { 5 }, new[] { 0, 7 }, new[] { 0, 7 }, new[] { 1, 6 }, new[] { 2, 5 }, new[] { 3, 4 }, new[] { 0, 5 }, new[] { 0, 2 }, new[] { 2, 7 }, new[] { 5, 7 }, new[] { 0, 5, 7 }, new[] { 1, 3, 6 }, new[] { 0, 2, 5 }, new[] { 1, 3, 4 }, new[] { 1, 3, 4 }, new[] { 0, 2, 7 }, new[] { 1, 4, 6 }, new[] { 2, 5, 7 }, new[] { 3, 4, 6 } };
    string[] groups = new string[] { "ABIJKRS", "CDLMTUV", "EFNOWX", "GHPQYZ" };
    string[] first12;
    int rollPointer;
    string[] PEOPLEIDONTREALLYLIKE = new string[] { "DESKTOP-RGTP319", "DESKTOP-4HOQP30", "laptopInMyBackPocket" };

    int chosenGroup;
    int submitGroup;
    List<string> validDisplays;
    List<string> validSubmits;

    string displayed;

    void Awake ()
    {
        moduleId = moduleIdCounter++;
        roll.OnInteract += delegate () { RollDice(); return false; };
        submit.OnInteract += delegate () { Submit(); return false; };
        allPips = new[] { Die1Pips, Die2Pips, Die3Pips, Die4Pips };
    }

    void Start ()
    {
        chosenGroup = UnityEngine.Random.Range(0, 4);
        submitGroup = (Bomb.GetBatteryHolderCount() + Bomb.GetIndicators().Count()) % 4;
        validDisplays = WordList.phrases.Where(x => GetGroups(x).Contains(chosenGroup)).ToList();
        validSubmits = validDisplays.Where(x => GetGroup(x[chosenGroup]) == submitGroup).ToList();

        GetFirst12();
        Debug.LogFormat("<Dice Cipher #{0}> The first 12 words are {1}.", moduleId, first12.Join());
        Debug.LogFormat("<Dice Cipher #{0}> The groups of the first 5 words are {1}.", moduleId, first12.Take(5).Select(str => GetGroups(str).Select(grp => grp + 1).Join("")).Join());
        Debug.LogFormat("[Dice Cipher #{0}] The common group on the module is group {1}. This is also the index of the target die.", moduleId, chosenGroup + 1);
        Debug.LogFormat("[Dice Cipher #{0}] The target die must be in group {1}.", moduleId, submitGroup + 1);
        RollDice(true); 
    }

    void GetFirst12()
    {
        string[] groupDeterminers;
        string[] solutionDeterminers;
        int[] groupsWhichAppearInAll;
        do
        {
            groupDeterminers = validDisplays.Shuffle().Take(5).ToArray();
            groupsWhichAppearInAll = Enumerable.Range(0, 4).Where(group => groupDeterminers.All(str => GetGroups(str).Contains(group))).ToArray();
        } while (groupsWhichAppearInAll.Count() != 1);
        do
        {
            solutionDeterminers = validDisplays.Shuffle().Take(7).ToArray();
        } while (!solutionDeterminers.Any(str => validSubmits.Contains(str)));

        first12 = groupDeterminers.Concat(solutionDeterminers).ToArray();
    }

    void RollDice(bool start = false)
    {
        if (!start)
        {
            roll.AddInteractionPunch(1);
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, roll.transform);
            Audio.PlaySoundAtTransform(
                (UnityEngine.Random.Range(0, 200) == 0 || PEOPLEIDONTREALLYLIKE.Contains(Environment.MachineName)) ?
                "rolldadice" : "Roll", transform);
        }
        if (rollPointer < 12)
            displayed = first12[rollPointer++];
        else displayed = validDisplays.PickRandom();
        Debug.LogFormat("[Dice Cipher #{0}] The dice have been rolled, the displayed word is {1}.", moduleId, displayed);

        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 8; j++)
                allPips[i][j].SetActive(false); //turns off all the pips
            foreach (int pip in pipPatterns[displayed[i] - 'A']) 
                allPips[i][pip].SetActive(true); //turns on the pips which are in the correct 
        }
        bool[] sides = new bool[48];
        bool[][] valids = new bool[][] { new[] { true, false, false }, new[] { false, true, false }, new[] { false, false, false }, new[] { true, false, true } }; //These patterns are able to appear in the top row of a dice.
        for (int i = 0; i < 12; i++)
            Array.Copy(valids.PickRandom(), 0, sides, 3*i, 3);
        for (int i = 0; i < 48; i++)
            sidePips[i].SetActive(sides[i]);
    }
    void Submit()
    {
        submit.AddInteractionPunch(1);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, submit.transform);
        if (moduleSolved) return;
        else if (validSubmits.Contains(displayed))
        {
            moduleSolved = true;
            GetComponent<KMBombModule>().HandlePass();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
            Debug.LogFormat("[Dice Cipher #{0}] {1} has been submitted, which is correct. Module solved.", moduleId, displayed);
        }
        else
        {
            GetComponent<KMBombModule>().HandleStrike();
            Debug.LogFormat("[Dice Cipher #{0}] {1} has been submitted, which is incorrect. Strike incurred.", moduleId, displayed);
        }
    }

    int GetGroup(char input)
    {
        for (int i = 0; i < 4; i++)
            if (groups[i].Contains(input))
                return i;
        throw new ArgumentOutOfRangeException("input");
    }
    IEnumerable<int> GetGroups(string input)
    {
        foreach (char letter in input)
            for (int i = 0; i < 4; i++)
                if (groups[i].Contains(letter))
                    yield return i;
    }


    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use [!{0} roll] to roll the dice. Use [!{0} submit] to press the submit button.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        if (command == "ROLL")
        {
            yield return null;
            roll.OnInteract();
        }
        if (command == "SUBMIT")
        {
            yield return null;
            submit.OnInteract();
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!validSubmits.Contains(displayed))
        {
            roll.OnInteract();
            yield return new WaitForSeconds(0.15f);
        }
        submit.OnInteract();
    }
}
