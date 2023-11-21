using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditorInternal.Profiling.Memory.Experimental;
using UnityEngine;

public class Board
{
    public enum eMatchDirection
    {
        NONE,
        HORIZONTAL,
        VERTICAL,
        ALL
    }

    private readonly int _boardSizeX;

    private readonly int _boardSizeY;

    private Cell[,] _mCells;

    private readonly Transform _mRoot;

    private readonly int _mMatchMin;

    public Board(Transform transform, GameSettings gameSettings)
    {
        _mRoot = transform;

        _mMatchMin = gameSettings.MatchesMin;

        this._boardSizeX = gameSettings.BoardSizeX;
        this._boardSizeY = gameSettings.BoardSizeY;

        _mCells = new Cell[_boardSizeX, _boardSizeY];
        _originalFormation = new Dictionary<Cell, Item>();

        CreateBoard();
    }

    private void CreateBoard()
    {
        Vector3 origin = new Vector3(-_boardSizeX * 0.5f + 0.5f, -_boardSizeY * 0.5f + 0.5f, 0f);
        GameObject prefabBG = Resources.Load<GameObject>(Constants.PREFAB_CELL_BACKGROUND);
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                GameObject go = GameObject.Instantiate(prefabBG);
                go.transform.position = origin + new Vector3(x, y, 0f);
                go.transform.SetParent(_mRoot);

                Cell cell = go.GetComponent<Cell>();
                cell.Setup(x, y);

                _mCells[x, y] = cell;
            }
        }

        //set neighbours
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                if (y + 1 < _boardSizeY) _mCells[x, y].NeighbourUp = _mCells[x, y + 1];
                if (x + 1 < _boardSizeX) _mCells[x, y].NeighbourRight = _mCells[x + 1, y];
                if (y > 0) _mCells[x, y].NeighbourBottom = _mCells[x, y - 1];
                if (x > 0) _mCells[x, y].NeighbourLeft = _mCells[x - 1, y];
            }
        }

    }

    private Dictionary<Cell, Item> _originalFormation;

    internal void Fill()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                NormalItem item = new NormalItem();
                    //new NormalItem();

                List<NormalItem.eNormalType> types = new List<NormalItem.eNormalType>();
                if (cell.NeighbourBottom != null)
                {
                    if (cell.NeighbourBottom.Item is NormalItem nitem)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                if (cell.NeighbourLeft != null)
                {
                    if (cell.NeighbourLeft.Item is NormalItem nitem)
                    {
                        types.Add(nitem.ItemType);
                    }
                }

                var type = Utils.GetRandomNormalTypeExcept(types.ToArray());
                item.SetType(type);
                GetView(item);
                //item.CreateView();
                item.SetViewRoot(_mRoot);

                cell.Assign(item);
                cell.ApplyItemPosition(false);

                _originalFormation[cell] = item;
            }
        }
    }

    private Dictionary<NormalItem.eNormalType, List<SpriteRenderer>> _normalDict 
        = new Dictionary<NormalItem.eNormalType, List<SpriteRenderer>>();
    //private Dictionary<BonusItem.eBonusType, List<SpriteRenderer>> _bonusDict;

    private void GetView(NormalItem item)
    {
        if (!_normalDict.ContainsKey(item.ItemType))
        {
            _normalDict.Add(item.ItemType, new List<SpriteRenderer>());
        }
        var availableItem = _normalDict[item.ItemType]
            .FirstOrDefault(i => !i.gameObject.activeSelf);

        if (!availableItem)
        {
            item.CreateView();
            _normalDict[item.ItemType].Add(item.CachedSpriteRenderer);
        }
        else
        {
            item.SetView(availableItem);
            item.ReactivateView();
        }
    }

    internal void Shuffle()
    {
        List<Item> list = new List<Item>();
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                list.Add(_mCells[x, y].Item);
                _mCells[x, y].Free();
            }
        }

        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                int rnd = UnityEngine.Random.Range(0, list.Count);
                _mCells[x, y].Assign(list[rnd]);
                _mCells[x, y].ApplyItemMoveToPosition();

                list.RemoveAt(rnd);
            }
        }
    }


    internal void FillGapsWithNewItems()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                var cell = _mCells[x, y];
                if (!cell.IsEmpty) continue;

                var item = new NormalItem();

                var types = new List<NormalItem.eNormalType>();
                types.AddIfCellExist(cell.NeighbourLeft);
                types.AddIfCellExist(cell.NeighbourBottom);
                types.AddIfCellExist(cell.NeighbourRight);
                types.AddIfCellExist(cell.NeighbourUp);

                item.SetType(Utils.GetRandomNormalTypeExcept(types));
                GetView(item);
                //item.CreateView();
                item.SetViewRoot(_mRoot);

                cell.Assign(item);
                cell.ApplyItemPosition(true);
            }
        }
    }

    internal void ExplodeAllItems()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                cell.ExplodeItem();
            }
        }
    }

    public void Swap(Cell cell1, Cell cell2, Action callback)
    {
        Item item = cell1.Item;
        cell1.Free();
        Item item2 = cell2.Item;
        cell1.Assign(item2);
        cell2.Free();
        cell2.Assign(item);

        item.View.DOMove(cell2.transform.position, 0.3f);
        item2.View.DOMove(cell1.transform.position, 0.3f).OnComplete(() => { if (callback != null) callback(); });
    }

    public List<Cell> GetHorizontalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell> { cell };

        //check horizontal match
        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourRight;
            if (!neib) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourLeft;
            if (!neib) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }


    public List<Cell> GetVerticalMatches(Cell cell)
    {
        List<Cell> list = new List<Cell>();
        list.Add(cell);

        Cell newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourUp;
            if (!neib) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        newcell = cell;
        while (true)
        {
            Cell neib = newcell.NeighbourBottom;
            if (!neib) break;

            if (neib.IsSameType(cell))
            {
                list.Add(neib);
                newcell = neib;
            }
            else break;
        }

        return list;
    }

    internal void ConvertNormalToBonus(List<Cell> matches, Cell cellToConvert)
    {
        eMatchDirection dir = GetMatchDirection(matches);

        BonusItem item = new BonusItem();
        switch (dir)
        {
            case eMatchDirection.ALL:
                item.SetType(BonusItem.eBonusType.ALL);
                break;
            case eMatchDirection.HORIZONTAL:
                item.SetType(BonusItem.eBonusType.HORIZONTAL);
                break;
            case eMatchDirection.VERTICAL:
                item.SetType(BonusItem.eBonusType.VERTICAL);
                break;
        }

        if (item != null)
        {
            if (!cellToConvert)
            {
                int rnd = UnityEngine.Random.Range(0, matches.Count);
                cellToConvert = matches[rnd];
            }

            item.CreateView();
            item.SetViewRoot(_mRoot);

            cellToConvert.Free();
            cellToConvert.Assign(item);
            cellToConvert.ApplyItemPosition(true);
        }
    }


    internal eMatchDirection GetMatchDirection(List<Cell> matches)
    {
        if (matches == null || matches.Count < _mMatchMin) return eMatchDirection.NONE;

        var listH = matches.Where(x => x.BoardX == matches[0].BoardX).ToList();
        if (listH.Count == matches.Count)
        {
            return eMatchDirection.VERTICAL;
        }

        var listV = matches.Where(x => x.BoardY == matches[0].BoardY).ToList();
        if (listV.Count == matches.Count)
        {
            return eMatchDirection.HORIZONTAL;
        }

        if (matches.Count > 5)
        {
            return eMatchDirection.ALL;
        }

        return eMatchDirection.NONE;
    }

    internal List<Cell> FindFirstMatch()
    {
        List<Cell> list = new List<Cell>();

        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];

                var listhor = GetHorizontalMatches(cell);
                if (listhor.Count >= _mMatchMin)
                {
                    list = listhor;
                    break;
                }

                var listvert = GetVerticalMatches(cell);
                if (listvert.Count >= _mMatchMin)
                {
                    list = listvert;
                    break;
                }
            }
        }

        return list;
    }

    public List<Cell> CheckBonusIfCompatible(List<Cell> matches)
    {
        var dir = GetMatchDirection(matches);

        var bonus = matches.Where(x => x.Item is BonusItem).FirstOrDefault();
        if(bonus == null)
        {
            return matches;
        }

        List<Cell> result = new List<Cell>();
        switch (dir)
        {
            case eMatchDirection.HORIZONTAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.HORIZONTAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.VERTICAL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.VERTICAL)
                    {
                        result.Add(cell);
                    }
                }
                break;
            case eMatchDirection.ALL:
                foreach (var cell in matches)
                {
                    BonusItem item = cell.Item as BonusItem;
                    if (item == null || item.ItemType == BonusItem.eBonusType.ALL)
                    {
                        result.Add(cell);
                    }
                }
                break;
        }

        return result;
    }

    internal List<Cell> GetPotentialMatches()
    {
        List<Cell> result = new List<Cell>();
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];

                //check right
                /* example *\
                  * * * * *
                  * * * * *
                  * * * ? *
                  * & & * ?
                  * * * ? *
                \* example  */

                if (cell.NeighbourRight)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourRight, cell.NeighbourRight.NeighbourRight);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check up
                /* example *\
                  * ? * * *
                  ? * ? * *
                  * & * * *
                  * & * * *
                  * * * * *
                \* example  */
                if (cell.NeighbourUp)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourUp, cell.NeighbourUp.NeighbourUp);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check bottom
                /* example *\
                  * * * * *
                  * & * * *
                  * & * * *
                  ? * ? * *
                  * ? * * *
                \* example  */
                if (cell.NeighbourBottom)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourBottom, cell.NeighbourBottom.NeighbourBottom);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                //check left
                /* example *\
                  * * * * *
                  * * * * *
                  * ? * * *
                  ? * & & *
                  * ? * * *
                \* example  */
                if (cell.NeighbourLeft)
                {
                    result = GetPotentialMatch(cell, cell.NeighbourLeft, cell.NeighbourLeft.NeighbourLeft);
                    if (result.Count > 0)
                    {
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * * * * *
                  * * ? * *
                  * & * & *
                  * * ? * *
                \* example  */
                Cell neib = cell.NeighbourRight;
                if (neib && neib.NeighbourRight != null && neib.NeighbourRight.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellVertical(neib, cell);
                    if (second)
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourRight);
                        result.Add(second);
                        break;
                    }
                }

                /* example *\
                  * * * * *
                  * & * * *
                  ? * ? * *
                  * & * * *
                  * * * * *
                \* example  */
                neib = null;
                neib = cell.NeighbourUp;
                if (neib  && neib.NeighbourUp  && neib.NeighbourUp.IsSameType(cell))
                {
                    Cell second = LookForTheSecondCellHorizontal(neib, cell);
                    if (second )
                    {
                        result.Add(cell);
                        result.Add(neib.NeighbourUp);
                        result.Add(second);
                        break;
                    }
                }
            }

            if (result.Count > 0) break;
        }

        return result;
    }

    private List<Cell> GetPotentialMatch(Cell cell, Cell neighbour, Cell target)
    {
        List<Cell> result = new List<Cell>();

        if (neighbour  && neighbour.IsSameType(cell))
        {
            Cell third = LookForTheThirdCell(target, neighbour);
            if (third )
            {
                result.Add(cell);
                result.Add(neighbour);
                result.Add(third);
            }
        }

        return result;
    }

    private Cell LookForTheSecondCellHorizontal(Cell target, Cell main)
    {
        if (!target) return null;
        if (target.IsSameType(main)) return null;

        //look right
        Cell second = null;
        second = target.NeighbourRight;
        if (second  && second.IsSameType(main))
        {
            return second;
        }

        //look left
        second = null;
        second = target.NeighbourLeft;
        if (second  && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheSecondCellVertical(Cell target, Cell main)
    {
        if (!target) return null;
        if (target.IsSameType(main)) return null;

        //look up        
        Cell second = target.NeighbourUp;
        if (second  && second.IsSameType(main))
        {
            return second;
        }

        //look bottom
        second = null;
        second = target.NeighbourBottom;
        if (second  && second.IsSameType(main))
        {
            return second;
        }

        return null;
    }

    private Cell LookForTheThirdCell(Cell target, Cell main)
    {
        if (!target) return null;
        if (target.IsSameType(main)) return null;

        //look up
        Cell third = CheckThirdCell(target.NeighbourUp, main);
        if (third )
        {
            return third;
        }

        //look right
        third = null;
        third = CheckThirdCell(target.NeighbourRight, main);
        if (third )
        {
            return third;
        }

        //look bottom
        third = null;
        third = CheckThirdCell(target.NeighbourBottom, main);
        if (third )
        {
            return third;
        }

        //look left
        third = null;
        third = CheckThirdCell(target.NeighbourLeft, main); ;
        if (third )
        {
            return third;
        }

        return null;
    }

    private Cell CheckThirdCell(Cell target, Cell main)
    {
        if (target  && target != main && target.IsSameType(main))
        {
            return target;
        }

        return null;
    }

    internal void ShiftDownItems()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            int shifts = 0;
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                if (cell.IsEmpty)
                {
                    shifts++;
                    continue;
                }

                if (shifts == 0) continue;

                Cell holder = _mCells[x, y - shifts];

                Item item = cell.Item;
                cell.Free();

                holder.Assign(item);
                item.View.DOMove(holder.transform.position, 0.3f);
            }
        }
    }

    public void Clear()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                cell.Clear();

                GameObject.Destroy(cell.gameObject);
                _mCells[x, y] = null;
            }
        }
    }
    
    public void RestartCell()
    {
        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                cell.DeactivateItem();
            }
        }

        for (int x = 0; x < _boardSizeX; x++)
        {
            for (int y = 0; y < _boardSizeY; y++)
            {
                Cell cell = _mCells[x, y];
                cell.Assign(_originalFormation[cell]);
                
                GetView((NormalItem) cell.Item);
                cell.ApplyItemPosition(true);
            }
        }
    }
}
