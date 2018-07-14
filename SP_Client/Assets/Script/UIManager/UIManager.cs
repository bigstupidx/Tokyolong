﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum eUI
{
	eChat,
	eTableSetting,
	eBillDetail,
	eBillSending,
	eMusicRequest,
	eWaiting,

	eNone = 100,
}

public class UIManager : SingletonMonobehaviour<UIManager> {

	[System.Serializable]
	public class UI
	{
		public eUI ui;
		public GameObject obj;
	}

	public GameObject objShadow;
	public List<UI> listUI;

    public UIAlarm uiAlarm;

	eUI curUI = eUI.eNone;
	Dictionary<eUI, GameObject> dicObject = new Dictionary<eUI, GameObject> ();

	void Awake () 
	{
		collect();
		Hide_All ();

        DontDestroyOnLoad(this);
	}

	void collect()
	{
		for (int i = 0; i < listUI.Count; i++) 
			dicObject.Add (listUI [i].ui, listUI [i].obj);
	}		

    public void Show(int pageIdx) { Show((eUI)pageIdx); }
	public GameObject Show(eUI page)
	{
		if (page != eUI.eWaiting) {
			curUI = page;
			objShadow.SetActive (true);
		}

		dicObject [page].SetActive (true);
		return dicObject [page];
	}

    public void Hide(int pageIdx) { Hide((eUI)pageIdx); }
	public void Hide(eUI page)
	{
		if (page != eUI.eWaiting) {
			curUI = eUI.eNone;
			objShadow.SetActive (false);
		}
		
		dicObject [page].SetActive (false);
	}
		
	public void Hide_All()
	{
		objShadow.SetActive (false);
		foreach (KeyValuePair<eUI, GameObject> pair in dicObject)
			pair.Value.SetActive (false);
	}

	public bool IsActive(eUI page)
	{
		return dicObject [page].activeSelf;
	}

	public GameObject GetUI(eUI page)
	{
		return dicObject [page];
	}

	public GameObject GetCurUI()
	{
		if (curUI == eUI.eNone)
			return null;

		return dicObject [curUI];
	}

    public void ShowChatAlarm()
    {
        uiAlarm.ShowAlarm("채팅이\n도착 하였습니다", _ShowChat);
    }
    public void ShowOrderAlarm()
    {
        uiAlarm.ShowAlarm("주문이\n접수 되었습니다", _ShowBillDetail);
    }

    void _ShowChat()
    {
        uiAlarm.HideAlarm();

        if (curUI != eUI.eNone && curUI != eUI.eChat)
            Hide(curUI);

        UIChat uiChat = Show(eUI.eChat).GetComponent<UIChat>();
        uiChat.ShowChatTable();
    }

    void _ShowBillDetail()
    {
        uiAlarm.HideAlarm();

        if (curUI != eUI.eNone && curUI != eUI.eBillDetail)
            Hide(curUI);

        NetworkManager.Instance.Order_Detail_REQ();
    }
}