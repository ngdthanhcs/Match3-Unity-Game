using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelCondition : MonoBehaviour
{
    public event Action ConditionCompleteEvent = delegate { };

    protected Text m_txt;
    protected float m_originalValue;

    protected bool m_conditionCompleted = false;

    public virtual void Setup(float value, Text txt)
    {
        m_txt = txt;
        m_originalValue = value;
    }

    public virtual void Setup(float value, Text txt, GameManager mngr)
    {
        m_txt = txt;
        m_originalValue = value;
    }

    public virtual void Setup(float value, Text txt, BoardController board)
    {
        m_txt = txt;
        m_originalValue = value;
    }

    public virtual void Restart()
    {
    }

    protected virtual void UpdateText() { }

    protected void OnConditionComplete()
    {
        m_conditionCompleted = true;

        ConditionCompleteEvent();
    }

    internal void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.SETUP:
                break;
            case GameManager.eStateGame.MAIN_MENU:
                break;
            case GameManager.eStateGame.GAME_STARTED:
                break;
            case GameManager.eStateGame.PAUSE:
                break;
            case GameManager.eStateGame.GAME_OVER:
                break;
            case GameManager.eStateGame.RESTART:
                Restart();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    protected virtual void OnDestroy()
    {

    }
}
