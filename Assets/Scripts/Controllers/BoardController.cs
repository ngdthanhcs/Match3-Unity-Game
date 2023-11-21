using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardController : MonoBehaviour
{
    public event Action OnMoveEvent = delegate { };

    public bool IsBusy { get; private set; }

    private Board _mBoard;

    private GameManager _mGameManager;

    private bool _mIsDragging;

    private Camera _mCam;

    private Collider2D _mHitCollider;

    private GameSettings _mGameSettings;

    private List<Cell> _mPotentialMatch;

    private float _mTimeAfterFill;

    private bool _mHintIsShown;

    private bool _mGameOver;

    public void StartGame(GameManager gameManager, GameSettings gameSettings)
    {
        _mGameManager = gameManager;

        _mGameSettings = gameSettings;

        _mGameManager.StateChangedAction += OnGameStateChange;

        _mCam = Camera.main;

        _mBoard = new Board(this.transform, gameSettings);

        Fill();
    }

    private void Fill()
    {
        _mBoard.Fill();
        FindMatchesAndCollapse();
    }

    private void OnGameStateChange(GameManager.eStateGame state)
    {
        switch (state)
        {
            case GameManager.eStateGame.GAME_STARTED:
                IsBusy = false;
                break;
            case GameManager.eStateGame.PAUSE:
                IsBusy = true;
                break;
            case GameManager.eStateGame.GAME_OVER:
                _mGameOver = true;
                StopHints();
                break;
            case GameManager.eStateGame.RESTART:
                StopHints();
                RestartCell();
                FindMatchesAndCollapse();
                break;
        }
    }

    private void RestartCell()
    {
        _mBoard.RestartCell();
    }


    public void Update()
    {
        if (_mGameOver) return;
        if (IsBusy) return;

        if (!_mHintIsShown)
        {
            _mTimeAfterFill += Time.deltaTime;
            if (_mTimeAfterFill > _mGameSettings.TimeForHint)
            {
                _mTimeAfterFill = 0f;
                ShowHint();
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            var hit = Physics2D.Raycast(_mCam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider)
            {
                _mIsDragging = true;
                _mHitCollider = hit.collider;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            ResetRayCast();
        }

        if (Input.GetMouseButton(0) && _mIsDragging)
        {
            var hit = Physics2D.Raycast(_mCam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider)
            {
                if (_mHitCollider && _mHitCollider != hit.collider)
                {
                    StopHints();

                    Cell c1 = _mHitCollider.GetComponent<Cell>();
                    Cell c2 = hit.collider.GetComponent<Cell>();
                    if (AreItemsNeighbor(c1, c2))
                    {
                        IsBusy = true;
                        SetSortingLayer(c1, c2);
                        _mBoard.Swap(c1, c2, () =>
                        {
                            FindMatchesAndCollapse(c1, c2);
                        });

                        ResetRayCast();
                    }
                }
            }
            else
            {
                ResetRayCast();
            }
        }
    }

    private void ResetRayCast()
    {
        _mIsDragging = false;
        _mHitCollider = null;
    }

    private void FindMatchesAndCollapse(Cell cell1, Cell cell2)
    {
        if (cell1.Item is BonusItem)
        {
            cell1.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else if (cell2.Item is BonusItem)
        {
            cell2.ExplodeItem();
            StartCoroutine(ShiftDownItemsCoroutine());
        }
        else
        {
            List<Cell> cells1 = GetMatches(cell1);
            List<Cell> cells2 = GetMatches(cell2);

            List<Cell> matches = new List<Cell>();
            matches.AddRange(cells1);
            matches.AddRange(cells2);
            matches = matches.Distinct().ToList();

            if (matches.Count < _mGameSettings.MatchesMin)
            {
                _mBoard.Swap(cell1, cell2, () =>
                {
                    IsBusy = false;
                });
            }
            else
            {
                OnMoveEvent();

                CollapseMatches(matches, cell2);
            }
        }
    }

    private void FindMatchesAndCollapse()
    {
        List<Cell> matches = _mBoard.FindFirstMatch();

        if (matches.Count > 0)
        {
            CollapseMatches(matches, null);
        }
        else
        {
            _mPotentialMatch = _mBoard.GetPotentialMatches();
            if (_mPotentialMatch.Count > 0)
            {
                IsBusy = false;

                _mTimeAfterFill = 0f;
            }
            else
            {
                //StartCoroutine(RefillBoardCoroutine());
                StartCoroutine(ShuffleBoardCoroutine());
            }
        }
    }

    private List<Cell> GetMatches(Cell cell)
    {
        List<Cell> listHor = _mBoard.GetHorizontalMatches(cell);
        if (listHor.Count < _mGameSettings.MatchesMin)
        {
            listHor.Clear();
        }

        List<Cell> listVert = _mBoard.GetVerticalMatches(cell);
        if (listVert.Count < _mGameSettings.MatchesMin)
        {
            listVert.Clear();
        }

        return listHor.Concat(listVert).Distinct().ToList();
    }

    private void CollapseMatches(List<Cell> matches, Cell cellEnd)
    {
        for (int i = 0; i < matches.Count; i++)
        {
            matches[i].ExplodeItem();
        }

        if(matches.Count > _mGameSettings.MatchesMin)
        {
            _mBoard.ConvertNormalToBonus(matches, cellEnd);
        }

        StartCoroutine(ShiftDownItemsCoroutine());
    }

    private IEnumerator ShiftDownItemsCoroutine()
    {
        _mBoard.ShiftDownItems();

        yield return new WaitForSeconds(0.2f);

        _mBoard.FillGapsWithNewItems();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator RefillBoardCoroutine()
    {
        _mBoard.ExplodeAllItems();

        yield return new WaitForSeconds(0.2f);

        _mBoard.Fill();

        yield return new WaitForSeconds(0.2f);

        FindMatchesAndCollapse();
    }

    private IEnumerator ShuffleBoardCoroutine()
    {
        _mBoard.Shuffle();

        yield return new WaitForSeconds(0.3f);

        FindMatchesAndCollapse();
    }


    private void SetSortingLayer(Cell cell1, Cell cell2)
    {
        if (cell1.Item != null) cell1.Item.SetSortingLayerHigher();
        if (cell2.Item != null) cell2.Item.SetSortingLayerLower();
    }

    private bool AreItemsNeighbor(Cell cell1, Cell cell2)
    {
        return cell1.IsNeighbour(cell2);
    }

    internal void Clear()
    {
        _mBoard.Clear();
    }

    private void ShowHint()
    {
        _mHintIsShown = true;
        foreach (var cell in _mPotentialMatch)
        {
            cell.AnimateItemForHint();
        }
    }

    private void StopHints()
    {
        _mHintIsShown = false;
        foreach (var cell in _mPotentialMatch)
        {
            cell.StopHintAnimation();
        }

        _mPotentialMatch.Clear();
    }
}
