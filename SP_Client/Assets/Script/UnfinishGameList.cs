﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LitJson;

public class UnfinishGameList : MonoBehaviour {

	public RectTransform rtScroll;
	public ScrollRect scrollRect;
	public GameObject objPrefab;

	List<GameElt> listElt = new List<GameElt>();

	public void SetInfo(string packing)
	{
		listElt.Clear ();
		for (int i = 0; i < rtScroll.childCount; i++) {
			Transform child = rtScroll.GetChild (i);
			if (child)
				Destroy (child.gameObject);
		}

		JsonData json = JsonMapper.ToObject (packing);
		for (int i = 0; i < json.Count; i++) {
			string parse_id = json [i] ["id"].ToString ();
			string parse_type = json [i] ["type"].ToString ();
			string parse_kind = json [i] ["kind"].ToString ();
			string parse_discount = json [i] ["discount"].ToString ();

			int id = int.Parse(parse_id);
			EGameType eType = (EGameType)int.Parse (parse_type);
			int curGame = int.Parse (parse_kind);
			EDiscount eDis = (EDiscount)int.Parse (parse_discount);
			_SetUnfinishGame (id, eType, curGame, eDis);
		}			

		scrollRect.horizontalNormalizedPosition = 0f;
	}

	void _SetUnfinishGame(int id, EGameType eType, int game, EDiscount eDis)
	{
		GameObject obj = Instantiate (objPrefab) as GameObject;
		obj.SetActive (true);

		Transform tr = obj.transform;
		tr.SetParent (rtScroll);
		tr.InitTransform ();

		GameElt elt = obj.GetComponent<GameElt> ();
		elt.SetInfo (id, eType, game, eDis);
	}

	public void RemoveUnfinish(int id)
	{
		int findIdx = -1;
		for (int i = 0; i < listElt.Count; i++) {
			if (listElt [i].GetID () != id)
				continue;

			findIdx = i;
			listElt.RemoveAt (i);
			break;
		}

		if (findIdx == -1)
			return;

		for (int i = 0; i < rtScroll.childCount; i++) {
			if (i != findIdx)
				continue;
			
			Transform child = rtScroll.GetChild (i);
			if (child)
				DestroyImmediate (child.gameObject);
			break;
		}
	}

	public void OnClose() { gameObject.SetActive (false); }
}