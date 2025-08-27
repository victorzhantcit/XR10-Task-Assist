using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MRTK.Extensions;
using System;
using TMPro;

public class UserLoginListItem : VirtualListItem<string>
{
    [SerializeField] private TMP_Text _username;

    private Action _onClick = null;

    public override void SetContent(string username, int _ = -1, bool __ = true)
    {
        _username.text = username;
    }

    public void SetOnClicked(Action action) => _onClick = action;

    public void OnClicked() => _onClick?.Invoke();
}
