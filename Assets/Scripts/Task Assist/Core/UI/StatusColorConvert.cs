using System;
using System.Collections.Generic;
using UnityEngine;

namespace TaskAssist.Utils
{
    public static class StatusColorConvert
    {
        // LabelColorSet.LabelSet.Code �@�� Key �� Lookup Table
        private static Dictionary<string, LabelColorSet> _colorMap;

        /// <summary>
        /// ��l���C��M�g��C
        /// </summary>
        /// <param name="colors">StatusColors ���C��C</param>
        public static void InitMap(List<StatusColor> colors)
        {
            _colorMap = new Dictionary<string, LabelColorSet>();

            for (int i = 0; i < colors.Count; i++)
            {
                StatusColor statusColor = colors[i];
                List<LabelSet> colorCodes = statusColor.StatusLabel;
                for (int j = 0; j < colorCodes.Count; j++)
                {
                    LabelSet labelSet = colorCodes[j];
                    if (!_colorMap.ContainsKey(labelSet.Code)) 
                    {
                        _colorMap[labelSet.Code] = new LabelColorSet(labelSet, statusColor.Colors);
                    }
                    else Debug.LogWarning($"���ƪ� StatusLabel: {labelSet.Code}�A�w���L�C");
                }
            }
        }

        /// <summary>
        /// �ھڪ��A������������C��C
        /// </summary>
        /// <param name="statusCode">���A���ҡC</param>
        /// <returns>�������C��C</returns>
        public static LabelColorSet GetLabelColorSet(string statusCode)
        {
            if (_colorMap != null && _colorMap.TryGetValue(statusCode, out var colorSet))
                return colorSet;
            Debug.LogWarning($"�L�k���������C��AMap Count({_colorMap.Count})�AStatusLabel: {statusCode}");
            return new LabelColorSet(); // �q�{��^�C��
        }
    }

    // Use for Unity Inspector
    [Serializable]
    public class StatusColor
    {
        public List<LabelSet> StatusLabel;
        public ColorSet Colors;

        public StatusColor(ColorSet colors)
        {
            Colors = colors;
            StatusLabel = new List<LabelSet>();
        }
    }

    [Serializable]
    public class LabelSet
    {
        public string Code;
        public string Text_zh;

        public LabelSet(string code, string text_zh)
        {
            Code = code;
            Text_zh = text_zh;
        }   
    }

    [Serializable]
    public class ColorSet
    {
        public Color BaseColor;
        public Color TextColor;

        public ColorSet(Color color, Color textColor)
        {
            BaseColor = color;
            TextColor = textColor;
        }
    }

    [Serializable]
    public class LabelColorSet
    {
        public LabelSet Label;
        public ColorSet Colors;

        public LabelColorSet()
        {
            Label = new LabelSet("default", "����");
            Colors = new ColorSet(Color.black, Color.white);
        }

        public LabelColorSet(LabelSet labelSet, ColorSet colors)
        {
            Label = labelSet;
            Colors = colors;
        }
    }
}
