﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FreeNet;
using SP_Server;

public partial class NetworkManager : SingletonMonobehaviour<NetworkManager> 
{
	void send(CPacket msg)
	{
		if (UIManager.Instance.IsActive (eUI.eWaiting) == false)
			UIManager.Instance.Show (eUI.eWaiting);

		this.sending_queue.Enqueue(msg);
	}

	public void Login_REQ(string table_no)
	{
		CPacket msg = CPacket.create ((short)PROTOCOL.LOGIN_REQ);
		msg.push (table_no);

		send (msg);
	}

	public void EnterCostomer_REQ(byte howMany, byte type)
	{
        CPacket msg = CPacket.create((short)PROTOCOL.ENTER_CUSTOMER_REQ);
        msg.push(howMany);
        msg.push(type);

        send(msg);
	}
}