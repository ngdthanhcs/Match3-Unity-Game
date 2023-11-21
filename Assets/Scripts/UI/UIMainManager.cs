using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIMainManager : MonoBehaviour
{
    private IMenu[] _menuList;

    private GameManager _gameManager;

    private void Awake()
    {
        _menuList = GetComponentsInChildren<IMenu>(true);
    }

    void Start()
    {
        for (int i = 0; i < _menuList.Length; i++)
        {
            _menuList[i].Setup(this);
        }
    }

    internal void ShowMainMenu()
    {
        _gameManager.ClearLevel();
        _gameManager.SetState(GameManager.eStateGame.MAIN_MENU);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_gameManager.State == GameManager.eStateGame.GAME_STARTED)
            {
                _gameManager.SetState(GameManager.eStateGame.PAUSE);
            }
            else if (_gameManager.State == GameManager.eStateGame.PAUSE)
            {
                _gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
            }
        }
    }

    internal void Setup(GameManager gameManager)
    {
        _gameManager = gameManager;
        _gameManager.StateChangedAction += OnGameStateChange;
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.SETUP:
                break;
            case GameManager.eStateGame.MAIN_MENU:
                ShowMenu<UIPanelMain>();
                break;
            case GameManager.eStateGame.GAME_STARTED:
                ShowMenu<UIPanelGame>();
                break;
            case GameManager.eStateGame.PAUSE:
                ShowMenu<UIPanelPause>();
                break;
            case GameManager.eStateGame.GAME_OVER:
                ShowMenu<UIPanelGameOver>();
                break;
            case GameManager.eStateGame.RESTART:
                break;
        }
    }

    private void ShowMenu<T>() where T : IMenu
    {
        for (int i = 0; i < _menuList.Length; i++)
        {
            IMenu menu = _menuList[i];
            if(menu is T)
            {
                menu.Show();
            }
            else
            {
                menu.Hide();
            }            
        }
    }

    internal Text GetLevelConditionView()
    {
        UIPanelGame game = _menuList.Where(x => x is UIPanelGame).Cast<UIPanelGame>().FirstOrDefault();
        if (game)
        {
            return game.LevelConditionView;
        }

        return null;
    }

    internal void ShowPauseMenu()
    {
        _gameManager.SetState(GameManager.eStateGame.PAUSE);
    }

    internal void LoadLevelMoves()
    {
        _gameManager.LoadLevel(GameManager.eLevelMode.MOVES);
    }

    internal void LoadLevelTimer()
    {
        _gameManager.LoadLevel(GameManager.eLevelMode.TIMER);
    }

    internal void ShowGameMenu()
    {
        _gameManager.SetState(GameManager.eStateGame.GAME_STARTED);
    }

    public void RestartLevel()
    {
        _gameManager.RestartLevel();
    }
}
