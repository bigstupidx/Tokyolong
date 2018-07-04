﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BillElt : MonoBehaviour {

	public Text txtName;
	public Text txtVal;
	public Text txtPrice;

	EMenuDetail eType;
	int value = 0;
	int price = 0;

	public void SetInfo(EMenuDetail eType)
	{
		SetInfo (eType, 1);
	}

	public void SetInfo(EMenuDetail eType, int value)
	{
		this.eType = eType;
		txtName.text = Info.MenuName (eType);

		this.value = value;
		txtVal.text = value.ToString ();

		_RefreshPrice ();
	}

	public void OnChangeValue(bool isAdd)
	{
		value = isAdd ? value + 1 : value - 1;
		if (value < 1)
			value = 1;

		txtVal.text = value.ToString ();
		_RefreshPrice ();
	}

	void _RefreshPrice()
	{
		price = Info.MenuPrice (eType) * value;
		txtPrice.text = Info.MakeMoneyString (price);

		if (SceneManager.GetActiveScene ().name != "Admin")
			((PageOrder)PageBase.Instance).bill.CalcTotalPrice ();
	}

	public void OnDelete()
	{
		((PageOrder)PageBase.Instance).bill.RemoveElt (eType);
	}

	public int GetCount() {	return value; }
	public int GetPrice() { return price; }
	public EMenuDetail MenuDetailType() { return eType; }
}