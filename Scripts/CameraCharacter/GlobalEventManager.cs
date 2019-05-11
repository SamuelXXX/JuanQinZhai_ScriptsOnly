using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GlobalEventManager
{
    #region Predefined Event
    public const string Back = "Back";
    public const string Forward = "Forward";
    public const string DrawUpClicked = "DrawUpClicked";
    public const string RoofRemoved = "RoofRemoved";
    public const string BlockCombine = "BlockCombine";
    public const string BlockDivide = "BlockDivide";
    #endregion

    #region Event Management
    static Hashtable eventTable = new Hashtable();
    public delegate void GlobalEventHandler(GlobalEvent evt);
    static Dictionary<string, GlobalEventHandler> immediateHandlersTable = new Dictionary<string, GlobalEventHandler>();

    public static void ResetGlobalEventManager()
    {
        immediateHandlersTable.Clear();
        eventTable.Clear();
    }

    public static void RegisterHandler(string evt, GlobalEventHandler handler)
    {
        if (string.IsNullOrEmpty(evt) || handler == null)
        {
            return;
        }

        if (!immediateHandlersTable.ContainsKey(evt))
        {
            immediateHandlersTable.Add(evt, handler);
        }
        else
        {
            immediateHandlersTable[evt] = handler;
        }
    }

    public static void UnregisterHandler(string evt, GlobalEventHandler handler)
    {
        if (string.IsNullOrEmpty(evt) || handler == null)
        {
            return;
        }

        if (immediateHandlersTable.ContainsKey(evt))
        {
            immediateHandlersTable[evt] -= handler;
        }
    }

    public static void UnregisterHandler(string evt)
    {
        if (string.IsNullOrEmpty(evt))
        {
            return;
        }

        if (immediateHandlersTable.ContainsKey(evt))
        {
            immediateHandlersTable.Remove(evt);
        }
    }

    public static void SendEvent(GlobalEvent globalEvent)
    {
        if (globalEvent == null || string.IsNullOrEmpty(globalEvent.evtName))
            return;

        if (immediateHandlersTable.ContainsKey(globalEvent.evtName) && immediateHandlersTable[globalEvent.evtName] != null)
        {
            immediateHandlersTable[globalEvent.evtName](globalEvent);
        }
        else
        {
            if (!eventTable.ContainsKey(globalEvent.evtName))
                eventTable.Add(globalEvent.evtName, globalEvent);
        }
    }

    public static void SendEvent(string globalEvent)
    {
        if (string.IsNullOrEmpty(globalEvent))
            return;

        SendEvent(new GlobalEvent(globalEvent));
    }

    public static bool PeekEvent(string evt, bool consumed = true)
    {
        if (eventTable.ContainsKey(evt))
        {
            if (consumed)
            {
                eventTable.Remove(evt);
            }
            return true;
        }
        return false;
    }

    public static string[] GetAllEvents()
    {
        string[] ret = new string[eventTable.Count];
        eventTable.Keys.CopyTo(ret, 0);
        return ret;
    }
    #endregion
}

public class GlobalEvent
{
    public string evtName;
    public object[] parameters;

    public GlobalEvent(string evtName)
    {
        this.evtName = evtName;
        parameters = null;
    }

    public GlobalEvent(string evtName, params object[] pars)
    {
        this.evtName = evtName;
        parameters = pars;
    }
}
