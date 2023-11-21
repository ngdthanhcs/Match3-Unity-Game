﻿using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using URandom = UnityEngine.Random;

public class Utils
{
    public static NormalItem.eNormalType GetRandomNormalType()
    {
        Array values = Enum.GetValues(typeof(NormalItem.eNormalType));
        NormalItem.eNormalType result = (NormalItem.eNormalType)values.GetValue(URandom.Range(0, values.Length));

        return result;
    }

    public static NormalItem.eNormalType GetRandomNormalTypeExcept(IEnumerable<NormalItem.eNormalType> types)
    {
        List<NormalItem.eNormalType> list = Enum.GetValues(typeof(NormalItem.eNormalType)).Cast<NormalItem.eNormalType>().Except(types).ToList();

        int rnd = URandom.Range(0, list.Count);
        NormalItem.eNormalType result = list[rnd];

        return result;
    }
}

public static class ListUtils
{
    public static void AddIfAbsent<T>(this List<T> list, T newElement)
    {
        if (!list.Contains(newElement))
        {
            list.Add(newElement);
        }
    }

    public static void AddIfCellExist(this List<NormalItem.eNormalType> list, [CanBeNull] Cell cell)
    {
        if (!cell) return;
        
        if (cell.NeighbourBottom.Item is NormalItem nitem)
        {
            list.Add(nitem.ItemType);
        }
    }
}
