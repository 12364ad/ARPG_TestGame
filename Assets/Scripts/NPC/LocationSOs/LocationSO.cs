using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName ="LocationSO")]
public class LocationSO : ScriptableObject
{
    [Header("地点编号（唯一标识，可用于存档/查找）")]
    public string locationID;
    [Header("地点显示名称")]
    public string displayName;
}
