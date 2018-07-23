﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using LitJson;

public class AdminBill : MonoBehaviour {
    
	public GameObject prefab;
	public GameObject objEmpty;
	public RectTransform rtScroll; 
    public Text totalPrice;
    public Text textDiscountPrice;
    public Text textCalcPrice;

    List<BillConfirmElt> listElt = new List<BillConfirmElt>();

    int billTotalPrice = 0;
    int discountPrice = 0;
    public int BillTotalPrice { get { return Mathf.Max(0, (billTotalPrice - discountPrice)); } }

	public void CalcTotalPrice()
	{
		int total = 0;
		for (int i = 0; i < listElt.Count; i++)
			total += listElt [i].GetPrice ();

        billTotalPrice = total;
		totalPrice.text = Info.MakeMoneyString (total);
        textDiscountPrice.text = "-"+Info.MakeMoneyString(discountPrice);
        textCalcPrice.text = Info.MakeMoneyString(BillTotalPrice);
	}

	public void RemoveElt(EMenuDetail eType)
	{
		int findIdx = -1;
		for (int i = 0; i < listElt.Count; i++) {
			if (listElt [i].MenuDetailType () != eType)
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
				Destroy (child.gameObject);
			break;
		}

		CalcTotalPrice ();
	}

	void _Clear()
	{
		for (int i = 0; i < rtScroll.childCount; i++) {
			Transform child = rtScroll.GetChild (i);
			if (child)
				Destroy (child.gameObject);
		}

		listElt.Clear ();

		if (objEmpty != null && objEmpty.activeSelf == false)
			objEmpty.SetActive (true);

        discountPrice = 0;

		CalcTotalPrice ();
	}

    public void CopyBill(List<KeyValuePair<EMenuDetail, int>> list, List<short> listDiscount)
	{
		_Clear ();

		for (int i = 0; i < list.Count; i++) {
			GameObject obj = Instantiate (prefab) as GameObject;
			obj.SetActive (true);

			Transform tr = obj.transform;
			tr.SetParent (rtScroll);
			tr.InitTransform ();

            BillConfirmElt elt = obj.GetComponent<BillConfirmElt> ();
			elt.SetInfo (list [i].Key, list [i].Value);
			listElt.Add (elt);
		}

        for (int i = 0; i < listDiscount.Count; i++) {
            discountPrice += Info.GetDiscountPrice(listDiscount[i]);
        }

        if (objEmpty != null)
            objEmpty.SetActive(list.Count <= 0);

		CalcTotalPrice ();
	}
}