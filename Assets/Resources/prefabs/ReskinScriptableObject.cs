using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ReskinSO", menuName = "ScriptableObjects/ReskinSO", order = 1)]
public class ReskinScriptableObject : ScriptableObject
{
    public List<SpriteRenderer> listObjectToReSkin;
    public List<Sprite> listSkin;

    [ContextMenu("SwapSkin")]
    public void SwapSkin()
    {
        for (int i = 0; i < listObjectToReSkin.Count; i++)
        {
            listObjectToReSkin[i].sprite = listSkin[i];
        }
    }
}
