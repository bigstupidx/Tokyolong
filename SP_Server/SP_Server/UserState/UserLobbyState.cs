﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using FreeNet;
using LitJson;

namespace SP_Server.UserState
{
    class UserLobbyState : IUserState
    {
        User owner;

        public UserLobbyState(User owner)
        {
            this.owner = owner;
        }

        void IUserState.on_message(FreeNet.CPacket msg)
        {
            StackFrame stackFrame = new StackFrame(1, true);

            try
            {
                PROTOCOL protocol = (PROTOCOL)msg.pop_protocol_id();

                owner.mainFrm.BeginInvoke(owner.mainFrm.WriteLogInstance,
                        new object[] { "[table Num : " + owner.tableNum.ToString() + "] protocol id " + protocol,
                        stackFrame.GetMethod().Name, stackFrame.GetFileName(),
                        stackFrame.GetFileLineNumber().ToString() });

                CPacket send_msg = null;
                CPacket other_msg = null;
                byte tableNo = 0;
                switch (protocol)
                {
                    case PROTOCOL.LOGIN_REQ:
                        bool existUser = false;
                        int tableNum = 0;

                        string tableNoStr = msg.pop_string();
                        if (tableNoStr == "admin")
                        {
                            tableNum = 10000;
                            Frm.SetAdminUser(owner);
                        }
                        else
                        {
                            if (int.TryParse(tableNoStr, out tableNum) == false)
                            {
                                send_msg = CPacket.create((short)PROTOCOL.FAILED_NOT_NUMBER);
                                break;
                            }

                            tableNum = int.Parse(tableNoStr);

                            UserInfo getUserInfo = null;
                            if (owner.mainFrm.AddUserInfo(tableNum, ref getUserInfo) == false)
                            {
                                owner.info = getUserInfo;
                                existUser = true;
                            }                                

                            // Admin Send packet
                            if(Frm.GetAdminUser() != null)
                            {
                                other_msg = CPacket.create((short)PROTOCOL.LOGIN_NOT);
                                other_msg.push(tableNum);
                                Frm.GetAdminUser().send(other_msg);
                            }                            
                        }

                        owner.tableNum = tableNum;                        

                        send_msg = CPacket.create((short)PROTOCOL.LOGIN_ACK);
                        send_msg.push(tableNoStr);

                        if (tableNoStr == "admin")
                        {
                            List<int> listUserTableNo = new List<int>();

                            foreach (UserInfo e in owner.mainFrm.dictUserInfo.Values)
                            {
                                UserInfo userInfo = e;
                                if (userInfo.IsAdmin())
                                    continue;

                                listUserTableNo.Add(userInfo.tableNum);
                            }

                            List<RequestOrder> listReqOrder = owner.mainFrm.listRequestOrder;
                            List<RequestMusicInfo> listReqMusic = owner.mainFrm.listReqMusicInfo;

                            send_msg.push((JsonMapper.ToJson(listUserTableNo)).ToString());
                            send_msg.push((JsonMapper.ToJson(listReqOrder)).ToString());
                            send_msg.push((JsonMapper.ToJson(listReqMusic)).ToString());
                        }
                        else
                        {                            
                            send_msg.push(owner.mainFrm.GetGameCount(tableNum));                            
                            send_msg.push(owner.info.surpriseCnt);

                            send_msg.push(existUser ? 1 : 0);

                            // 유저 정보가 있는경우 추가 패킷
                            if (existUser)
                            {
                                send_msg.push(owner.info.peopleCnt);
                                send_msg.push(owner.info.customerType);

                                List<UserInfo> list = new List<UserInfo>();
                                list.Add(owner.info);
                                // 접속된 유저들에게 현재 접속 유저 정보 전송
                                for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                                {
                                    User user = owner.mainFrm.ListUser[i];
                                    if (user.tableNum == 10000 ||
                                        user.tableNum <= 0 ||
                                        user.info == null)
                                        continue;

                                    list.Add(user.info);

                                    if (user.info.tableNum == owner.info.tableNum)
                                        continue;

                                    other_msg = CPacket.create((short)PROTOCOL.ENTER_CUSTOMER_NOT);
                                    JsonData loginUser = JsonMapper.ToJson(owner.info);
                                    other_msg.push(loginUser.ToString());
                                    user.send(other_msg);
                                }

                                JsonData json = JsonMapper.ToJson(list);
                                send_msg.push(json.ToString());
                            }
                        }                           
                        break;
                    case PROTOCOL.LOGOUT_REQ:
                        tableNo = msg.pop_byte();

                        // 해당 테이블 로그인 화면으로 보내기
                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User user = owner.mainFrm.ListUser[i];
                            if (user.IsAdmin)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.LOGOUT_NOT);
                            other_msg.push(tableNo);
                            user.send(other_msg);
                        }
                        
                        owner.mainFrm.RemoveUserData((int)tableNo);

                        send_msg = CPacket.create((short)PROTOCOL.LOGOUT_ACK);
                        send_msg.push(tableNo);

                        break;
                    case PROTOCOL.ENTER_CUSTOMER_REQ:
                        tableNo = msg.pop_byte();
                        byte peopleCnt = msg.pop_byte();
                        byte customerType = msg.pop_byte();

                        // 유저 리스트에 정보 입력하기                        
                        owner.info = new UserInfo(owner.tableNum, peopleCnt, customerType);
                        owner.mainFrm.SetUserInfo(tableNo, owner.info);

                        List<UserInfo> listUserInfo = new List<UserInfo>();
                        listUserInfo.Add(owner.info);
                        // 접속된 유저들에게 현재 접속 유저 정보 전송
                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User user = owner.mainFrm.ListUser[i];
                            if (user.tableNum == 10000 || 
                                user.tableNum <= 0 || 
                                user.info == null)
                                continue;

                            listUserInfo.Add(user.info);

                            if (user.info.tableNum == owner.info.tableNum)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.ENTER_CUSTOMER_NOT);
                            JsonData loginUser = JsonMapper.ToJson(owner.info);
                            other_msg.push(loginUser.ToString());
                            user.send(other_msg);
                        }

                        send_msg = CPacket.create((short)PROTOCOL.ENTER_CUSTOMER_ACK);
                        send_msg.push(peopleCnt);
                        send_msg.push(customerType);

                        JsonData listUerJson = JsonMapper.ToJson(listUserInfo);
                        send_msg.push(listUerJson.ToString());
                        break;
                    case PROTOCOL.WAITER_CALL_REQ:
                        tableNo = msg.pop_byte();

                        // Admin Send packet
                        if (Frm.GetAdminUser() != null)
                        {
                            other_msg = CPacket.create((short)PROTOCOL.WAITER_CALL_NOT);
                            other_msg.push(tableNo);
                            Frm.GetAdminUser().send(other_msg);
                        }

                        send_msg = CPacket.create((short)PROTOCOL.WAITER_CALL_ACK);                        
                        break;
                    case PROTOCOL.ORDER_REQ:
                        tableNo = msg.pop_byte();
                        string order = msg.pop_string();
                        int orderCnt = msg.pop_int32();

                        owner.info.AddGameCount(orderCnt);
                        owner.mainFrm.RefreshGameCount(tableNo, owner.info.GetGameCount());

                        ++owner.mainFrm.orderID;

                        RequestOrder reqOrder = new RequestOrder((byte)ERequestOrerType.eOrder, owner.mainFrm.orderID, tableNo, order);
                        owner.mainFrm.SetRequestOrder(reqOrder);

                        // Admin Send packet
                        if (Frm.GetAdminUser() != null)
                        {
                            other_msg = CPacket.create((short)PROTOCOL.ORDER_NOT);
                            other_msg.push(reqOrder.type);
                            other_msg.push(reqOrder.id);
                            other_msg.push((byte)reqOrder.tableNo);
                            other_msg.push(reqOrder.packing);

                            Frm.GetAdminUser().send(other_msg);
                        }

                        send_msg = CPacket.create((short)PROTOCOL.ORDER_ACK);
                        send_msg.push(orderCnt);
                        break;
                    case PROTOCOL.CHAT_REQ:
                        tableNo = msg.pop_byte();
                        byte otherTableNo = msg.pop_byte();
                        string chat = msg.pop_string();

                        string tt = DateTime.Now.ToString("tt");
                        string hh = DateTime.Now.ToString("hh");
                        string mm = DateTime.Now.ToString("mm");
                        string makeTime = tt + "/" + hh + "/" + mm;

                        // 상대방 유저에게 채팅 보내기
                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User other = owner.mainFrm.ListUser[i];
                            if (other.tableNum != otherTableNo)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.CHAT_NOT);
                            other_msg.push(tableNo);                            
                            other_msg.push(makeTime);
                            other_msg.push(chat);
                            other.send(other_msg);
                            break;
                        }

                        send_msg = CPacket.create((short)PROTOCOL.CHAT_ACK);
                        send_msg.push(otherTableNo);
                        send_msg.push(makeTime);
                        send_msg.push(chat);

                        break;
                    case PROTOCOL.ORDER_DETAIL_REQ:
                        tableNo = msg.pop_byte();

                        List<SendMenu> listSendMenu = owner.mainFrm.GetOrder((int)tableNo);
                        JsonData listSendMenuJson = JsonMapper.ToJson(listSendMenu);

                        send_msg = CPacket.create((short)PROTOCOL.ORDER_DETAIL_ACK);
                        send_msg.push(listSendMenuJson.ToString());
                        send_msg.push(owner.mainFrm.GetDiscount((int)tableNo));
                        break;
                    case PROTOCOL.GAME_DISCOUNT_REQ:

                        tableNo = msg.pop_byte();
                        short discount = msg.pop_int16();
                        owner.mainFrm.SetDiscount(tableNo, discount);

                        send_msg = CPacket.create((short)PROTOCOL.GAME_DISCOUNT_ACK);

                        break;
                    case PROTOCOL.REQUEST_MUSIC_LIST_REQ:

                        JsonData listRequestMusicJson = JsonMapper.ToJson(owner.mainFrm.listReqMusicInfo);

                        send_msg = CPacket.create((short)PROTOCOL.REQUEST_MUSIC_LIST_ACK);
                        send_msg.push(listRequestMusicJson.ToString());
                        break;
                    case PROTOCOL.REQUEST_MUSIC_REQ:

                        byte reqTableNo = msg.pop_byte();
                        string reqTitle = msg.pop_string();
                        string reqSinger = msg.pop_string();

                        bool requestMusicAdd = owner.mainFrm.listReqMusicInfo.Count < Frm.REQUEST_MUSIC_MAX_COUNT;
                        send_msg = CPacket.create((short)PROTOCOL.REQUEST_MUSIC_ACK);
                        send_msg.push(Convert.ToByte(requestMusicAdd));
                        if (requestMusicAdd)
                        {
                            RequestMusicInfo reqMusicInfo = owner.mainFrm.AddRequestMusic(reqTableNo, reqTitle, reqSinger);
                            JsonData reqMusicJson = JsonMapper.ToJson(reqMusicInfo);

                            // 관리자에게 전달
                            if (Frm.GetAdminUser() != null)
                            {
                                other_msg = CPacket.create((short)PROTOCOL.REQUEST_MUSIC_NOT);
                                other_msg.push(Convert.ToByte(requestMusicAdd));
                                other_msg.push(reqMusicJson.ToString());
                                Frm.GetAdminUser().send(other_msg);
                            }

                            send_msg.push(reqMusicJson.ToString());
                        }

                        break;
                    case PROTOCOL.REQUEST_MUSIC_REMOVE_REQ:

                        int removeReqMusicID = msg.pop_int32();
                        owner.mainFrm.RemoveRequestMusicInfo(removeReqMusicID);

                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User other = owner.mainFrm.ListUser[i];
                            if (other.tableNum == 10000 || other.tableNum <= 0 || other.info == null)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.REQUEST_MUSIC_REMOVE_NOT);
                            other_msg.push(removeReqMusicID);
                            other.send(other_msg);
                            break;
                        }

                        send_msg = CPacket.create((short)PROTOCOL.REQUEST_MUSIC_REMOVE_ACK);
                        send_msg.push(removeReqMusicID);

                        break;
                    case PROTOCOL.ORDER_CONFIRM_REQ:
                        byte reqType = msg.pop_byte();
                        int reqOrderId = msg.pop_int32();
                        byte reqOrderTableNo = msg.pop_byte();
                        string reqOrderPacking = msg.pop_string();

                        switch ((ERequestOrerType)reqType)
                        {
                            case ERequestOrerType.eOrder:       owner.mainFrm.SetOrder((int)reqOrderTableNo, reqOrderPacking);      break;
                        }

                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User other = owner.mainFrm.ListUser[i];
                            if (other.tableNum != reqOrderTableNo)
                                continue;                                                                                    

                            other_msg = CPacket.create((short)PROTOCOL.ORDER_CONFIRM_NOT);
                            other_msg.push(reqType);
                            other_msg.push(other.info.surpriseCnt);
                            other.send(other_msg);
                            break;
                        }

                        owner.mainFrm.RemoveRequestOrder(reqOrderId);

                        send_msg = CPacket.create((short)PROTOCOL.ORDER_CONFIRM_ACK);
                        send_msg.push(reqOrderId);
                        break;
                    case PROTOCOL.TABLE_ORDER_CONFIRM_REQ:
                        tableNo = msg.pop_byte();
                        List<SendMenu> listTableOrder = owner.mainFrm.GetOrder((int)tableNo);
                        JsonData tableOrderJson = JsonMapper.ToJson(listTableOrder);

                        send_msg = CPacket.create((short)PROTOCOL.TABLE_ORDER_CONFIRM_ACK);
                        send_msg.push(tableNo);
                        send_msg.push(tableOrderJson.ToString());
                        send_msg.push(owner.mainFrm.GetDiscount((int)tableNo));
                        send_msg.push(owner.mainFrm.GetGameCount((int)tableNo));
                        break;
                    case PROTOCOL.TABLE_ORDER_INPUT_REQ:
                        tableNo = msg.pop_byte();
                        string inputTableOrderPacking = msg.pop_string();
                        int tableInputOrderCnt = msg.pop_int32();

                        owner.mainFrm.AddGameCount((int)tableNo, tableInputOrderCnt);
                        owner.mainFrm.RefreshGameCount(tableNo, owner.mainFrm.GetGameCount((int)tableNo));

                        JsonData inputOrder = JsonMapper.ToObject(inputTableOrderPacking);
                        for (int i = 0; i < inputOrder.Count; i++)
                        {
                            int reqSendMenu = int.Parse(inputOrder[i]["menu"].ToString());
                            int reqSendCnt = int.Parse(inputOrder[i]["cnt"].ToString());

                            owner.mainFrm.SetOrder((int)tableNo, new SendMenu(reqSendMenu, reqSendCnt));
                        }

                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User inputTargetUser = owner.mainFrm.ListUser[i];
                            if (inputTargetUser.tableNum != (int)tableNo)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.TABLE_ORDER_INPUT_NOT);
                            other_msg.push(tableInputOrderCnt);
                            inputTargetUser.send(other_msg);
                            break;
                        }

                        send_msg = CPacket.create((short)PROTOCOL.TABLE_ORDER_INPUT_ACK);
                        break;
                    case PROTOCOL.SLOT_START_REQ:
                        tableNo = msg.pop_byte();
                        owner.info.AddGameCount(-1);
                        owner.mainFrm.RefreshGameCount(tableNo, owner.info.GetGameCount());

                        short ranDiscountIdx = owner.mainFrm.GetRandomDiscountIndex();

                        send_msg = CPacket.create((short)PROTOCOL.SLOT_START_ACK);
                        send_msg.push(owner.info.GetGameCount());
                        send_msg.push(ranDiscountIdx);
                        break;                    
                    case PROTOCOL.TABLE_DISCOUNT_INPUT_REQ:
                        tableNo = msg.pop_byte();
                        int inputDiscount = msg.pop_int32();

                        owner.mainFrm.SetDiscount((int)tableNo, inputDiscount);

                        bool isSend = owner.tableNum != tableNo;
                        if (isSend)
                        {
                            for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                            {
                                User other = owner.mainFrm.ListUser[i];
                                if (other.tableNum != tableNo)
                                    continue;

                                other_msg = CPacket.create((short)PROTOCOL.TABLE_DISCOUNT_INPUT_NOT);
                                other.send(other_msg);
                                break;
                            }
                        }

                        send_msg = CPacket.create((short)PROTOCOL.TABLE_DISCOUNT_INPUT_ACK);
                        send_msg.push(Convert.ToByte(isSend));

                        break;
                    case PROTOCOL.GET_RANDOM_DISCOUNT_PROB_REQ:

                        send_msg = CPacket.create((short)PROTOCOL.GET_RANDOM_DISCOUNT_PROB_ACK);
                        for(int i = 0; i < owner.mainFrm.listDiscountProb.Count; i++)
                            send_msg.push(owner.mainFrm.listDiscountProb[i]);                        
                        break;
                    case PROTOCOL.SET_RANDOM_DISCOUNT_PROB_REQ:

                        List<float> listDiscountProb = new List<float>();
                        for (int i = 0; i < 5; i++)
                            listDiscountProb.Add(msg.pop_float());                        

                        owner.mainFrm.SetDiscountProb(listDiscountProb);

                        send_msg = CPacket.create((short)PROTOCOL.SET_RANDOM_DISCOUNT_PROB_ACK);
                        break;
                    case PROTOCOL.TABLE_PRICE_CONFIRM_REQ:
                        tableNo = msg.pop_byte();

                        int tablePrice = owner.mainFrm.GetTablePrice((int)tableNo);
                        int tableDiscount = owner.mainFrm.GetDiscount((int)tableNo);

                        send_msg = CPacket.create((short)PROTOCOL.TABLE_PRICE_CONFIRM_ACK);
                        send_msg.push(tableNo);
                        send_msg.push(tablePrice);
                        send_msg.push(tableDiscount);

                        break;                    
                    case PROTOCOL.SURPRISE_REQ:
                        tableNo = msg.pop_byte();
                        --owner.info.surpriseCnt;
                        if (owner.info.surpriseCnt < 0)
                            owner.info.surpriseCnt = 0;

                        owner.mainFrm.DataUserInfoSave();

                        int surpriseCnt = owner.info.surpriseCnt;
                        send_msg = CPacket.create((short)PROTOCOL.SURPRISE_ACK);
                        send_msg.push(surpriseCnt);
                        break;
                    case PROTOCOL.GAME_COUNT_INPUT_REQ:
                        tableNo = msg.pop_byte();
                        int gameCount = msg.pop_int32();

                        owner.mainFrm.AddGameCount((int)tableNo, gameCount);
                        owner.mainFrm.RefreshGameCount(tableNo, owner.mainFrm.GetGameCount((int)tableNo));

                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User inputTargetUser = owner.mainFrm.ListUser[i];
                            if (inputTargetUser.tableNum != (int)tableNo)
                                continue;

                            other_msg = CPacket.create((short)PROTOCOL.GAME_COUNT_INPUT_NOT);
                            other_msg.push(gameCount);
                            inputTargetUser.send(other_msg);
                            break;
                        }

                        send_msg = CPacket.create((short)PROTOCOL.GAME_COUNT_INPUT_ACK);
                        break;
                    case PROTOCOL.TABLE_MOVE_REQ:
                        tableNo = msg.pop_byte();
                        int moveTableNo = msg.pop_int32();

                        UserInfo prevUser = null;
                        foreach (UserInfo e in owner.mainFrm.dictUserInfo.Values)
                        {                            
                            if (e.IsAdmin())            continue;
                            if (e.tableNum != tableNo)  continue;

                            prevUser = e;
                            break;
                        }

                        send_msg = CPacket.create((short)PROTOCOL.FAILED);
                        if (prevUser == null)
                            break;

                        UserInfo afterUser = null;
                        if (owner.mainFrm.AddUserInfo(moveTableNo, ref afterUser))
                            break;

                        for (int i = 0; i < owner.mainFrm.ListUser.Count; i++)
                        {
                            User moveUser = owner.mainFrm.ListUser[i];
                            if (moveUser.tableNum != moveTableNo)
                                continue;

                            send_msg = CPacket.create((short)PROTOCOL.TABLE_MOVE_ACK);
                            send_msg.push(tableNo);

                            afterUser = new UserInfo();
                            afterUser.Copy(prevUser, moveTableNo);
                            owner.mainFrm.SetUserInfo(moveTableNo, afterUser);                            

                            other_msg = CPacket.create((short)PROTOCOL.TABLE_MOVE_NOT);
                            moveUser.send(other_msg);
                            break;
                        }

                        break;
                    default:
                        break;
                }

                send(send_msg);
            }
            catch (Exception e)
            {
                owner.mainFrm.BeginInvoke(owner.mainFrm.WriteLogInstance,
                        new object[] { e.ToString(),
                        stackFrame.GetMethod().Name, stackFrame.GetFileName(),
                        stackFrame.GetFileLineNumber().ToString() });

                Console.WriteLine("Error : " + e.ToString());
            }

            //owner.db.Close();
        }

        void send(CPacket msg)
        {
            PROTOCOL protocol = (PROTOCOL)msg.protocol_id;

            StackFrame stackFrame = new StackFrame(1, true);
            owner.mainFrm.BeginInvoke(owner.mainFrm.WriteLogInstance,
                        new object[] { "[table Num : " + owner.tableNum.ToString() + "] protocol id " + protocol,
                        stackFrame.GetMethod().Name, stackFrame.GetFileName(),
                        stackFrame.GetFileLineNumber().ToString() });

            owner.send(msg);
        }


    }
}
