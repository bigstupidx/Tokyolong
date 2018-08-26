﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CrashCat;

public class Database : MonoBehaviour {

    public List<Character> listChar;
    public List<ThemeData> listTheme;

    void Awake()
    {
        CharacterDatabase.SetCharacter(listChar);
        ThemeDatabase.SetThemeData(listTheme);
    }
}
